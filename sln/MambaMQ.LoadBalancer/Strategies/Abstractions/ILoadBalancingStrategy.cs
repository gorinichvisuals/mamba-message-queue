namespace MambaMQ.LoadBalancer.Strategies.Abstractions;

public interface ILoadBalancingStrategy
{
    Subscriber SelectSubscriber(IReadOnlyList<Subscriber> subscribers);
}