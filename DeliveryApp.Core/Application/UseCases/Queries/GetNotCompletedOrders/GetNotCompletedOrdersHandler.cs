using MediatR;
using DeliveryApp.Core.Ports;
using DeliveryApp.Core.Domain.Model.OrderAggregate;

namespace DeliveryApp.Core.Application.UseCases.Queries.GetNotCompletedOrders;

public class GetNotCompletedOrdersHandler : IRequestHandler<GetNotCompletedOrdersQuery, GetNotCompletedOrdersResponse>
{
    private readonly IOrderRepository _orderRepository;

    public GetNotCompletedOrdersHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
    }

    public async Task<GetNotCompletedOrdersResponse> Handle(GetNotCompletedOrdersQuery request, CancellationToken cancellationToken)
    {
        var domainOrders = await _orderRepository.GetByStatuses(OrderStatus.Created, OrderStatus.Assigned);
        var dtoOrders = domainOrders.Select(MapToDtoOrder).ToList();
        return new GetNotCompletedOrdersResponse(dtoOrders);
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