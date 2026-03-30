using LLL.DurableTask.Worker;
using LLL.DurableTask.Worker.Attributes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDurableTaskEFCoreStorage()
    .UseInMemoryDatabase("Sample");
builder.Services.AddDurableTaskClient();
builder.Services.AddDurableTaskWorker(builder =>
{
    builder.AddAnnotatedFrom(typeof(Program).Assembly);
});
builder.Services.AddDurableTaskApi(options =>
{
    options.DisableAuthorization = true;
});
builder.Services.AddDurableTaskUi();

var app = builder.Build();
app.UseRouting();
app.MapDurableTaskApi();
app.UseDurableTaskUi();
app.Run();

[Orchestration(Name = "SingleService")]
public class SingleServiceOrchestration : OrchestrationBase<SimpleDemoResult, SimpleDemoInput>
{
    public override async Task<SimpleDemoResult> Execute(SimpleDemoInput input)
    {
        return await Task.FromResult(new SimpleDemoResult
        {
            OutputText = $"Hello, {input?.InputText ?? "World"}!"
        });
    }
}

public class SimpleDemoInput
{
    public string? InputText { get; set; }
}

public class SimpleDemoResult
{
    public string? OutputText { get; set; }
}
