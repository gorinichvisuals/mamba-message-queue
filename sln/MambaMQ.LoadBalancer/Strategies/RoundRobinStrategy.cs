namespace MambaMQ.LoadBalancer.Strategies;

internal sealed class RoundRobinStrategy : ILoadBalancingStrategy
{
    private Guid _lastSubscriberId;

    public Subscriber SelectSubscriber(IReadOnlyList<Subscriber> subscribers)
    {
        if (subscribers.Count is 0)
            throw new InvalidOperationException("No subscribers available.");

        int startIndex = 0;

        if (_lastSubscriberId != Guid.Empty)
        {
            for (int i = 0; i < subscribers.Count; i++)
            {
                if (subscribers[i].Connection.Id != _lastSubscriberId)
                    continue;

                startIndex = (i + 1) % subscribers.Count;
                break;
            }
        }
        
        Subscriber subscriber = subscribers[startIndex];

        _lastSubscriberId = subscriber.Connection.Id;

        return subscriber;
    }
}