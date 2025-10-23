using Business.Interfaces;
using Entity.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace Business.Repository
{
    /// <summary>
    /// Implementacion del patron Unit of Work (UoW) para coordinar operaciones transaccionales
    /// sobre el contexto de base de datos <see cref="ApplicationDbContext"/>.
    ///
    /// Esta clase:
    /// - Encapsula la gestion de transacciones.
    /// - Aplica estrategias de reintento configuradas en EF Core.
    /// - Permite registrar acciones post-commit (<see cref="RegisterPostCommit"/>) que se ejecutan
    ///   solo si la transaccion se completa exitosamente.
    ///
    /// Es sealed para evitar herencia y garantizar la consistencia del ciclo de vida transaccional.
    /// </summary>
    public sealed class UnitOfWork : IUnitOfWork
    {
        /// <summary>
        /// Contexto de base de datos de EF Core.
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Logger opcional para registrar errores o eventos del flujo transaccional.
        /// </summary>
        private readonly ILogger<UnitOfWork>? _logger;

        /// <summary>
        /// Lista de acciones que deben ejecutarse despues de un commit exitoso.
        /// </summary>
        private readonly List<Func<CancellationToken, Task>> _postCommitActions = new();

        /// <summary>
        /// Profundidad actual del UnitOfWork para soportar ejecuciones anidadas.
        /// </summary>
        private readonly AsyncLocal<int> _scopeDepth = new();

        private bool IsInScope => _scopeDepth.Value > 0;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="UnitOfWork"/>.
        /// </summary>
        /// <param name="context">Instancia del contexto de datos (<see cref="ApplicationDbContext"/>).</param>
        /// <param name="logger">Instancia opcional de logger para registrar errores y diagnosticos.</param>
        public UnitOfWork(ApplicationDbContext context, ILogger<UnitOfWork>? logger = null)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Ejecuta una accion dentro de una transaccion con estrategia de reintento (retry) de EF Core.
        /// </summary>
        /// <param name="action">Funcion asincronica que representa la operacion a ejecutar dentro de la transaccion.</param>
        /// <param name="ct">Token de cancelacion opcional.</param>
        public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
        {
            if (IsInScope)
            {
                _scopeDepth.Value++;
                try
                {
                    await action(ct);
                }
                finally
                {
                    _scopeDepth.Value--;
                }
                return;
            }

            _scopeDepth.Value = 1;
            var strategy = _context.Database.CreateExecutionStrategy();
            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _context.Database.BeginTransactionAsync(ct);
                    try
                    {
                        await action(ct);
                        await _context.SaveChangesAsync(ct);
                        await tx.CommitAsync(ct);
                        await RunPostCommitAsync(ct);
                    }
                    catch
                    {
                        _postCommitActions.Clear();
                        await tx.RollbackAsync(ct);
                        throw;
                    }
                });
            }
            finally
            {
                _scopeDepth.Value = 0;
            }
        }

        /// <summary>
        /// Ejecuta una accion que retorna un valor dentro de una transaccion con soporte de reintento.
        /// </summary>
        /// <typeparam name="T">Tipo del valor de retorno.</typeparam>
        /// <param name="action">Funcion asincronica que representa la operacion transaccional a ejecutar.</param>
        /// <param name="ct">Token de cancelacion opcional.</param>
        /// <returns>Resultado de la operacion ejecutada dentro de la transaccion.</returns>
        public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default)
        {
            if (IsInScope)
            {
                _scopeDepth.Value++;
                try
                {
                    return await action(ct);
                }
                finally
                {
                    _scopeDepth.Value--;
                }
            }

            _scopeDepth.Value = 1;
            var strategy = _context.Database.CreateExecutionStrategy();
            try
            {
                return await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _context.Database.BeginTransactionAsync(ct);
                    try
                    {
                        var result = await action(ct);
                        await _context.SaveChangesAsync(ct);
                        await tx.CommitAsync(ct);
                        await RunPostCommitAsync(ct);
                        return result;
                    }
                    catch
                    {
                        _postCommitActions.Clear();
                        await tx.RollbackAsync(ct);
                        throw;
                    }
                });
            }
            finally
            {
                _scopeDepth.Value = 0;
            }
        }

        /// <summary>
        /// Registra una accion que se ejecutara unicamente despues de un commit exitoso.
        /// </summary>
        /// <param name="action">Funcion asincronica a ejecutar despues del commit.</param>
        public void RegisterPostCommit(Func<CancellationToken, Task> action)
        {
            if (action is null) return;
            _postCommitActions.Add(action);
        }

        /// <summary>
        /// Ejecuta todas las acciones registradas post-commit de forma secuencial.
        /// </summary>
        /// <param name="ct">Token de cancelacion opcional.</param>
        private async Task RunPostCommitAsync(CancellationToken ct)
        {
            if (_postCommitActions.Count == 0) return;

            var actions = _postCommitActions.ToArray();
            _postCommitActions.Clear();

            foreach (var act in actions)
            {
                try
                {
                    await act(ct);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Post-commit action failed");
                }
            }
        }
    }
}
