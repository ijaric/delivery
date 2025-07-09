using MediatR;
using Primitives;
using CSharpFunctionalExtensions;
using System.Collections.Generic;

namespace DeliveryApp.Core.Application.UseCases.Commands.CreateOrder;

public class CreateOrderCommand : IRequest<Result<Unit, Error>>
{
    private CreateOrderCommand(int x, int y, int volume)
    {
        X = x;
        Y = y;
        Volume = volume;
    }

    public int X { get; }
    public int Y { get; }
    public int Volume { get; }

    public static Result<CreateOrderCommand, Error> Create(int x, int y, int volume)
    {
        var errors = new List<string>();
        
        if (x < 1 || x > 10)
            errors.Add("X coordinate must be between 1 and 10");
            
        if (y < 1 || y > 10)
            errors.Add("Y coordinate must be between 1 and 10");
            
        if (volume < 1)
            errors.Add("Volume must be greater than 0");

        if (errors.Count > 0)
            return GeneralErrors.ValueIsInvalid(string.Join(", ", errors));

        return new CreateOrderCommand(x, y, volume);
    }
}