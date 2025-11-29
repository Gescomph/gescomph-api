using System.Net.Http.Headers;
using Business.Services.Business.Payments;
using Entity.Infrastructure.Configurations.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace WebGESCOMPH.Extensions.Payments;

public static class MercadoPagoServiceCollectionExtensions
{
    public static IServiceCollection AddMercadoPagoServices(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("MercadoPago");
        services.Configure<MercadoPagoSettings>(section);
        services.AddHttpClient<IMercadoPagoService, MercadoPagoService>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<MercadoPagoSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!string.IsNullOrWhiteSpace(settings.AccessToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.AccessToken);
            }
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
