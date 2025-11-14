using Business.Interfaces.PDF;
using DotLiquid;
using Entity.DTOs.Implements.Business.Contract;
using Humanizer;
using Microsoft.Playwright;
using System.Globalization;
using Templates.Templates;

namespace Business.Services.Utilities.PDF
{
    /// <summary>
    /// Servicio encargado de generar el PDF de un contrato.
    /// Se encarga de preparar el HTML, solicitar un contexto del navegador,
    /// renderizar el contenido y producir el archivo final.
    /// 
    /// También administra la inicialización del navegador y el pool de contextos,
    /// asegurando que todo se cargue una sola vez y que el sistema responda rápido
    /// incluso bajo carga.
    /// </summary>
    public class ContractPdfService : IContractPdfGeneratorService, IAsyncDisposable
    {
        // Controla que la inicialización solo ocurra una vez
        private readonly SemaphoreSlim _initLock = new(1, 1);

        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private BrowserContextPool? _pool;

        // Estos valores no dependen del navegador, así que pueden ser estáticos
        private static readonly string? _logoBase64 = TryLoadLogoBase64();
        private static readonly CultureInfo _esCO = new("es-CO");
        private static readonly CultureInfo _esES = new("es");
        private static readonly Template _parsedTemplate;

        /// <summary>
        /// Configura el motor de plantillas una sola vez al cargar la clase.
        /// Este proceso es liviano y ayuda a mejorar la velocidad en tiempo de ejecución.
        /// </summary>
        static ContractPdfService()
        {
            Template.NamingConvention = new DotLiquid.NamingConventions.CSharpNamingConvention();
            _parsedTemplate = Template.Parse(ContractTemplate.Html);
        }

        /// <summary>
        /// Genera el PDF completo del contrato:
        /// 1. Arma el HTML del documento.
        /// 2. Asegura que el navegador esté inicializado.
        /// 3. Solicita un contexto disponible del pool.
        /// 4. Abre una página, carga el contenido y produce el PDF final.
        /// </summary>
        /// <param name="contract">Información del contrato que se mostrará en el PDF.</param>
        /// <returns>El archivo PDF generado en forma de arreglo de bytes.</returns>
        public async Task<byte[]> GeneratePdfAsync(ContractSelectDto contract)
        {
            var html = BuildHtml(contract);

            // Asegura que el pool está listo
            var pool = await EnsurePoolAsync();

            // Toma un contexto disponible
            var context = await pool.RentAsync();

            try
            {
                var page = await context.NewPageAsync();
                await page.EmulateMediaAsync(new() { Media = Media.Print });

                // Carga el HTML generado del contrato
                await page.SetContentAsync(html, new()
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 5000
                });

                // Encabezado personalizado del PDF
                var headerHtml = $@"
<div style='width:100%;font-size:9.5pt;padding-top:6px;padding-bottom:4px;padding-left:25mm;padding-right:25mm;opacity:0.63;'>
  <table style='width:100%;border-collapse:collapse;'>
    <tr>
      <td style='width:70%;vertical-align:top;padding-right:12px;'>
        <img src=""data:image/jpeg;base64,{_logoBase64}"" style='width:100%;height:auto;' />
      </td>
      <td style='font-size:9.5pt;text-align:right;vertical-align:top;white-space:nowrap;'>
        <b>COMUNICACIÓN<br/>OFICIAL DESPACHADA</b><br/>
        Código: FOR-GP-05<br/>
        Versión: 05<br/>
        Fecha: {contract.StartDate:dd/MM/yyyy}<br/>
        Página <span class=""pageNumber""></span> de <span class=""totalPages""></span>
      </td>
    </tr>
  </table>
</div>";

                // Genera el PDF final usando Playwright
                var pdfBytes = await page.PdfAsync(new()
                {
                    Format = "Letter",
                    PrintBackground = true,
                    PreferCSSPageSize = true,
                    DisplayHeaderFooter = true,
                    HeaderTemplate = headerHtml,
                    FooterTemplate = "<div style=\"font-size:10px;width:100%;text-align:center;color:#555;\">Página <span class=\"pageNumber\"></span> de <span class=\"totalPages\"></span></div>",
                    Margin = new()
                    {
                        Top = "58mm",
                        Bottom = "25mm",
                        Left = "28mm",
                        Right = "28mm"
                    }
                });

                return pdfBytes;
            }
            finally
            {
                // Devuelve el contexto para que otro proceso pueda usarlo
                await pool.ReturnAsync(context);
            }
        }

        /// <summary>
        /// Permite que la aplicación inicialice el navegador y el pool anticipadamente,
        /// evitando que la primera solicitud tenga una demora mayor.
        /// </summary>
        public Task WarmupAsync() => EnsurePoolAsync();

        /// <summary>
        /// Inicializa Playwright, el navegador y el pool de contextos.
        /// Este proceso solo se ejecuta una vez, sin importar cuántas veces se llame.
        /// </summary>
        /// <returns>El pool ya inicializado y listo para usarse.</returns>
        private async Task<BrowserContextPool> EnsurePoolAsync()
        {
            if (_pool is not null) return _pool;

            await _initLock.WaitAsync();
            try
            {
                if (_pool is not null) return _pool;

                // Inicia Playwright
                _playwright ??= await Playwright.CreateAsync();

                // Abre el navegador
                _browser ??= await _playwright.Chromium.LaunchAsync(new()
                {
                    Headless = true,
                    Args = new[]
                    {
                        "--no-sandbox",
                        "--disable-dev-shm-usage",
                        "--disable-gpu",
                        "--disable-extensions"
                    }
                });

                // Tamaño del pool: depende del rendimiento del servidor
                // Usar al menos 2 y máximo cores * 2 para balance óptimo
                var size = Math.Max(2, Math.Min(Environment.ProcessorCount * 2, 16));

                // Crea el pool completo de contextos
                _pool = await BrowserContextPool.CreateAsync(_browser!, size);

                return _pool;
            }
            finally
            {
                _initLock.Release();
            }
        }

        /// <summary>
        /// Construye el HTML final del contrato mezclando la plantilla con los datos recibidos.
        /// Este HTML es el que luego se renderiza como PDF.
        /// Optimizado: evita crear nuevas CultureInfo, reduce allocations innecesarias.
        /// </summary>
        private static string BuildHtml(ContractSelectDto c)
        {
            var months = ((c.EndDate.Year - c.StartDate.Year) * 12) + (c.EndDate.Month - c.StartDate.Month);

            // Pre-calcular valores antes de crear el modelo anónimo
            var durationMonthsWords = ((long)months).ToWords(_esES).ToUpperInvariant();
            var monthlyRentAmountWords = ((long)c.TotalBaseRentAgreed).ToWords(_esES).ToUpperInvariant();
            var monthlyRentAmount = c.TotalBaseRentAgreed.ToString("N0", _esCO);
            var startDateStr = c.StartDate.ToString("dd/MM/yyyy");
            var endDateStr = c.EndDate.ToString("dd/MM/yyyy");

            var model = new
            {
                FullName = c.FullName,
                Document = c.Document,
                Phone = c.Phone,
                Email = c.Email,
                ContractNumber = c.Id,
                ContractYear = c.StartDate.Year,
                StartDate = startDateStr,
                EndDate = endDateStr,
                DurationMonths = months,
                DurationMonthsWords = durationMonthsWords,
                MonthlyRentAmountWords = monthlyRentAmountWords,
                MonthlyRentAmount = monthlyRentAmount,
                UVTValue = c.TotalUvtQtyAgreed,
                Address = c.Address,
                PremisesLeased = c.PremisesLeased?.Select(p => new
                {
                    EstablishmentName = p.EstablishmentName,
                    Address = p.Address,
                    AreaM2 = p.AreaM2,
                    PlazaName = p.PlazaName
                }).ToList(),
                Clauses = c.Clauses?.Select(x => new { Description = x.Description }).ToList()
            };

            return _parsedTemplate.Render(Hash.FromAnonymousObject(model));
        }

        /// <summary>
        /// Carga el logo desde los recursos embebidos y lo convierte a Base64 para ser usado
        /// dentro del PDF sin necesidad de archivos externos.
        /// </summary>
        private static string? TryLoadLogoBase64()
        {
            var asm = typeof(ContractTemplate).Assembly;
            var name = asm.GetManifestResourceNames()
                          .FirstOrDefault(x => x.EndsWith("banner.jpg", StringComparison.OrdinalIgnoreCase));
            if (name == null) return null;

            using var stream = asm.GetManifestResourceStream(name);
            using var ms = new MemoryStream();
            stream!.CopyTo(ms);
            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Libera todos los recursos usados por el servicio al apagar la aplicación,
        /// asegurando que el navegador y los contextos queden cerrados correctamente.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_pool is not null)
                await _pool.DisposeAsync();

            if (_browser is not null)
                await _browser.CloseAsync();

            _playwright?.Dispose();
        }
    }
}
