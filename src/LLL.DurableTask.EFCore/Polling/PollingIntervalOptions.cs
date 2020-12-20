namespace LLL.DurableTask.EFCore.Polling
{
    public class PollingIntervalOptions
    {
        public PollingIntervalOptions(double initial, double factor, double max)
        {
            Initial = initial;
            Factor = factor;
            Max = max;
        }

        public double Initial { get; set; }
        public double Factor { get; set; }
        public double Max { get; set; }
    }
}