using DeliveryApp.Core.Domain.Model.SharedKernel;
using CSharpFunctionalExtensions;
using Primitives;
using DeliveryApp.Core.Domain.Model.OrderAggregate;

namespace DeliveryApp.Core.Domain.Model.CourierAggregate;

public class Courier : Aggregate<Guid>
{
    public string Name { get; }
    public int Speed { get; }
    public Location Location { get; }
    public List<StoragePlace> StoragePlaces { get; }

    private Courier() { }

    private Courier(string name, int speed, Location location, StoragePlace storagePlace)
    {
        Name = name;
        Speed = speed;
        Location = location;
        StoragePlaces.Add(storagePlace);
    }

    public static Result<Courier, Error> Create(string name, int speed, Location location)
    {
        if (string.IsNullOrEmpty(name)) return GeneralErrors.ValueIsInvalid("Provide a valid name");
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
}
