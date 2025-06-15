using CSharpFunctionalExtensions;
using MediatR;
using Primitives;

namespace StoragePlace;

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
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(nameof(name));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(volume);

        return new StoragePlace(name, volume);
    }

    public Result<bool, Error> CanStore(int volume)
    {
        return (TotalVolume >= volume);
    }

    public UnitResult<Error> Store(Guid orderId, int volume)
    {
        if (volume > TotalVolume) GeneralErrors.ValueIsInvalid("Order volume exceed storage volume");
        if (IsOccupied()) GeneralErrors.ValueIsInvalid("Storage is occupied");

        OrderId = orderId;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Clear(Guid orderId)
    {
        if (OrderId != orderId) GeneralErrors.ValueIsInvalid("This order is not in storage");

        OrderId = null;
        return UnitResult.Success<Error>();
    }

    private bool IsOccupied() => OrderId.HasValue;
}