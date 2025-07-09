using MediatR;
using Primitives;
using CSharpFunctionalExtensions;

namespace DeliveryApp.Core.Application.UseCases.Commands.CreateCourier;

public class CreateCourierCommand : IRequest<Result<Unit, Error>>
{
    private CreateCourierCommand(string name, int speed)
    {
        Name = name;
        Speed = speed;
    }

    public string Name { get; }
    public int Speed { get; }

    public static Result<CreateCourierCommand, Error> Create(string name, int speed)
    {
        if (string.IsNullOrWhiteSpace(name))
            return GeneralErrors.ValueIsRequired(nameof(name));

        if (speed <= 0)
            return GeneralErrors.ValueIsInvalid($"{nameof(speed)} must be greater than 0");

        return new CreateCourierCommand(name, speed);
    }
}