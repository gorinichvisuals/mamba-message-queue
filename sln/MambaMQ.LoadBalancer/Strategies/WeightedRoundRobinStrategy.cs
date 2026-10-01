namespace MambaMQ.LoadBalancer.Strategies;

internal sealed class WeightedRoundRobinStrategy : ILoadBalancingStrategy
{
    private readonly Dictionary<Guid, int> _currentWeights = [];

    public Subscriber SelectSubscriber(IReadOnlyList<Subscriber> subscribers)
    {
        if (subscribers.Count is 0)
            throw new InvalidOperationException("No subscribers available.");

        Subscriber? selectedSubscriber = null;
        int selectedWeight = int.MinValue;
        int totalWeight = 0;

        foreach (Subscriber subscriber in subscribers)
        {
            if (subscriber.Weight <= 0)
                throw new InvalidOperationException($"Subscriber '{subscriber.Connection.Id}' has invalid weight '{subscriber.Weight}'.");

            Guid subscriberId = subscriber.Connection.Id;

            int currentWeight = _currentWeights.GetValueOrDefault(subscriberId, 0);

            currentWeight += subscriber.Weight;
            _currentWeights[subscriberId] = currentWeight;

            totalWeight += subscriber.Weight;

            if (currentWeight <= selectedWeight)
                continue;

            selectedWeight = currentWeight;
            selectedSubscriber = subscriber;
        }

        _currentWeights[selectedSubscriber!.Connection.Id] -= totalWeight;

        return selectedSubscriber;
    }
}