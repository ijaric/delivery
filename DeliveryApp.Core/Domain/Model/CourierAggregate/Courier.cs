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
        if (string.IsNullOrWhiteSpace(name)) return GeneralErrors.ValueIsRequired("Name");
        if (speed <= 0) return GeneralErrors.ValueIsInvalid("Speed must be greater than 0");

        var defaultStorageResult = StoragePlace.Create("Сумка", 10);
        if (defaultStorageResult.IsFailure) return defaultStorageResult.Error;
        
        return new Courier(name, speed, location, defaultStorageResult.Value);
    }

    public UnitResult<Error> AddStoragePlace(string name, int volume)
    {
        var storagePlace = StoragePlace.Create(name, volume);
        StoragePlaces.Add(storagePlace.Value);
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> CanTakeOrder(Order order)
    {
        if (order == null) return GeneralErrors.ValueIsRequired("Order");

        var hasEnoughSpace = StoragePlaces.Any(s => s.CanStore(order.Volume).Value);
        if (!hasEnoughSpace) return GeneralErrors.ValueIsInvalid("No storage place has enough volume for this order");

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> TakeOrder(Order order)
    {
        if (order == null) return GeneralErrors.ValueIsRequired("Order");

        var storageToUse = StoragePlaces.FirstOrDefault(s => s.CanStore(order.Volume).Value);
        if (storageToUse == null) return GeneralErrors.ValueIsInvalid("No available storage place for this order");

        // Assign order to courier
        var assignResult = order.Assign(this);
        if (assignResult.IsFailure) return assignResult.Error;

        // Store order in storage place
        return storageToUse.Store(order.Id, order.Volume);
    }

    public UnitResult<Error> CompleteOrder(Order order)
    {
        if (order == null) return GeneralErrors.ValueIsRequired("Order");

        var storage = StoragePlaces.FirstOrDefault(s => s.OrderId == order.Id);
        if (storage == null) return GeneralErrors.InternalServerError($"Could not find storage for order {order.Id}");
        order.Complete();
        
        return storage.Clear(order.Id);
    }


    public Result<double, Error> CalculateTimeToLocation(Location location)
    {
        var distance = Location.DistanceTo(location);
        return distance / Speed;
    }

    public UnitResult<Error> Move(Location target)
    {
        if (Location == target) return GeneralErrors.ValueIsInvalid("Courier already at target");

        var stepsLeft = Speed;

        // Move one cell at a time until limit is reached or target is reached
        while (stepsLeft-- > 0 && Location != target)
        {
            var dx = target.X - Location.X;
            var dy = target.Y - Location.Y;

            // Move along the axis with the greater distance first
            if (Math.Abs(dx) >= Math.Abs(dy) && dx != 0)
            {
                Location = new Location(Location.X + Math.Sign(dx), Location.Y);
            }
            else if (dy != 0)
            {
                Location = new Location(Location.X, Location.Y + Math.Sign(dy));
            }
        }

        return UnitResult.Success<Error>();
    }
}