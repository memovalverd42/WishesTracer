using WishesTracer.Application;
using WishesTracer.Extensions;
using WishesTracer.Infraestructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGlobalExceptionHandling();
builder.Services.AddControllers();

builder.Services.AddApplication();

builder.Services.AddInfrastructure(builder.Configuration);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.Run();
