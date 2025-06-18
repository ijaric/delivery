using DeliveryApp.Core.Domain.Model.SharedKernel;
using CSharpFunctionalExtensions;
using Primitives;
using DeliveryApp.Core.Domain.Model.OrderAggregate;

namespace DeliveryApp.Core.Domain.Model.CourierAggregate;

public class Courier : Aggregate<Guid>
{
    public string Name { get; }
    public int Speed { get; }
    public Location Location { get; private set; }
    public List<StoragePlace> StoragePlaces { get; private set; }

    private Courier() { }

    private Courier(string name, int speed, Location location, StoragePlace storagePlace)
    {
        Name = name;
        Speed = speed;
        Location = location;
        StoragePlaces = [storagePlace];
    }

    public static Result<Courier, Error> Create(string name, int speed, Location location)
    {
        if (string.IsNullOrEmpty(name)) return GeneralErrors.ValueIsRequired("Name");
        if (speed <= 0) return GeneralErrors.ValueIsInvalid("Speed must be greater than 0");

        var defaultStorage = StoragePlace.Create("Сумка", 10).Value;
        return new Courier(name, speed, location, defaultStorage);
    }

    public UnitResult<Error> AddStoragePlace(string name, int volume)
    {
        var storagePlace = StoragePlace.Create(name, volume);
        StoragePlaces.Add(storagePlace.Value);
        return UnitResult.Success<Error>();
    }

    public Result<bool, Error> CanTakeOrder(Order order)
    {
        if (order == null) return GeneralErrors.ValueIsRequired("Order");

        var hasEnoughSpace = StoragePlaces.Any(s => s.CanStore(order.Volume).Value);
        if (!hasEnoughSpace) return GeneralErrors.ValueIsInvalid("No storage place has enough volume for this order");

        return true;
    }

    public UnitResult<Error> TakeOrder(Order order)
    {
        if (order == null) return GeneralErrors.ValueIsRequired("Order");

        var canTakeOrderResult = CanTakeOrder(order);
        if (canTakeOrderResult.IsFailure || canTakeOrderResult.Value == false)
            return GeneralErrors.ValueIsInvalid("This courier doesn't have enough storage");

        // Assign order to courier
        order.Assign(this);
        // Store order in storage place
        StoragePlaces.First(s => s.CanStore(order.Volume).Value).Store(order.Id, order.Volume);

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> CompleteOrder(Order order)
    {
        if (order == null) return GeneralErrors.ValueIsRequired("Order");

        order.Complete();
        StoragePlaces.First(s => s.OrderId == order.Id).Clear(order.Id);
        return UnitResult.Success<Error>();
    }

    public Result<double, Error> CalculateTimeToLocation(Location location)
    {
        var distance = Location.DistanceTo(location);
        return distance / Speed;
    }

    public UnitResult<Error> Move(Location target)
    {
        if (Location == target) return GeneralErrors.ValueIsInvalid("Couirer already at target");

        Location = target;
        return UnitResult.Success<Error>();
    }
}