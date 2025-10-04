using FinancialSystem.Application.Services.EnvironmentServices;
using FinancialSystem.Application.Services.UserServices;
using FinancialSystem.Application.Shared.Interfaces;
using FinancialSystem.Application.Shared.Interfaces.EnvironmentServices;
using FinancialSystem.Application.Shared.Interfaces.UserServices;
using FinancialSystem.Core.Settings;
using FinancialSystem.EntityFrameworkCore.Context;
using FinancialSystem.EntityFrameworkCore.Repositories;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using FinancialSystem.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
       .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
       .AddEnvironmentVariables();

builder.Services.AddDbContext<DataContext>();

var myAllowSpecificOrigins = "_myAllowSpecificOrigins";
var configuration = builder.Configuration;
var environment = builder.Environment;

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
                      builder =>
                      {
                          builder.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                      });
});

// Get Environment Name
var envName = configuration.GetSection("EnvironmentName").Value;
var versionName = configuration.GetSection("VersionName").Value;

// Swagger + JWT
builder.Services.AddSwaggerGen(swagger =>
{
    swagger.CustomSchemaIds(type => type.ToString());
    swagger.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = versionName,
        Title = $"FinancialSystem.Web API - {envName}"
    });

    // Configuração do JWT no Swagger
    swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT desta forma: Bearer {seu token}"
    });

    swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;

}).ConfigureApiBehaviorOptions(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var key = Encoding.ASCII.GetBytes(builder.Configuration["JwtSettings:Secret"]);
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        //ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        //ValidAudience = builder.Configuration["JwtSettings:Audience"],
        ClockSkew = TimeSpan.Zero // sem tolerância extra pro token expirar
    };
});

builder.Services.AddScoped(typeof(IGeneralRepository<>), typeof(GeneralRepository<>));
builder.Services.AddScoped<IUserSettingsAppService, UserSettingsAppService>();
builder.Services.AddScoped<IEnvironmentSettingsAppService, EnvironmentSettingsAppService>();
builder.Services.AddScoped<ITransactionAppService, TransactionAppService>();
builder.Services.AddScoped<IGoalsSettingsAppService, GoalsSettingsAppService>();
builder.Services.AddScoped<IRankingAppService, RankingAppService>();
builder.Services.AddScoped<IDashboardsAppService, DashboardsAppService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IAppSession, AppSession>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
});

builder.Services.AddOptions();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FinancialSystem.Web - Api"));
    app.UseHsts();
}
app.UseSession();

app.UseRouting();

app.UseCors(myAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();