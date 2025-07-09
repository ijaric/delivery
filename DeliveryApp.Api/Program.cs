using System.Reflection;
using CSharpFunctionalExtensions;
using DeliveryApp.Api.Adapters.BackgroundJobs;
using DeliveryApp.Api;
using DeliveryApp.Core.Application.UseCases.Commands.AssignOrders;
using DeliveryApp.Core.Application.UseCases.Commands.CreateOrder;
using DeliveryApp.Core.Application.UseCases.Commands.CreateCourier;
using DeliveryApp.Core.Application.UseCases.Commands.MoveCouriers;
using DeliveryApp.Core.Application.UseCases.Queries.GetAllCouriers;
using DeliveryApp.Core.Application.UseCases.Queries.GetNotCompletedOrders;
using DeliveryApp.Core.Domain.Services.DispatchService;
using DeliveryApp.Core.Ports;
using DeliveryApp.Infrastructure.Adapters.Postgres;
using DeliveryApp.Infrastructure.Adapters.Postgres.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Primitives;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

// Health Checks
builder.Services.AddHealthChecks();

// Cors
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.AllowAnyOrigin(); // Не делайте так в проде!
        });
});

// Configuration
builder.Services.ConfigureOptions<SettingsSetup>();
var connectionString = builder.Configuration["CONNECTION_STRING"];

// Domain Services
builder.Services.AddSingleton<IDispatchService, DispatchService>();

// БД, ORM 
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseNpgsql(connectionString,
            sqlOptions => { sqlOptions.MigrationsAssembly("DeliveryApp.Infrastructure"); });
        options.EnableSensitiveDataLogging();
    }
);

// UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Repositories
builder.Services.AddScoped<ICourierRepository, CourierRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Mediator
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

// Commands - обновленные с Result<T> pattern
builder.Services.AddScoped<IRequestHandler<CreateOrderCommand, Result<Unit, Error>>, CreateOrderHandler>();
builder.Services.AddScoped<IRequestHandler<CreateCourierCommand, Result<Unit, Error>>, CreateCourierHandler>();
builder.Services.AddScoped<IRequestHandler<MoveCouriersCommand, bool>, MoveCouriersHandler>();
builder.Services.AddScoped<IRequestHandler<AssignOrdersCommand, bool>, AssignOrdersHandler>();

// Queries - переименованные с семантичными именами
builder.Services.AddScoped<IRequestHandler<GetNotCompletedOrdersQuery, GetNotCompletedOrdersResponse>, GetNotCompletedOrdersHandler>();
builder.Services.AddScoped<IRequestHandler<GetAllCouriersQuery, GetAllCouriersResponse>, GetAllCouriersHandler>();

// Controllers
builder.Services.AddControllers()
    .AddNewtonsoftJson();

var app = builder.Build();

// -----------------------------------
// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
    app.UseHsts();

app.UseHealthChecks("/health");
app.UseRouting();

// Apply Migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

// CRON Jobs
builder.Services.AddQuartz(configure =>
{
    var assignOrdersJobKey = new JobKey(nameof(AssignOrdersJob));
    var moveCouriersJobKey = new JobKey(nameof(MoveCouriersJob));
    configure
        .AddJob<AssignOrdersJob>(assignOrdersJobKey)
        .AddTrigger(
            trigger => trigger.ForJob(assignOrdersJobKey)
                .WithSimpleSchedule(
                    schedule => schedule.WithIntervalInSeconds(1)
                        .RepeatForever()))
        .AddJob<MoveCouriersJob>(moveCouriersJobKey)
        .AddTrigger(
            trigger => trigger.ForJob(moveCouriersJobKey)
                .WithSimpleSchedule(
                    schedule => schedule.WithIntervalInSeconds(2)
                        .RepeatForever()));
});
builder.Services.AddQuartzHostedService();

app.Run();