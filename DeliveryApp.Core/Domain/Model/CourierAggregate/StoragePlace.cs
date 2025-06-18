using CSharpFunctionalExtensions;
using Primitives;

namespace DeliveryApp.Core.Domain.Model.CourierAggregate;

public class StoragePlace : Entity<Guid>
{
    private StoragePlace()
    { }


    public string Name { get; }
    public int TotalVolume { get; }
    public Guid? OrderId { get; private set; }

    private StoragePlace(string name, int volume) : this()
    {
        Id = Guid.NewGuid();
        Name = name;
        TotalVolume = volume;
    }

    public static Result<StoragePlace, Error> Create(string name, int volume)
    {
        if (string.IsNullOrWhiteSpace(name)) return GeneralErrors.ValueIsInvalid("Provide correct name of a storage");
        if (volume <= 0) return GeneralErrors.ValueIsInvalid("Volume must be greater than 0");

        return new StoragePlace(name, volume);
    }

    public Result<bool, Error> CanStore(int volume)
    {
        if (volume <= 0) return GeneralErrors.ValueIsInvalid("Provide volume greater than 0");

        if (IsOccupied()) return false;

        return (TotalVolume >= volume);
    }

    public UnitResult<Error> Store(Guid orderId, int volume)
    {
        if (volume > TotalVolume) return GeneralErrors.ValueIsInvalid("Order volume exceed storage volume");
        if (IsOccupied()) return GeneralErrors.ValueIsInvalid("Storage is occupied");

        OrderId = orderId;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Clear(Guid orderId)
    {
        if (OrderId != orderId) return GeneralErrors.ValueIsInvalid("This order is not in storage");

        OrderId = null;
        return UnitResult.Success<Error>();
    }

    private bool IsOccupied() => OrderId.HasValue;

}