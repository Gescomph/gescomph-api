using WebGESCOMPH.Extensions.Composition;
using WebGESCOMPH.Extensions.Infrastructure;
using WebGESCOMPH.Extensions.Payments;
using WebGESCOMPH.Extensions.Presentation;
using WebGESCOMPH.Extensions.RealTime;
using Business.Interfaces.Implements.Business;
using WebGESCOMPH.Services;

var builder = WebApplication.CreateBuilder(args);

// --------------------------
// CONFIGURACIÓN Y SERVICIOS
// --------------------------

builder.Services.AddPresentationControllers();
builder.Services.AddCustomSwagger();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMercadoPagoServices(builder.Configuration);
builder.Services.AddScoped<IObligationNotifier, SignalRObligationNotifier>();

var app = builder.Build();

// --------------------------
// MIDDLEWARE GLOBAL (ORDEN)
// --------------------------

app.UseForwardedHeaders();

app.UseMiddleware<ExceptionMiddleware>();
app.UseStaticFiles();
app.UseCustomSwagger();
app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
// MigrationManager.MigrateAllDatabases(app.Services, builder.Configuration);
app.MapAppSignalRHubs();
await app.UsePdfWarmupAsync();
app.UseHangfireDashboardAndJobs(builder.Configuration);

app.Run();
