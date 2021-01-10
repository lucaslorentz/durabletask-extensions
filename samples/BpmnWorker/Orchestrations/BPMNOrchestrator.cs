using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using BpmnWorker.Activities;
using BpmnWorker.Providers;
using DurableTask.Core;
using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Spec.BPMN.MODEL;

namespace BpmnWorker.Orchestrations
{
    [Orchestration(Name = "BPMN")]
    public class BPMNOrchestrator : OrchestrationBase<object, BPMNOrchestratorInput>
    {
        private readonly IBPMNProvider _bpmnProvider;
        private readonly IScriptExecutor _scriptExecutor;
        private readonly ILogger<BPMNOrchestrator> _logger;

        private TDefinitions _model;
        private CancellationTokenSource _terminateCancellationTokenSource;
        private TaskCompletionSource<object> _terminateTaskCompletionSource;
        private ConcurrentBag<Func<Task>> _compensations;
        private ConcurrentDictionary<string, int> _nodeHits;
        private JObject _variables;

        public BPMNOrchestrator(
            IBPMNProvider bpmnProvider,
            IScriptExecutor scriptExecutor,
            ILogger<BPMNOrchestrator> logger)
        {
            _bpmnProvider = bpmnProvider;
            _scriptExecutor = scriptExecutor;
            _logger = logger;
        }

        public override async Task<object> RunTask(BPMNOrchestratorInput input)
        {
            _model = await GetBpmnDefinition(input);

            _terminateCancellationTokenSource = new CancellationTokenSource();
            _terminateTaskCompletionSource = new TaskCompletionSource<object>();
            _compensations = new ConcurrentBag<Func<Task>>();
            _nodeHits = new ConcurrentDictionary<string, int>();
            _variables = new JObject();

            var executableProcess = _model.RootElement.OfType<TProcess>()
                .Where(p => p.IsExecutable == true)
                .FirstOrDefault();

            await WaitForAny(
                _terminateTaskCompletionSource.Task,
                VisitProcess(executableProcess)
            );

            _terminateCancellationTokenSource.Cancel();

            return null;
        }

        private async Task VisitFlowNodes(TFlowNode[] flowNodes)
        {
            await WaitForAll(flowNodes.Select(VisitFlowNode));
        }

        private async Task VisitFlowNode(TFlowNode flowNode)
        {
            switch (flowNode)
            {
                case TStartEvent startEvent:
                    await VisitStartEvent(startEvent);
                    break;
                case TEndEvent endEvent:
                    await VisitEndEvent(endEvent);
                    break;
                case TParallelGateway parallelGateway:
                    await VisitParallelGateway(parallelGateway);
                    break;
                case TExclusiveGateway exclusiveGateway:
                    await VisitExclusiveGateway(exclusiveGateway);
                    break;
                case TInclusiveGateway inclusiveGateway:
                    await VisitInclusiveGateway(inclusiveGateway);
                    break;
                case TComplexGateway complexGateway:
                    await VisitComplexGateway(complexGateway);
                    break;
                case TEventBasedGateway eventBasedGateway:
                    await VisitEventBasedGateway(eventBasedGateway);
                    break;
                case TIntermediateThrowEvent intermediateThrowEvent:
                    await VisitIntermediateThrowEvent(intermediateThrowEvent);
                    break;
                case TIntermediateCatchEvent intermediateCatchEvent:
                    await VisitIntermediateCatchEvent(intermediateCatchEvent);
                    break;
                case TServiceTask serviceTask:
                    await VisitServiceTask(serviceTask);
                    break;
                case TScriptTask scriptTask:
                    await VisitScriptTask(scriptTask);
                    break;
            }
        }

        private async Task VisitProcess(TProcess process)
        {
            var startEvents = process.FlowElement.OfType<TStartEvent>().ToArray();
            await VisitFlowNodes(startEvents);
        }

        private async Task VisitParallelGateway(TParallelGateway parallelGateway)
        {
            var hits = Hit(parallelGateway);
            if (hits < parallelGateway.Incoming.Count)
                return;

            await VisitFlowNodes(GetParallelOutgoingNodes(parallelGateway));
        }

        private async Task VisitExclusiveGateway(TExclusiveGateway exclusiveGateway)
        {
            var hits = Hit(exclusiveGateway);
            if (hits > 1)
                return;

            await VisitFlowNode(GetExclusiveOutgoingNode(exclusiveGateway));
        }

        private async Task VisitInclusiveGateway(TInclusiveGateway inclusiveGateway)
        {
            var hits = Hit(inclusiveGateway);
            if (hits < inclusiveGateway.Incoming.Count)
                return;

            await VisitFlowNodes(GetInclusiveOutgoingNodes(inclusiveGateway));
        }

        private Task VisitComplexGateway(TComplexGateway complexGateway)
        {
            throw new NotSupportedException($"ComplexGateway is not supported");
        }

        private async Task VisitEventBasedGateway(TEventBasedGateway eventBasedGateway)
        {
            var hits = Hit(eventBasedGateway);
            if (hits > 1)
                return;

            await VisitFlowNodes(GetParallelOutgoingNodes(eventBasedGateway));
        }

        private async Task VisitStartEvent(TStartEvent startEvent)
        {
            await VisitFlowNodes(GetParallelOutgoingNodes(startEvent));
        }

        private async Task VisitEndEvent(TEndEvent element)
        {
            foreach (var eventDefinition in element.EventDefinition)
            {
                switch (eventDefinition)
                {
                    case TTerminateEventDefinition terminateEvent:
                        await VisitTerminateEvent(element, terminateEvent);
                        return;
                }
            }

            _logger.LogWarning("End");
        }

        private async Task VisitTerminateEvent(TEndEvent endEvent, TTerminateEventDefinition terminateEvent)
        {
            _logger.LogWarning("Terminate");
            _terminateTaskCompletionSource.TrySetResult(default(object));
            await VisitFlowNodes(GetParallelOutgoingNodes(endEvent));
        }

        private async Task VisitServiceTask(TServiceTask serviceTask)
        {
            var topic = serviceTask.AnyAttribute.FirstOrDefault(x => x.LocalName == "topic")?.Value;
            var nameVersion = new Regex(@"^(?<name>.*?)\s*(?:\((?<version>.*)\))?$", RegexOptions.Compiled).Match(topic);
            var name = nameVersion.Groups["name"].Value;
            var version = nameVersion.Groups["version"].Value;

            var inputOutput = serviceTask.ExtensionElements?.Any?.FirstOrDefault(x => x.LocalName == "inputOutput");

            var input = await PrepareInput(inputOutput);

            var output = await Context.ScheduleTask<JToken>(name, version, input);

            var compensationsBoundaryEvents = GetBoundaryEvents<TCompensateEventDefinition>(serviceTask);
            if (compensationsBoundaryEvents.Length != 0)
            {
                foreach (var compensationBoundaryEvent in compensationsBoundaryEvents)
                {
                    _compensations.Add(async () =>
                    {
                        await VisitFlowNodes(GetBoundaryFlowNodes(compensationBoundaryEvent));
                    });
                }
            }

            await ProcessOutput(inputOutput, output);

            await VisitFlowNodes(GetParallelOutgoingNodes(serviceTask));
        }

        private async Task<JObject> PrepareInput(XmlElement element)
        {
            var input = default(JObject);
            if (element != null)
            {
                input = new JObject();
                foreach (var inputParameter in element.ChildNodes.OfType<XmlElement>().Where(x => x.LocalName == "inputParameter"))
                {
                    var name = inputParameter.GetAttribute("name");
                    var script = inputParameter.ChildNodes.OfType<XmlElement>().FirstOrDefault(x => x.LocalName == "script");
                    if (script != null)
                    {
                        var scriptFormat = script.GetAttribute("scriptFormat");
                        var scriptContent = script.InnerText;
                        var result = await _scriptExecutor.Execute<JToken>(scriptFormat, scriptContent, new Dictionary<string, object>
                        {
                            ["variables"] = _variables
                        });
                        input[name] = result;
                    }
                    else
                    {
                        var value = inputParameter.InnerText;
                        input[name] = value;
                    }
                }
            }
            return input;
        }

        private async Task ProcessOutput(XmlElement element, JToken output)
        {
            if (element == null)
                return;

            foreach (var outputParameter in element.ChildNodes.OfType<XmlElement>().Where(x => x.LocalName == "outputParameter"))
            {
                var name = outputParameter.GetAttribute("name");
                var script = outputParameter.ChildNodes.OfType<XmlElement>().FirstOrDefault(x => x.LocalName == "script");
                if (script != null)
                {
                    var scriptFormat = script.GetAttribute("scriptFormat");
                    var scriptContent = script.InnerText;
                    var scriptOutput = await _scriptExecutor.Execute<JToken>(scriptFormat, scriptContent, new Dictionary<string, object>
                    {
                        ["variables"] = _variables,
                        ["result"] = output
                    });
                    _variables[name] = scriptOutput;
                }
                else
                {
                    var value = outputParameter.InnerText;
                    _variables[name] = new JValue(value);
                }
            }
        }

        private async Task VisitScriptTask(TScriptTask scriptTask)
        {
            var input = new ScriptActivity.Input
            {
                Name = scriptTask.Name,
                ScriptFormat = scriptTask.ScriptFormat,
                Script = scriptTask.Script?.Text?.FirstOrDefault(),
                Variables = _variables
            };

            var output = await Context.ScheduleTask<JToken>("Script", string.Empty, input);

            var resultVariable = scriptTask.AnyAttribute.FirstOrDefault(a => a.LocalName == "resultVariable")?.Value;
            if (resultVariable != null)
            {
                _variables[resultVariable] = output;
            }

            await VisitFlowNodes(GetParallelOutgoingNodes(scriptTask));
        }

        private async Task VisitIntermediateThrowEvent(TIntermediateThrowEvent intermediateThrowEvent)
        {
            foreach (var eventDefinition in intermediateThrowEvent.EventDefinition)
            {
                switch (eventDefinition)
                {
                    case TMessageEventDefinition messageEventDefinition:
                        await VisitIntermediateThrowMessageEvent(intermediateThrowEvent, messageEventDefinition);
                        break;
                    case TCompensateEventDefinition compensateEventDefinition:
                        var compensationsTasks = new List<Task>();
                        while (_compensations.TryTake(out var c))
                            compensationsTasks.Add(c());
                        await WaitForAll(compensationsTasks);
                        break;
                }
            }

        }

        private async Task VisitIntermediateThrowMessageEvent(
            TIntermediateThrowEvent intermediateThrowEvent,
            TMessageEventDefinition messageEventDefinition)
        {
            var messageRef = messageEventDefinition.MessageRef.ToString();

            Context.SendEvent(Context.OrchestrationInstance, messageRef, null);

            await VisitFlowNodes(GetParallelOutgoingNodes(intermediateThrowEvent));
        }

        private async Task VisitIntermediateCatchEvent(TIntermediateCatchEvent intermediateCatchEvent)
        {
            foreach (var eventDefinition in intermediateCatchEvent.EventDefinition)
            {
                switch (eventDefinition)
                {
                    case TMessageEventDefinition messageEventDefinition:
                        await VisitIntermediateCatchMessageEvent(intermediateCatchEvent, messageEventDefinition);
                        break;
                    case TTimerEventDefinition timerEventDefinition:
                        await VisitIntermediateCatchTimerEvent(intermediateCatchEvent, timerEventDefinition);
                        break;
                }
            }
        }

        private async Task VisitIntermediateCatchMessageEvent(
            TIntermediateCatchEvent intermediateCatchEvent,
            TMessageEventDefinition messageEventDefinition)
        {
            var messageRef = messageEventDefinition.MessageRef.ToString();
            _logger.LogWarning("Waiting for message {message}", messageRef);

            var result = await EventReceiver.WaitForEventAsync<JObject>(messageRef, _terminateCancellationTokenSource.Token);
            _logger.LogWarning("Received message {message}", messageRef);

            await VisitFlowNodes(GetParallelOutgoingNodes(intermediateCatchEvent));
        }

        private async Task VisitIntermediateCatchTimerEvent(
            TIntermediateCatchEvent intermediateCatchEvent,
            TTimerEventDefinition timerEventDefinition)
        {
            if (timerEventDefinition.TimeDate != null)
            {
                var fireAt = XmlConvert.ToDateTime(timerEventDefinition.TimeDate.Text.First(), XmlDateTimeSerializationMode.Local);
                _logger.LogWarning("Waiting for timer {fireAt}", fireAt);
                await Context.CreateTimer(fireAt, CancellationToken.None);
                _logger.LogWarning("Received timer {fireAt}", fireAt);

                await VisitFlowNodes(GetParallelOutgoingNodes(intermediateCatchEvent));
            }
            else if (timerEventDefinition.TimeDuration != null)
            {
                var duration = XmlConvert.ToTimeSpan(timerEventDefinition.TimeDuration.Text.First());
                var fireAt = Context.CurrentUtcDateTime.Add(duration);
                _logger.LogWarning("Waiting for timer {fireAt}", fireAt);
                await Context.CreateTimer(fireAt, CancellationToken.None);
                _logger.LogWarning("Received timer {fireAt}", fireAt);

                await VisitFlowNodes(GetParallelOutgoingNodes(intermediateCatchEvent));
            }
            else
            {
                _logger.LogWarning("Not supported timerEventDefinition");
            }
        }

        private TFlowNode GetExclusiveOutgoingNode(TFlowNode flowNode)
        {
            var sequence = FindByIds<TSequenceFlow>(flowNode.Outgoing.Select(x => x.Name))
                .Where(x => x.ConditionExpression != null)
                .FirstOrDefault(EvaluateSequence);

            if (sequence != null)
                return FindById<TFlowNode>(sequence.TargetRef);

            var defaultSequence = FindByIds<TSequenceFlow>(flowNode.Outgoing.Select(x => x.Name))
                .FirstOrDefault(x => x.ConditionExpression == null);

            if (defaultSequence == null)
                throw new Exception("No applicable exclusive outgoing node");

            return FindById<TFlowNode>(defaultSequence.TargetRef);
        }

        private TFlowNode[] GetInclusiveOutgoingNodes(TFlowNode flowNode)
        {
            var sequences = FindByIds<TSequenceFlow>(flowNode.Outgoing.Select(x => x.Name))
                .Where(x => x.ConditionExpression != null)
                .Where(EvaluateSequence)
                .ToArray();

            if (sequences.Length > 0)
                return FindByIds<TFlowNode>(sequences.Select(s => s.TargetRef));

            var defaultSequence = FindByIds<TSequenceFlow>(flowNode.Outgoing.Select(x => x.Name))
                .FirstOrDefault(x => x.ConditionExpression == null);

            if (defaultSequence == null)
                throw new Exception("No applicable invlusive outgoing node");

            return new TFlowNode[] {
                FindById<TFlowNode>(defaultSequence.TargetRef)
            };
        }

        private TBoundaryEvent[] GetBoundaryEvents<T>(TFlowNode flowNode)
        {
            return FindElements<TBoundaryEvent>()
                .Where(x => x.AttachedToRef.Name == flowNode.Id)
                .Where(x => x.EventDefinition.OfType<T>().Any())
                .ToArray();
        }

        private TFlowNode[] GetParallelOutgoingNodes(TFlowNode flowNode)
        {
            var sequences = FindByIds<TSequenceFlow>(flowNode.Outgoing.Select(x => x.Name))
                .Where(EvaluateSequence)
                .ToArray();

            return FindByIds<TFlowNode>(sequences.Select(s => s.TargetRef));
        }

        private TFlowNode[] GetBoundaryFlowNodes(TBoundaryEvent boundaryEvent)
        {
            var targetIds = FindElements<TAssociation>()
                .Where(a => a.SourceRef.Name == boundaryEvent.Id)
                .Select(a => a.TargetRef.Name)
                .ToArray();

            return FindByIds<TFlowNode>(targetIds);
        }

        private bool EvaluateSequence(TSequenceFlow sequenceFlow)
        {
            var expression = sequenceFlow.ConditionExpression?.Text?.FirstOrDefault();
            if (expression == null)
                return true;

            var result = _scriptExecutor.Execute<bool>("javascript", expression, new Dictionary<string, object>
            {
                ["variables"] = _variables
            }).Result;

            return result;
        }

        private T[] FindByIds<T>(IEnumerable<string> ids)
            where T : TBaseElement
        {
            return ids.Select(id => FindById<T>(id)).ToArray();
        }

        private T FindById<T>(string id)
            where T : TBaseElement
        {
            return FindElements<T>()
                .FirstOrDefault(f => f.Id == id);
        }

        private IEnumerable<T> FindElements<T>()
        {
            return _model.RootElement
                .OfType<TProcess>()
                .SelectMany(p => p.FlowElement.OfType<TBaseElement>().Concat(p.Artifact.OfType<TBaseElement>()))
                .OfType<T>();
        }

        private int Hit(TFlowNode flowNode)
        {
            return _nodeHits.AddOrUpdate(flowNode.Id, 1, (k, v) => v + 1);
        }

        private async Task<TDefinitions> GetBpmnDefinition(BPMNOrchestratorInput input)
        {
            var bpmnBytes = await _bpmnProvider.GetBPMN(input.Name);
            using (var stream = new MemoryStream(bpmnBytes))
            {
                var serializer = new XmlSerializerFactory().CreateSerializer(typeof(TDefinitions));
                return serializer.Deserialize(stream) as TDefinitions;
            }
        }

        private async Task WaitForAll(params Task[] tasks)
        {
            await WaitForAll((IEnumerable<Task>)tasks);
        }

        private async Task WaitForAll(IEnumerable<Task> tasks)
        {
            var pending = tasks.ToHashSet();
            while (pending.Count > 0)
            {
                var finishedTask = await Task.WhenAny(pending);
                pending.Remove(finishedTask);
                await finishedTask;
            }
        }

        private async Task WaitForAny(params Task[] tasks)
        {
            await WaitForAny((IEnumerable<Task>)tasks);
        }

        private async Task WaitForAny(IEnumerable<Task> tasks)
        {
            await await Task.WhenAny(tasks);
        }

        public override object OnGetStatus()
        {
            return new
            {
                variables = _variables
            };
        }
    }

    public class BPMNOrchestratorInput
    {
        public string Name { get; set; }
    }
}
