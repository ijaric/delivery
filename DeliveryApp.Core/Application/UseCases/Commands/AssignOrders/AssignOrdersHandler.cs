using DeliveryApp.Core.Ports;
using DeliveryApp.Core.Domain.Model.OrderAggregate;
using MediatR;
using Primitives;
using DeliveryApp.Core.Domain.Services.DispatchService;

namespace DeliveryApp.Core.Application.UseCases.Commands.AssignOrders;

public class AssignOrdersHandler : IRequestHandler<AssignOrdersCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICourierRepository _courierRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDispatchService _dispatchService;

    public AssignOrdersHandler(IOrderRepository orderRepository, ICourierRepository courierRepository, IUnitOfWork unitOfWork, IDispatchService dispatchService)
    {
        _orderRepository = orderRepository;
        _courierRepository = courierRepository;
        _unitOfWork = unitOfWork;
        _dispatchService = dispatchService;
    }

    public async Task<bool> Handle(AssignOrdersCommand message, CancellationToken cancellationToken)
    {
        // Get available couriers and unassigned orders
        var availableCouriers = await _courierRepository.GetAvailable();
        var unassignedOrders = await _orderRepository.GetByStatuses(OrderStatus.Created); // Only Created, not Assigned
        
        // Process each unassigned order
        foreach (var order in unassignedOrders)
        {
            if (availableCouriers.Length == 0) break; // No more couriers available
            
            // Use dispatch service to find best courier and assign order
            var dispatchResult = _dispatchService.Dispatch(order, availableCouriers);
            
            if (dispatchResult.IsSuccess)
            {
                // Order was assigned successfully
                var assignedCourier = dispatchResult.Value;
                
                // Update repositories
                _orderRepository.UpdateOrder(order);
                _courierRepository.UpdateCourier(assignedCourier);
                
                // Remove assigned courier from available list for next iterations
                availableCouriers = availableCouriers.Where(c => c.Id != assignedCourier.Id).ToArray();
            }
            // If dispatch fails, skip this order and continue with next
        }
        
        // Save all changes in one transaction
        return await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}