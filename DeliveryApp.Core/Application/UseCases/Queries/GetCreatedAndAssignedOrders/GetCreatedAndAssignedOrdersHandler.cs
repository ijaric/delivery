using MediatR;
using DeliveryApp.Core.Ports;
using DeliveryApp.Core.Domain.Model.OrderAggregate;

namespace DeliveryApp.Core.Application.UseCases.Queries.GetCreatedAndAssignedOrders;

public class GetCreatedAndAssignedOrdersHandler : IRequestHandler<GetCreatedAndAssignedOrdersQuery, GetCreatedAndAssignedOrdersResponse>
{

    private readonly IOrderRepository _orderRepository;

    public GetCreatedAndAssignedOrdersHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }
    public async Task<GetCreatedAndAssignedOrdersResponse> Handle(GetCreatedAndAssignedOrdersQuery request, CancellationToken cancellationToken)
    {
        var domainOrders = await _orderRepository.GetByStatuses(OrderStatus.Created, OrderStatus.Assigned);
        var dtoOrders = domainOrders.Select(MapToDtoOrder).ToList();
        return new GetCreatedAndAssignedOrdersResponse(dtoOrders);
    }

    private static OrderLocation MapToDtoOrder(Order domainOrder)
    {
        return new OrderLocation
        {
            Id = domainOrder.Id,
            Location = new Location
            {
                X = domainOrder.Location.X,
                Y = domainOrder.Location.Y
            }
        };
    }
}