using DeliveryApp.Core.Ports;
using DeliveryApp.Core.Domain.Model.OrderAggregate;
using MediatR;
using Primitives;

namespace DeliveryApp.Core.Application.UseCases.Commands.MoveCouriers;

public class MoveCouriersHandler : IRequestHandler<MoveCouriersCommand, bool>
{
    private readonly ICourierRepository _courierRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MoveCouriersHandler(ICourierRepository courierRepository, IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _courierRepository = courierRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(MoveCouriersCommand message, CancellationToken cancellationToken)
    {
        // Get all couriers, move one tick
        var couriers = await _courierRepository.GetBusy();
        foreach (var courier in couriers)
        {
            // Get first storage place with an order
            var storageWithOrder = courier.StoragePlaces.FirstOrDefault(s => s.OrderId.HasValue);
            
            if (storageWithOrder != null)
            {
                // Fetch the specific order by ID
                var order = await _orderRepository.GetById(storageWithOrder.OrderId.Value);
                courier.Move(order.Location);
            }
        }

        // Save state in a storage
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return true;
    }
}