using FluentValidation;
using FourtitudeMiddleware.Dtos;
using FourtitudeMiddleware.Services;
using log4net;
using log4net.Config;
using System.IO;
using System.Reflection;
using FourtitudeMiddleware.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// log4net configuration
var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register FluentValidation validators
builder.Services.AddSingleton<IValidator<SubmitTransactionRequest>, SubmitTransactionRequestValidator>();

builder.Services.AddSingleton<ITransactionService, TransactionService>();
builder.Services.AddSingleton<IPartnerService, PartnerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseRequestResponseLogging();

app.UseAuthorization();

app.MapControllers();

app.Run();
