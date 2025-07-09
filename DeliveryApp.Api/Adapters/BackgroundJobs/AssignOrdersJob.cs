using DeliveryApp.Core.Application.UseCases.Commands.AssignOrders;
using MediatR;
using Quartz;

namespace DeliveryApp.Api.Adapters.BackgroundJobs;

[DisallowConcurrentExecution]
public class AssignOrdersJob : IJob
{
    private readonly IMediator _mediator;

    public AssignOrdersJob(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var assignOrdersCommand = new AssignOrdersCommand();
        await _mediator.Send(assignOrdersCommand);
    }
}