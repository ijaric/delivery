using DeliveryApp.Api;
using DeliveryApp.Core.Domain.Services;
using DeliveryApp.Core.Ports;
using DeliveryApp.Infrastructure.Adapters.Postgres;
using DeliveryApp.Infrastructure.Adapters.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Primitives;

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

app.Run();