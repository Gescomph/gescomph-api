using Business.Interfaces.PDF;
using Microsoft.Playwright;

namespace Business.Services.Utilities.PDF
{
    public sealed class PdfBrowserHost : IPdfBrowserHost
    {
        private readonly SemaphoreSlim _initLock = new(1, 1);

        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private BrowserContextPool? _pool;

        public async Task<BrowserContextPool> GetPoolAsync()
        {
            if (_pool is not null)
                return _pool;

            await _initLock.WaitAsync();
            try
            {
                if (_pool is not null)
                    return _pool;

                _playwright ??= await Playwright.CreateAsync();

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

                var size = Math.Max(2, Math.Min(Environment.ProcessorCount * 2, 16));

                _pool = await BrowserContextPool.CreateAsync(_browser, size);

                return _pool;
            }
            finally
            {
                _initLock.Release();
            }
        }

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
