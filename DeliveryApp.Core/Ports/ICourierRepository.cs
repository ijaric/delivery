using CSharpFunctionalExtensions;
using Primitives;
using DeliveryApp.Core.Domain.Model.CourierAggregate;

namespace DeliveryApp.Core.Ports;

public interface ICourierRepository
{
    public Task CreateCourier(Courier courier);

    public void UpdateCourier(Courier courier);

    public Task<Courier> GetById(Guid id);

    public Task<Courier[]> GetAvailable();
}