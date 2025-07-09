using DeliveryApp.Core.Domain.Model.OrderAggregate;
using DeliveryApp.Core.Ports;
using MediatR;
using Primitives;
using DeliveryApp.Core.Domain.Model.SharedKernel;
using CSharpFunctionalExtensions;

namespace DeliveryApp.Core.Application.UseCases.Commands.CreateOrder;

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Result<Unit, Error>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<Unit, Error>> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        // Create order with specified coordinates and volume
        var location = new Location(command.X, command.Y);
        var orderCreateResult = Order.Create(Guid.NewGuid(), location, command.Volume);
        if (orderCreateResult.IsFailure) 
            return orderCreateResult.Error;
        
        var order = orderCreateResult.Value;

        // Save order in a storage
        await _orderRepository.CreateOrder(order);
        
        var saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (!saveResult)
            return GeneralErrors.InternalServerError("Failed to save order to database");
        
        return Unit.Value;
    }
}