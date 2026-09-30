using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using PersonalLibrary.Api.Authentication;
using PersonalLibrary.Api.Contracts.Common;
using PersonalLibrary.Api.Middleware;
using PersonalLibrary.Application.Common.Authentication;
using PersonalLibrary.Application.Identity.Services;
using PersonalLibrary.Application.Libraries.Services;
using PersonalLibrary.Infrastructure;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILibraryService, LibraryService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();

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
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                return WriteFailureResponseAsync(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    "Unauthorized",
                    "A valid access token is required.");
            },
            OnForbidden = context => WriteFailureResponseAsync(
                context.HttpContext,
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "You do not have permission to access this resource.")
        };
    });

builder.Services.AddAuthorization();
builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var details = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "The supplied value is invalid."
                            : error.ErrorMessage)
                        .ToArray());

            return new BadRequestObjectResult(ApiResponse.Failed(
                "ValidationFailed",
                "One or more validation errors occurred.",
                details,
                context.HttpContext.TraceIdentifier));
        };
    });

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter the JWT access token returned by the login endpoint."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseStatusCodePages(async context =>
{
    var (code, message) = context.HttpContext.Response.StatusCode switch
    {
        StatusCodes.Status404NotFound => ("NotFound", "The requested endpoint was not found."),
        StatusCodes.Status405MethodNotAllowed => ("MethodNotAllowed", "The HTTP method is not allowed."),
        _ => ("RequestFailed", "The request could not be completed.")
    };

    await WriteFailureResponseAsync(
        context.HttpContext,
        context.HttpContext.Response.StatusCode,
        code,
        message);
});
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static Task WriteFailureResponseAsync(
    HttpContext context,
    int statusCode,
    string code,
    string message)
{
    if (context.Response.HasStarted)
        return Task.CompletedTask;

    context.Response.StatusCode = statusCode;
    context.Response.ContentType = "application/json";
    return context.Response.WriteAsJsonAsync(
        ApiResponse.Failed(code, message, traceId: context.TraceIdentifier),
        context.RequestAborted);
}
