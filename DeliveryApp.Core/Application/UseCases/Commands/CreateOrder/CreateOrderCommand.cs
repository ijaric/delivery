using MediatR;

namespace DeliveryApp.Core.Application.UseCases.Commands.CreateOrder;

public class CreateOrderCommand : IRequest<bool>
{
    public CreateOrderCommand(Guid id, string street)
    {
        Id = id;
        Street = street;
    }

    public Guid Id { get; }
    public string Street { get;  }
}