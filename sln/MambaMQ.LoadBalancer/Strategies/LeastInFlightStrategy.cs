namespace MambaMQ.LoadBalancer.Strategies;

internal sealed class LeastInFlightStrategy : ILoadBalancingStrategy
{
    public Subscriber SelectSubscriber(IReadOnlyList<Subscriber> subscribers)
    {
        if (subscribers.Count is 0)
            throw new InvalidOperationException("No subscribers available.");

        Subscriber selectedSubscriber = subscribers[0];

        for (int i = 1; i < subscribers.Count; i++)
        {
            Subscriber subscriber = subscribers[i];

            if (subscriber.InFlight < selectedSubscriber.InFlight)
                selectedSubscriber = subscriber;
        }

        return selectedSubscriber;
    }
}