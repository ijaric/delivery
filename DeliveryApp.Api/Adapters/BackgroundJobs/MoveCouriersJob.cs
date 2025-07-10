using DeliveryApp.Core.Application.UseCases.Commands.MoveCouriers;
using MediatR;
using Quartz;

namespace DeliveryApp.Api.Adapters.BackgroundJobs;

[DisallowConcurrentExecution]
public class MoveCouriersJob : IJob
{
    private readonly IMediator _mediator;
    private readonly ILogger<MoveCouriersJob> _logger;

    public MoveCouriersJob(IMediator mediator, ILogger<MoveCouriersJob> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("Starting courier movement job");
            var moveCourierToOrderCommand = new MoveCouriersCommand();
            var result = await _mediator.Send(moveCourierToOrderCommand);
            _logger.LogInformation("Courier movement job completed. Result: {Result}", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during courier movement job execution");
            throw;
        }
    }
}