using CSharpFunctionalExtensions;

namespace DeliveryApp.Core.Domain.Model.OrderAggregate;

public class OrderStatus : ValueObject
{
    public static OrderStatus Created => new(nameof(Created).ToLowerInvariant());
    public static OrderStatus Assigned => new(nameof(Assigned).ToLowerInvariant());
    public static OrderStatus Completed => new(nameof(Completed).ToLowerInvariant());

    public string Name { get; }
    private OrderStatus()
    { }

    private OrderStatus(string name) : this()
    {
        Name = name;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
    }
}