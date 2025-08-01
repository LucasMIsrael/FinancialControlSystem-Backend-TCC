using FinancialSystem.Application.Services.UserSettings;
using FinancialSystem.Application.Shared.Interfaces.UserSettings;
using FinancialSystem.Core.Settings;
using FinancialSystem.EntityFrameworkCore.Context;
using FinancialSystem.EntityFrameworkCore.Repositories;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
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

// Swagger
builder.Services.AddSwaggerGen(swagger =>
{
    swagger.CustomSchemaIds(type => type.ToString());
    swagger.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = versionName,
        Title = $"FinancialSystem.Web API - {envName}"
    });
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;

}).ConfigureApiBehaviorOptions(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});
//}).AddNewtonsoftJson(options =>
//{
//    options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Include;
//    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
//});

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
        ValidateAudience = false
    };
});


builder.Services.AddScoped(typeof(IGeneralRepository<>), typeof(GeneralRepository<>));
builder.Services.AddScoped<IUserSettingsAppService, UserSettingsAppService>();


builder.Services.AddOptions();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FinancialSystem.Web - Api"));
    app.UseHsts();
}

app.UseRouting();

app.UseCors(myAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints => endpoints.MapControllers());

app.Run();