using System.Threading.Channels;
using Microsoft.Playwright;

namespace Business.Services.Utilities.PDF;

internal sealed class BrowserContextPool
{
    private readonly Channel<IBrowserContext> _pool;
    private readonly IBrowser _browser;

    public BrowserContextPool(IBrowser browser, int size)
    {
        _browser = browser;
        _pool = Channel.CreateBounded<IBrowserContext>(size);

        for (int i = 0; i < size; i++)
            _pool.Writer.TryWrite(CreateContextAsync().GetAwaiter().GetResult());
    }

    private async Task<IBrowserContext> CreateContextAsync()
    {
        var ctx = await _browser.NewContextAsync(new()
        {
            ViewportSize = null,
            JavaScriptEnabled = false,
            BypassCSP = true
        });

        // route SOLO UNA VEZ
        await ctx.RouteAsync("**/*", route =>
        {
            var url = route.Request.Url;
            if (url.StartsWith("http://") || url.StartsWith("https://"))
                return route.AbortAsync();
            return route.ContinueAsync();
        });

        return ctx;
    }

    public Task<IBrowserContext> RentAsync() => _pool.Reader.ReadAsync().AsTask();

    public async Task ReturnAsync(IBrowserContext ctx)
    {
        var pages = ctx.Pages.ToArray();

        foreach (var p in pages)
            await p.CloseAsync();

        await _pool.Writer.WriteAsync(ctx);
    }
}
