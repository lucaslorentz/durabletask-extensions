namespace LLL.DurableTask.Api.Models
{
    public class RaiseEventRequest
    {
        public string EventName { get; set; }
        public object EventData { get; set; }
    }
}
