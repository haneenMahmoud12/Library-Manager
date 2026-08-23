using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PersonalLibrary.Api.ExceptionHandling;
using PersonalLibrary.Application.Identity.Commands.ConfirmEmail;
using PersonalLibrary.Application.Identity.Commands.Login;
using PersonalLibrary.Application.Identity.Commands.RefreshToken;
using PersonalLibrary.Application.Identity.Commands.Register;
using PersonalLibrary.Application.Identity.Commands.ResendConfirmation;
using PersonalLibrary.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IRegisterUserHandler, RegisterUserHandler>();
builder.Services.AddScoped<ILoginHandler, LoginUserHandler>();
builder.Services.AddScoped<IConfirmEmailHandler, ConfirmEmailHandler>();
builder.Services.AddScoped<IRefreshTokenHandler, RefreshTokenHandler>();
builder.Services.AddScoped<IResendConfirmationHandler, ResendConfirmationHandler>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
            IssuerSigningKeyResolver = (_, _, _, _) =>
            {
                var signingKey = builder.Configuration["Jwt:SigningKey"];
                return string.IsNullOrWhiteSpace(signingKey) ||
                       Encoding.UTF8.GetByteCount(signingKey) < 32
                    ? []
                    : [new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))];
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<RegistrationValidationExceptionHandler>();
builder.Services.AddExceptionHandler<AuthenticationFlowExceptionHandler>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();