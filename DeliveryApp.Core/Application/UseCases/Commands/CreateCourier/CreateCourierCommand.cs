using MediatR;
using Primitives;
using CSharpFunctionalExtensions;
using System.Collections.Generic;

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
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(name))
            errors.Add("Courier name is required");

        if (speed <= 0)
            errors.Add("Courier speed must be greater than 0");

        if (errors.Count > 0)
            return GeneralErrors.ValueIsInvalid(string.Join(", ", errors));

        return new CreateCourierCommand(name, speed);
    }
}