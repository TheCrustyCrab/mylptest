namespace LPApi
{
    public record CalculationResult(long ExecutionTimeMillis, bool IsOptimal, double[] Weights, double ObjectiveValue);
}
