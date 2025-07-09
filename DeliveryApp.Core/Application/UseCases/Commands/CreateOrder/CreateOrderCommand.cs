using MediatR;
using Primitives;
using CSharpFunctionalExtensions;

namespace DeliveryApp.Core.Application.UseCases.Commands.CreateOrder;

public class CreateOrderCommand : IRequest<Result<Unit, Error>>
{
    private CreateOrderCommand(Guid id, string street, int volume)
    {
        Id = id;
        Street = street;
        Volume = volume;
    }

    public Guid Id { get; }
    public string Street { get; }
    public int Volume { get; }

    public static Result<CreateOrderCommand, Error> Create(Guid id, string street, int volume)
    {
        if (id == Guid.Empty)
            return GeneralErrors.ValueIsRequired(nameof(id));

        if (string.IsNullOrWhiteSpace(street))
            return GeneralErrors.ValueIsRequired(nameof(street));

        if (volume <= 0)
            return GeneralErrors.ValueIsInvalid($"{nameof(volume)} must be greater than 0");

        return new CreateOrderCommand(id, street, volume);
    }
}