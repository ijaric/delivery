using System.Diagnostics.CodeAnalysis;
using CSharpFunctionalExtensions;
using Primitives;

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
    public int X { get; private set; }

    /// <summary>
    /// Координата по оси Y (от 1 до 10).
    /// </summary>
    public int Y { get; private set; }

    [ExcludeFromCodeCoverage]
    private Location()
    { }

    /// <summary>
    /// Создаёт экземпляр <see cref="Location"/> с валидацией координат.
    /// </summary>
    /// <param name="x_value">Координата X, должна быть от 1 до 10.</param>
    /// <param name="y_value">Координата Y, должна быть от 1 до 10.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Выбрасывается, если любая из координат вне диапазона 1..10.
    /// </exception>
    public Location(int x_value, int y_value)
    {
        if (x_value < 1 || x_value > 10)
            throw new ArgumentOutOfRangeException(nameof(x_value), "Value must be within 1..10");
        if (y_value < 1 || y_value > 10)
            throw new ArgumentOutOfRangeException(nameof(y_value), "Value must be within 1..10");

        X = x_value;
        Y = y_value;
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
        if (other is null)
            throw new ArgumentNullException(nameof(other));

        int dx = X - other.X;
        int dy = Y - other.Y;
        return Math.Abs(dx) + Math.Abs(dy);
    }

    /// <summary>
    /// Генерирует случайную точку на поле 10×10.
    /// </summary>
    /// <returns>Новый экземпляр <see cref="Location"/> со случайными координатами.</returns>
    public static Location CreateRandom()
    {
        int x = Random.Shared.Next(1, 11);
        int y = Random.Shared.Next(1, 11);
        return new Location(x, y);
    }

    [ExcludeFromCodeCoverage]
    protected override IEnumerable<IComparable> GetEqualityComponents()
    {
        yield return X;
        yield return Y;
    }
}