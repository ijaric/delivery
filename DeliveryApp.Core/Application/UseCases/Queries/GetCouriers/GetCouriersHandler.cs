using MediatR;
using DeliveryApp.Core.Ports;
using DeliveryApp.Core.Domain.Model.CourierAggregate;

namespace DeliveryApp.Core.Application.UseCases.Queries.GetCouriers;

public class GetCouriersHandler : IRequestHandler<GetCouriersQuery, GetCouriersResponse>
{
    private readonly ICourierRepository _couirerRepository;

    public GetCouriersHandler(ICourierRepository courierRepository)
    {
        _couirerRepository = courierRepository;
    }

    public async Task<GetCouriersResponse> Handle(GetCouriersQuery request, CancellationToken cancellationToken)
    {
        var domainCouriers = await _couirerRepository.GetBusy();
        var dtoCouriers = domainCouriers.Select(MapToDtoCourier).ToList();
        return new GetCouriersResponse(dtoCouriers);
    }

    private static CourierLocation MapToDtoCourier(Courier courier)
    {
        return new CourierLocation
        {
            Id = courier.Id,
            Location = new Location
            {
                X = courier.Location.X,
                Y = courier.Location.Y
            }
        };
    }
}