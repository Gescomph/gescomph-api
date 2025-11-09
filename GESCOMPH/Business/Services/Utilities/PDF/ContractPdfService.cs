using Business.Interfaces.PDF;
using DotLiquid;
using Entity.DTOs.Implements.Business.Contract;
using Humanizer;
using Microsoft.Playwright;
using System.Globalization;
using Templates.Templates;

namespace Business.Services.Utilities.PDF
{
    public class ContractPdfService : IContractPdfGeneratorService
    {
        private static BrowserContextPool? _pool;
        private static readonly SemaphoreSlim _initLock = new(1, 1);
        private static IPlaywright? _playwright;
        private static IBrowser? _browser;

        private static readonly Lazy<string?> _logoBase64 = new(TryLoadLogoBase64);

        private static readonly Template _parsedTemplate;

        // se ejecuta UNA sola vez cuando la clase se carga en memoria
        static ContractPdfService()
        {
            Template.NamingConvention = new DotLiquid.NamingConventions.CSharpNamingConvention();
            _parsedTemplate = Template.Parse(ContractTemplate.Html);
        }

        public async Task<byte[]> GeneratePdfAsync(ContractSelectDto contract)
        {
            var html = BuildHtml(contract);

            await EnsureBrowserAsync();

            var context = await _pool!.RentAsync();

            try
            {
                var page = await context.NewPageAsync();
                await page.EmulateMediaAsync(new() { Media = Media.Print });

                await page.SetContentAsync(html, new()
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 2000
                });

                var headerHtml = $@"
<div style='width:100%;font-size:9.5pt;padding-top:6px;padding-bottom:4px;padding-left:25mm;padding-right:25mm;opacity:0.63;'>
  <table style='width:100%;border-collapse:collapse;'>
    <tr>
      <td style='width:70%;vertical-align:top;padding-right:12px;'>
        <img src=""data:image/jpeg;base64,{_logoBase64.Value}"" style='width:100%;height:auto;' />
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
                await _pool.ReturnAsync(context);
            }
        }

        public Task WarmupAsync() => EnsureBrowserAsync();

        private static async Task EnsureBrowserAsync()
        {
            if (_pool is not null) return;

            await _initLock.WaitAsync();
            try
            {
                if (_pool is not null) return;

                _playwright ??= await Playwright.CreateAsync();

                _browser = await _playwright.Chromium.LaunchAsync(new()
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

                var size = Environment.ProcessorCount * 2;
                _pool = new BrowserContextPool(_browser!, size);
            }
            finally
            {
                _initLock.Release();
            }
        }

        private static string BuildHtml(ContractSelectDto c)
        {
            var esCO = new CultureInfo("es-CO");
            var months = ((c.EndDate.Year - c.StartDate.Year) * 12) + (c.EndDate.Month - c.StartDate.Month);

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
                DurationMonthsWords = ((long)months).ToWords(new CultureInfo("es")).ToUpperInvariant(),
                MonthlyRentAmountWords = ((long)c.TotalBaseRentAgreed).ToWords(new CultureInfo("es")).ToUpperInvariant(),
                MonthlyRentAmount = c.TotalBaseRentAgreed.ToString("N0", esCO),
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
