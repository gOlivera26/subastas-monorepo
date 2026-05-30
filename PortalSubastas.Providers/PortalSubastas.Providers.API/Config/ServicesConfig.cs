using PortalSubastas.Providers.Application.AutoMapper;
using PortalSubastas.Providers.Application.Services.Implementations;
using PortalSubastas.Providers.Application.Services.Interfaces;
using PortalSubastas.Providers.Domain.Models;

namespace PortalSubastas.Providers.API.Config;

public static class ServicesConfig
{
    public static void AddConfig(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                if (allowedOrigins != null && allowedOrigins.Any())
                {
                    builder.WithOrigins(allowedOrigins)
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials();
                }
                else
                {
                    builder.SetIsOriginAllowed(_ => true)
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials();
                }
            });
        });

        services.AddSwagger();
        services.AddJwt(configuration);

        services.AddAutoMapper(typeof(ProviderProfile).Assembly);

        services.AddDbContext<ProvidersContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddRabbitMq(configuration);

        services.AddInternalServices();

        services.AddControllers();

        services.AddHealthChecks();

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());
    }

    private static void AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                var host = configuration["RabbitMq:Host"] ?? "localhost";
                var user = configuration["RabbitMq:Username"] ?? "guest";
                var pass = configuration["RabbitMq:Password"] ?? "guest";

                cfg.Host(host, "/", h =>
                {
                    h.Username(user);
                    h.Password(pass);
                });
            });
        });
    }

    private static void AddJwt(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters()
            {
                RequireExpirationTime = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"] ?? "PortalSubastas.Identity",
                ValidAudience = configuration["Jwt:Audience"] ?? "PortalSubastas.Frontend",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(
                    configuration["Jwt:SecretKey"] ?? "YourSecretKeyHere12345")),
                ClockSkew = TimeSpan.Zero,
            };

            options.Events = new JwtBearerEvents()
            {
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync(JsonSerializer.Serialize(
                        new { success = false, message = "No estas autenticado.", code = 401 }));
                },
                OnForbidden = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync(JsonSerializer.Serialize(
                        new { success = false, message = "No tienes permisos para realizar esta accion.", code = 403 }));
                }
            };
        });
    }

    private static void AddSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "PortalSubastas.Providers API",
                Version = "v1"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Ingrese el token de autenticacion en el siguiente formato: Bearer {token}",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
    }

    private static void AddInternalServices(this IServiceCollection services)
    {
        services.AddScoped<IProviderService, ProviderService>();
        services.AddScoped<IDomicilioService, DomicilioService>();
        services.AddScoped<ICatalogoService, CatalogoService>();
        services.AddScoped<IAfipService, AfipService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IRubroService, RubroService>();
    }
}
