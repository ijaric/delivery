using DeliveryApp.Core.Application.UseCases.Commands.AssignOrders;
using MediatR;
using Quartz;

namespace DeliveryApp.Api.Adapters.BackgroundJobs;

[DisallowConcurrentExecution]
public class AssignOrdersJob : IJob
{
    private readonly IMediator _mediator;
    private readonly ILogger<AssignOrdersJob> _logger;

    public AssignOrdersJob(IMediator mediator, ILogger<AssignOrdersJob> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("Starting order assignment job");
            var assignOrdersCommand = new AssignOrdersCommand();
            var result = await _mediator.Send(assignOrdersCommand);
            _logger.LogInformation("Order assignment job completed. Result: {Result}", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during order assignment job execution");
            throw;
        }
    }
}