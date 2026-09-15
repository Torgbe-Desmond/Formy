using dotenv.net;
using Extensions;
using Formify.Api.Data;
using Formify.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using Serilog;

// DotEnv.Load(options: new DotEnvOptions(ignoreExceptions: true));
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.RegisterServices();

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.RegisterSwaggerUI();

builder.Services.RegisterAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.RegisterCors(builder.Configuration);
var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
