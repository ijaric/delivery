using DeliveryApp.Core.Domain.Model.CourierAggregate;
using DeliveryApp.Core.Domain.Model.SharedKernel;
using DeliveryApp.Core.Ports;
using MediatR;
using Primitives;
using CSharpFunctionalExtensions;

namespace DeliveryApp.Core.Application.UseCases.Commands.CreateCourier;

public class CreateCourierHandler : IRequestHandler<CreateCourierCommand, Result<Unit, Error>>
{
    private readonly ICourierRepository _courierRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCourierHandler(ICourierRepository courierRepository, IUnitOfWork unitOfWork)
    {
        _courierRepository = courierRepository ?? throw new ArgumentNullException(nameof(courierRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<Unit, Error>> Handle(CreateCourierCommand command, CancellationToken cancellationToken)
    {
        // Создаем курьера в случайной локации
        var location = Location.CreateRandom();
        var courierResult = Courier.Create(command.Name, command.Speed, location);
        
        if (courierResult.IsFailure) 
            return courierResult.Error;
        
        var courier = courierResult.Value;
        
        // Сохраняем курьера
        await _courierRepository.CreateCourier(courier);
        
        var saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (!saveResult)
            return GeneralErrors.InternalServerError("Failed to save courier to database");
        
        return Unit.Value;
    }
}