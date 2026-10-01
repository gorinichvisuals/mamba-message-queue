namespace MambaMQ.LoadBalancer.Factories;

public static class LoadBalancingStrategyFactory
{
    public static ILoadBalancingStrategy Create(LoadBalancingAlgorithm algorithm)
    {
        return algorithm switch
        {
            LoadBalancingAlgorithm.RoundRobin => new RoundRobinStrategy(),
            LoadBalancingAlgorithm.WeightedRoundRobin => new WeightedRoundRobinStrategy(),
            LoadBalancingAlgorithm.LeastInFlight => new LeastInFlightStrategy(),

            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null)
        };
    }
}