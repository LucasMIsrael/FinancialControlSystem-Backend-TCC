using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
       .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
       .AddEnvironmentVariables();

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

//builder.Services.AddScoped<IQACodaiPaymentAppService, QACodaiPaymentAppService>();
//builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

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