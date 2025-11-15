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
    /// Se encarga de preparar el HTML, solicitar un contexto del navegador 
    /// desde el host compartido, renderizar el contenido y producir el archivo final.
    /// 
    /// Este servicio ya no administra Playwright ni el pool directamente.
    /// En su lugar, utiliza un host singleton que mantiene los recursos pesados
    /// (Playwright + Browser + BrowserContextPool) inicializados una sola vez 
    /// para toda la aplicación, garantizando alto rendimiento bajo carga.
    /// </summary>
    public class ContractPdfService : IContractPdfGeneratorService
    {
        // Host que administra el navegador, el pool y toda la inicialización pesada.
        private readonly IPdfBrowserHost _browserHost;

        // Valores estáticos que no dependen del navegador
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

        public ContractPdfService(IPdfBrowserHost browserHost)
        {
            _browserHost = browserHost;
        }

        /// <summary>
        /// Genera el PDF completo del contrato:
        /// 1. Arma el HTML del documento.
        /// 2. Obtiene el pool compartido del host.
        /// 3. Solicita un contexto disponible del pool.
        /// 4. Abre una página, carga el contenido y produce el PDF final.
        /// </summary>
        public async Task<byte[]> GeneratePdfAsync(ContractSelectDto contract)
        {
            var html = BuildHtml(contract);

            // Obtiene el pool compartido mantenido por el host
            var pool = await _browserHost.GetPoolAsync();

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
        /// La inicialización es manejada por el host.
        /// </summary>
        public Task WarmupAsync() => _browserHost.GetPoolAsync();

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

            var model = new
            {
                FullName = c.FullName,
                Document = c.Document,
                Phone = c.Phone,
                Email = c.Email,
                ContractNumber = c.Id,
                ContractYear = c.StartDate.Year,
                StartDate = c.StartDate.ToString("dd/MM/yyyy"),
                EndDate = c.EndDate.ToString("dd/MM/yyyy"),
                DurationMonths = months,
                DurationMonthsWords = durationMonthsWords,
                MonthlyRentAmountWords = monthlyRentAmountWords,
                MonthlyRentAmount = monthlyRentAmount,
                UVTValue = c.TotalUvtQtyAgreed,
                Address = c.Address,
                PremisesLeased = c.PremisesLeased?.Select(p => new
                {
                    p.EstablishmentName,
                    p.Address,
                    p.AreaM2,
                    p.PlazaName
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
    }
}
