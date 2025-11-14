using System.Threading.Channels;
using Microsoft.Playwright;

namespace Business.Services.Utilities.PDF
{
    /// <summary>
    /// Maneja un conjunto de contextos de navegador ya preparados para generar PDF.
    /// En lugar de crear un contexto nuevo en cada solicitud, los contextos se crean
    /// por adelantado y se reutilizan, lo que mejora la velocidad y reduce la carga
    /// del sistema.
    /// </summary>
    internal sealed class BrowserContextPool : IAsyncDisposable
    {
        private readonly Channel<IBrowserContext> _pool;
        private readonly IBrowser _browser;
        private bool _disposed;

        /// <summary>
        /// Crea un pool usando un navegador ya inicializado. El constructor es privado
        /// porque la creación completa del pool requiere pasos asíncronos.
        /// </summary>
        private BrowserContextPool(IBrowser browser, Channel<IBrowserContext> pool)
        {
            _browser = browser;
            _pool = pool;
        }

        /// <summary>
        /// Crea un pool de contextos listos para usarse. Cada contexto se prepara desde cero
        /// (con su configuración y sus reglas de navegación) antes de agregarse al pool.
        /// </summary>
        /// <param name="browser">Instancia del navegador Playwright.</param>
        /// <param name="size">Cantidad de contextos que estarán disponibles.</param>
        /// <returns>Un pool completamente preparado y listo para procesar solicitudes.</returns>
        public static async Task<BrowserContextPool> CreateAsync(IBrowser browser, int size)
        {
            var channel = Channel.CreateBounded<IBrowserContext>(size);
            var pool = new BrowserContextPool(browser, channel);

            for (int i = 0; i < size; i++)
            {
                var ctx = await pool.CreateContextAsync();
                await channel.Writer.WriteAsync(ctx);
            }

            return pool;
        }

        /// <summary>
        /// Crea un contexto nuevo del navegador con una configuración limpia.
        /// También se aplica una regla que bloquea automáticamente cualquier intento
        /// de cargar contenido externo por internet, para garantizar seguridad y velocidad.
        /// </summary>
        private async Task<IBrowserContext> CreateContextAsync()
        {
            var ctx = await _browser.NewContextAsync(new()
            {
                ViewportSize = null,
                JavaScriptEnabled = false,
                BypassCSP = true
            });

            // Evita que el contexto cargue recursos externos
            await ctx.RouteAsync("**/*", route =>
            {
                var url = route.Request.Url;

                if (url.StartsWith("http://") || url.StartsWith("https://"))
                    return route.AbortAsync();

                return route.ContinueAsync();
            });

            return ctx;
        }

        /// <summary>
        /// Permite tomar un contexto disponible del pool.  
        /// Si en ese momento no queda ninguno libre, la llamada espera hasta que otro
        /// proceso lo devuelva.
        /// </summary>
        public Task<IBrowserContext> RentAsync()
            => _pool.Reader.ReadAsync().AsTask();

        /// <summary>
        /// Devuelve un contexto al pool después de usarlo.  
        /// Antes de regresarlo, se cierran todas las páginas abiertas dentro de ese contexto
        /// para que quede limpio y listo para la siguiente operación.
        /// </summary>
        public async Task ReturnAsync(IBrowserContext ctx)
        {
            var pages = ctx.Pages.ToArray();

            foreach (var p in pages)
                await p.CloseAsync();

            await _pool.Writer.WriteAsync(ctx);
        }

        /// <summary>
        /// Se usa cuando la aplicación se está apagando.  
        /// Cierra todos los contextos que aún estén dentro del pool y libera recursos.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            // Evita que se agreguen más contextos
            _pool.Writer.TryComplete();

            // Va leyendo y cerrando cada contexto que queda en el pool
            while (await _pool.Reader.WaitToReadAsync())
            {
                while (_pool.Reader.TryRead(out var ctx))
                {
                    await ctx.CloseAsync();
                }
            }
        }
    }
}
