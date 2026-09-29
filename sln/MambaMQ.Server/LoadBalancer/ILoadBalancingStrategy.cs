namespace MambaMQ.Server.LoadBalancer;

public interface ILoadBalancingStrategy
{
    Subscriber Select(IReadOnlyList<Subscriber> subscribers);
}