namespace DeliveryApp.Core.Application.UseCases.Queries.GetCouriers;

public class GetCouriersResponse
{
    public GetCouriersResponse(List<CourierLocation> couriers)
    {
        Couriers.AddRange(couriers);
    }

    public List<CourierLocation> Couriers { get; set; } = new();
}

public class CourierLocation
{
    /// <summary>
    ///     Идентификатор
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Имя
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Геопозиция (X,Y)
    /// </summary>
    public Location Location { get; set; }
}

public class Location
{
    /// <summary>
    ///     Горизонталь
    /// </summary>
    public int X { get; set; }

    /// <summary>
    ///     Вертикаль
    /// </summary>
    public int Y { get; set; }
}