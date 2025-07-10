using MediatR;
using DeliveryApp.Core.Ports;
using DeliveryApp.Core.Domain.Model.CourierAggregate;

namespace DeliveryApp.Core.Application.UseCases.Queries.GetAllCouriers;

public class GetAllCouriersHandler : IRequestHandler<GetAllCouriersQuery, GetAllCouriersResponse>
{
    private readonly ICourierRepository _courierRepository;

    public GetAllCouriersHandler(ICourierRepository courierRepository)
    {
        _courierRepository = courierRepository ?? throw new ArgumentNullException(nameof(courierRepository));
    }

    public async Task<GetAllCouriersResponse> Handle(GetAllCouriersQuery request, CancellationToken cancellationToken)
    {
        var domainCouriers = await _courierRepository.GetAll();
        var dtoCouriers = domainCouriers.Select(MapToDtoCourier).ToList();
        return new GetAllCouriersResponse(dtoCouriers);
    }

    private static CourierLocation MapToDtoCourier(Courier courier)
    {
        return new CourierLocation
        {
            Id = courier.Id,
            Name = courier.Name, // Исправил: добавил поле Name
            Location = new Location
            {
                X = courier.Location.X,
                Y = courier.Location.Y
            }
        };
    }
} 