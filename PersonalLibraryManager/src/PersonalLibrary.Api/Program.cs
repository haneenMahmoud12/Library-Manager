using PersonalLibrary.Api.ExceptionHandling;
using PersonalLibrary.Application.Identity.Commands;
using PersonalLibrary.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IRegisterUserHandler, RegisterUserHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<RegistrationValidationExceptionHandler>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();