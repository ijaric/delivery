using DeliveryApp.Core.Domain.Model.OrderAggregate;
using DeliveryApp.Core.Ports;
using MediatR;
using Primitives;
using DeliveryApp.Core.Domain.Model.SharedKernel;
namespace DeliveryApp.Core.Application.UseCases.Commands.CreateOrder;

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<bool> Handle(CreateOrderCommand message, CancellationToken cancellationToken)
    {
        // TODO: Shall we receive order volume from the command?
        // Create order
        var orderCreateResult = Order.Create(message.Id, Location.CreateRandom(), 1);
        if (orderCreateResult.IsFailure) return false;
        var order = orderCreateResult.Value;

        // Save order in a storage
        await _orderRepository.CreateOrder(order);
        return await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}