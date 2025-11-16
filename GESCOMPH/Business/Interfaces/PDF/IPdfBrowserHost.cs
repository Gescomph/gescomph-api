using Business.Services.Utilities.PDF;

namespace Business.Interfaces.PDF;

    public interface IPdfBrowserHost : IAsyncDisposable
    {
        Task<BrowserContextPool> GetPoolAsync();
    }
