namespace LPApi
{
    public class CalculationResult
    {
        public long ExecutionTimeMillis { get; set; }
        public bool IsOptimal { get; set; }
        public double[] Weights { get; set; }

        public CalculationResult(long executionTimeMillis, bool isOptimal, double[] weights)
        {
            ExecutionTimeMillis = executionTimeMillis;
            IsOptimal = isOptimal;
            Weights = weights;
        }
    }
}
