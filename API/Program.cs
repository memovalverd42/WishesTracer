// Program.cs - Application entry point and configuration
using Hangfire;
using MediatR;
using Serilog;
using Serilog.Events;
using System.Reflection;
using WishesTracer.Application;
using WishesTracer.Application.Features.Products.Commands.CheckProductPrices;
using WishesTracer.Extensions;
using WishesTracer.Infraestructure;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for structured logging
builder.Host.UseSerilog();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

// Register global exception handling middleware
builder.Services.AddGlobalExceptionHandling();
builder.Services.AddControllers();

// Register Application layer services (MediatR, behaviors)
builder.Services.AddApplication();

// Register Infrastructure layer services (DB, Redis, Hangfire, Scrapers)
builder.Services.AddInfrastructure(builder.Configuration);

// Configure Swagger/OpenAPI for API documentation
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Configure exception handling middleware
app.UseGlobalExceptionHandling();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Configure Hangfire dashboard for background job monitoring
app.UseHangfire();

// Configure recurring background job for price monitoring
// Uses the Service Scope pattern to access scoped services from singleton context
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    // Schedule hourly price check job using CRON expression
    // Job ID "check-prices" ensures single recurring instance
    // Cron.Hourly: runs every hour at minute 0
    recurringJobManager.AddOrUpdate<IMediator>(
        "check-prices", 
        mediator => mediator.Send(new CheckProductPricesCommand(), CancellationToken.None), 
        Cron.Hourly
    );
}

app.Run();
