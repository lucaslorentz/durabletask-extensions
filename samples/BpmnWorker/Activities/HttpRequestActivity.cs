using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DurableTask.Core;
using LLL.DurableTask.Worker;
using Newtonsoft.Json;

namespace BpmnWorker.Activities
{
    public class HttpRequestActivity : DistributedAsyncTaskActivity<HttpRequestActivity.Input, HttpRequestActivity.Output>
    {
        protected override async Task<Output> ExecuteAsync(TaskContext context, Input input)
        {
            var httpRequest = new HttpRequestMessage
            {
                RequestUri = input.Url,
                Method = input.Method
            };

            if (input.Headers != null)
            {
                foreach (var header in input.Headers)
                {
                    httpRequest.Headers.Add(header.Key, header.Value);
                }
            }

            if (input.Content != null)
            {
                var json = JsonConvert.SerializeObject(input.Content);
                httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            using (var httpClient = new HttpClient())
            {
                var httpResponse = await httpClient.SendAsync(httpRequest);

                string contentRaw = null;
                object content = null;

                if (httpResponse.Content != null)
                {
                    contentRaw = await httpResponse.Content.ReadAsStringAsync();

                    content = httpResponse.Content.Headers.ContentType.MediaType switch
                    {
                        "application/json" => JsonConvert.DeserializeObject(contentRaw),
                        _ => null
                    };
                }

                return new Output
                {
                    StatusCode = (int)httpResponse.StatusCode,
                    Headers = httpResponse.Headers
                        .ToDictionary(h => h.Key, h => h.Value.FirstOrDefault()),
                    Content = content,
                    ContentRaw = contentRaw
                };
            }
        }

        public class Input
        {
            public Uri Url { get; set; }
            public HttpMethod Method { get; set; } = HttpMethod.Get;
            public Dictionary<string, string> Headers { get; set; }
            public string ContentType { get; set; }
            public object Content { get; set; }
        }

        public class Output
        {
            public int StatusCode { get; set; }
            public Dictionary<string, string> Headers { get; set; }
            public object Content { get; set; }
            public string ContentRaw { get; set; }
        }
    }
}
