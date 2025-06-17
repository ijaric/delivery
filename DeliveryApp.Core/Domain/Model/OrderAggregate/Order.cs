using DeliveryApp.Core.Domain.Model.SharedKernel;
using DeliveryApp.Core.Domain.Model.CourierAggregate;
using CSharpFunctionalExtensions;
using Primitives;

namespace DeliveryApp.Core.Domain.Model.OrderAggregate;

public class Order : Aggregate<Guid>
{    
    public Location Location { get; }
    public int Volume { get; }
    public OrderStatus Status { get; private set; }
    public Guid? Courierld { get; private set; }

    private Order() { }

    private Order(Guid orderId, Location location, int volume) : this()
    {
        Id = orderId;
        Location = location;
        Volume = volume;
    }

    public static Result<Order, Error> Create(Guid orderId, Location location, int volume)
    {
        if (volume <= 0) return GeneralErrors.ValueIsInvalid("Volume must be greater than 0");

        var order = new Order(orderId, location, volume);
        order.Status = OrderStatus.Created;
        return order;
    }

    public UnitResult<Error> Assign(Courier courier)
    {
        if (Courierld != null) return GeneralErrors.ValueIsInvalid("Order has Courier");
        if (Status != OrderStatus.Created) return GeneralErrors.ValueIsInvalid("Order must be in created state to be assigned");

        Courierld = courier.Id;
        Status = OrderStatus.Assigned;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Complete()
    {
        if (Status != OrderStatus.Assigned) return GeneralErrors.ValueIsInvalid("Order must be assigned to be completed");

        Status = OrderStatus.Completed;
        return UnitResult.Success<Error>();
    }
}