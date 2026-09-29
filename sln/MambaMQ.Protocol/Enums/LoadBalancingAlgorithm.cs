namespace MambaMQ.Protocol.Enums;

public enum LoadBalancingAlgorithm : byte
{
    Random = 1,
    RoundRobin = 2,
    WeightedRoundRobin = 3,
    LeastInFlight = 4
}