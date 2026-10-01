namespace MambaMQ.Protocol.Enums;

public enum LoadBalancingAlgorithm : byte
{
    RoundRobin = 1,
    WeightedRoundRobin = 2,
    LeastInFlight = 3,
}