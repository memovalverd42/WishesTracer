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

builder.Host.UseSerilog();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Services.AddGlobalExceptionHandling();
builder.Services.AddControllers();

builder.Services.AddApplication();

builder.Services.AddInfrastructure(builder.Configuration);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

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

app.UseHangfire();

// 2. Programar el Job Recurrente
// Usamos el Service Scope de la app para acceder a los servicios
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    // "check-prices" es el ID único del job
    // Cron.Hourly: Se ejecuta cada hora. Puedes poner Cron.Minutely para probar.
    recurringJobManager.AddOrUpdate<IMediator>(
        "check-prices", 
        mediator => mediator.Send(new CheckProductPricesCommand(), CancellationToken.None), 
        Cron.Hourly
    );
}

app.Run();
