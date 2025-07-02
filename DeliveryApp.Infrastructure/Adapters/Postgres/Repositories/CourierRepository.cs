using Microsoft.EntityFrameworkCore;
using DeliveryApp.Core.Ports;
using DeliveryApp.Core.Domain.Model.CourierAggregate;

namespace DeliveryApp.Infrastructure.Adapters.Postgres.Repositories;

public class CourierRepository : ICourierRepository
{
    public readonly ApplicationDbContext _dbContext;

    public CourierRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task CreateCourier(Courier courier)
    {
        await _dbContext.Couriers.AddAsync(courier);
    }

    public void UpdateCourier(Courier courier)
    {
        _dbContext.Update(courier);
    }

    public async Task<Courier> GetById(Guid id)
    {
        return await _dbContext.Couriers.FindAsync(id);
    }

    public async Task<Courier[]> GetAvailable()
    {
        return await _dbContext.Couriers.Where(c => c.StoragePlaces.All(s => s.OrderId == null)).ToArrayAsync();
    }
}