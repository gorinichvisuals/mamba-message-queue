namespace MambaMQ.Server.LoadBalancer;

internal sealed class RandomStrategy : ILoadBalancingStrategy
{
    public Subscriber Select(IReadOnlyList<Subscriber> subscribers)
    {
        return subscribers.Count is 0 
            ? throw new InvalidOperationException("No subscribers available.") 
            : subscribers[Random.Shared.Next(subscribers.Count)];
    }
}