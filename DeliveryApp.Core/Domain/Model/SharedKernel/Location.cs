using System.Diagnostics.CodeAnalysis;
using CSharpFunctionalExtensions;

namespace DeliveryApp.Core.Domain.Model.SharedKernel;

/// <summary>
/// Представляет точку на поле размером 10×10.
/// Иммутабельный Value Object с координатами X и Y.
/// </summary>
public class Location : ValueObject
{
    /// <summary>
    /// Координата по оси X (от 1 до 10).
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Координата по оси Y (от 1 до 10).
    /// </summary>
    public int Y { get; }

    [ExcludeFromCodeCoverage]
    private Location()
    { }

    /// <summary>
    /// Создаёт экземпляр <see cref="Location"/> с валидацией координат.
    /// </summary>
    /// <param name="x">Координата X, должна быть от 1 до 10.</param>
    /// <param name="y">Координата Y, должна быть от 1 до 10.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Выбрасывается, если любая из координат вне диапазона 1..10.
    /// </exception>
    public Location(int x, int y)
    {
        if (x < 1 || x > 10)
            throw new ArgumentOutOfRangeException(nameof(x));
        if (y < 1 || y > 10)
            throw new ArgumentOutOfRangeException(nameof(y));

        X = x;
        Y = y;
    }

    /// <summary>
    /// Вычисляет манхэттенское (решётчатое) расстояние до другой точки.
    /// </summary>
    /// <param name="other">Другая точка <see cref="Location"/>.</param>
    /// <returns>Сумма по модулю разниц координат X и Y.</returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="other"/> равен <c>null</c>.
    /// </exception>
    public int DistanceTo(Location other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
    }

    /// <summary>
    /// Генерирует случайную точку на поле 10×10.
    /// </summary>
    /// <returns>Новый экземпляр <see cref="Location"/> со случайными координатами.</returns>
    public static Location CreateRandom()
    {
        var random = new Random();
        return new Location(random.Next(1, 11), random.Next(1, 11));
    }

    [ExcludeFromCodeCoverage]
    protected override IEnumerable<IComparable> GetEqualityComponents()
    {
        yield return X;
        yield return Y;
    }
}