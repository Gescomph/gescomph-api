using Business.Interfaces.Implements.Business;
using Business.Repository;
using Data.Interfaz.DataBasic;
using Data.Interfaz.IDataImplement.Business;
using Entity.Domain.Models.Implements.AdministrationSystem;
using Entity.Domain.Models.Implements.Business;
using Entity.DTOs.Implements.Business.ObligationMonth;
using Entity.Enum;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using Utilities.Exceptions;

namespace Business.Services.Business
{
    public class ObligationMonthService
        : BusinessGeneric<ObligationMonthSelectDto, ObligationMonthDto, ObligationMonthUpdateDto, ObligationMonth>,
          IObligationMonthService
    {
        private readonly IObligationMonthRepository _obligationRepository;
        private readonly IContractRepository _contractRepository;
        private readonly IDataGeneric<SystemParameter> _systemParamRepository;
        private readonly IObligationNotifier _notifier;

        public ObligationMonthService(
            IObligationMonthRepository obligationRepository,
            IContractRepository contractRepository,
            IDataGeneric<SystemParameter> systemParamRepository,
            IObligationNotifier notifier,
            IMapper mapper) : base(obligationRepository, mapper)
        {
            _obligationRepository = obligationRepository;
            _contractRepository = contractRepository;
            _systemParamRepository = systemParamRepository;
            _notifier = notifier;
        }

        public async Task GenerateMonthlyAsync(int year, int month)
        {
            var (monthStart, monthEnd, dueDate) = GetPeriodDates(year, month);
            var uvtValue = await GetParameterValueAsync("UVT", dueDate);
            var vatRate = await GetParameterValueAsync("IVA", dueDate);

            var contracts = await _contractRepository.GetAllQueryable()
                .Where(c => c.Active && c.StartDate < monthEnd && c.EndDate >= monthStart)
                .ToListAsync();

            foreach (var contract in contracts)
                await UpsertObligationAsync(contract, monthStart, uvtValue, vatRate);
        }

        public async Task GenerateForContractMonthAsync(int contractId, int year, int month)
        {
            var contract = await _contractRepository.GetByIdAsync(contractId)
                ?? throw new BusinessException("Contrato no existe.");

            var (monthStart, monthEnd, dueDate) = GetPeriodDates(year, month);

            if (!(contract.StartDate < monthEnd && contract.EndDate >= monthStart))
                return;

            var uvtValue = await GetParameterValueAsync("UVT", dueDate);
            var vatRate = await GetParameterValueAsync("IVA", dueDate);

            await UpsertObligationAsync(contract, monthStart, uvtValue, vatRate);
        }

        public async Task<IEnumerable<object>> GetLastSixMonthsPaidAsync()
        {
            return await _obligationRepository.GetLastSixMonthsPaidAsync();
        }

        public async Task<IReadOnlyList<ObligationMonthSelectDto>> GetByContractAsync(int contractId)
        {
            if (contractId <= 0)
                throw new BusinessException("contractId invalido.");

            var list = await _obligationRepository.GetByContractQueryable(contractId)
                .AsNoTracking()
                .ToListAsync();

            return _mapper.Map<List<ObligationMonthSelectDto>>(list).AsReadOnly();
        }

        public async Task MarkAsPaidAsync(int id)
        {
            var existing = await _obligationRepository.GetByIdAsync(id)
                ?? throw new BusinessException($"No existe obligación mensual con Id {id}.");

            // Validación de idempotencia: si ya está pagada, no hacer nada
            if (existing.Status == Status.Aprobada && existing.Locked)
            {
                // Ya está pagada, comportamiento idempotente (evita duplicados)
                return;
            }

            existing.PaymentDate = DateTime.UtcNow;
            existing.Status = Status.Aprobada;
            existing.Locked = true;

            await _obligationRepository.UpdateAsync(existing);

            // Notificar actualización en tiempo real
            await _notifier.NotifyObligationsUpdatedAsync();
        }

        private async Task UpsertObligationAsync(Contract contract, DateTime periodDate, decimal uvtValue, decimal vatRate)
        {
            var existing = await GetExistingObligationAsync(contract.Id, periodDate);
            
            if (existing != null && existing.Locked)
                return;

            var (effectiveDays, totalDaysInMonth) = CalculateEffectiveDays(contract, periodDate);
            var proportionalAmount = CalculateProportionalAmount(contract, periodDate, uvtValue, effectiveDays, totalDaysInMonth);
            
            // Manejar acumulación del primer mes parcial
            if (await ShouldAccumulateFirstMonthAsync(contract, periodDate, effectiveDays, totalDaysInMonth, proportionalAmount))
                return;
            
            // Calcular monto base con acumulado si aplica
            var baseAmount = await CalculateBaseAmountWithAccumulatedAsync(contract, periodDate, proportionalAmount);
            
            // Calcular totales
            var (vatAmount, totalAmount) = CalculateTaxAndTotal(baseAmount, vatRate);
            
            // Calcular fecha límite
            var dueDate = await CalculateDueDateAsync(periodDate);
            
            // Crear o actualizar obligación
            await SaveOrUpdateObligationAsync(existing, contract, periodDate, uvtValue, vatRate, baseAmount, vatAmount, totalAmount, dueDate);
        }

        /// <summary>
        /// Obtiene obligación existente para el contrato y período.
        /// </summary>
        private async Task<ObligationMonth?> GetExistingObligationAsync(int contractId, DateTime periodDate)
        {
            return await _obligationRepository
                .GetByContractYearMonthAsync(contractId, periodDate.Year, periodDate.Month);
        }

        /// <summary>
        /// Calcula el monto proporcional basado en días efectivos.
        /// </summary>
        private decimal CalculateProportionalAmount(Contract contract, DateTime periodDate, decimal uvtValue, int effectiveDays, int totalDaysInMonth)
        {
            decimal monthlyBase = contract.TotalBaseRentAgreed > 0m
                ? contract.TotalBaseRentAgreed
                : contract.TotalUvtQtyAgreed * uvtValue;
            
            return (monthlyBase / totalDaysInMonth) * effectiveDays;
        }

        /// <summary>
        /// Determina si se debe acumular el primer mes parcial y lo guarda si es necesario.
        /// </summary>
        private async Task<bool> ShouldAccumulateFirstMonthAsync(
            Contract contract, 
            DateTime periodDate, 
            int effectiveDays, 
            int totalDaysInMonth, 
            decimal proportionalAmount)
        {
            bool isFirstMonth = IsFirstMonthOfContract(contract, periodDate);
            bool isPartialMonth = effectiveDays < totalDaysInMonth;
            
            if (isFirstMonth && isPartialMonth)
            {
                contract.AccumulatedFirstMonth = proportionalAmount;
                await _contractRepository.UpdateAsync(contract);
                return true; // Indicar que se acumuló y NO se debe generar obligación
            }
            
            return false;
        }

        /// <summary>
        /// Calcula monto base incluyendo acumulado del primer mes si aplica.
        /// </summary>
        private async Task<decimal> CalculateBaseAmountWithAccumulatedAsync(
            Contract contract, 
            DateTime periodDate, 
            decimal proportionalAmount)
        {
            decimal baseAmount = proportionalAmount;
            
            if (IsSecondMonthOfContract(contract, periodDate) && contract.AccumulatedFirstMonth.HasValue)
            {
                baseAmount += contract.AccumulatedFirstMonth.Value;
                
                // Limpiar acumulado
                contract.AccumulatedFirstMonth = null;
                await _contractRepository.UpdateAsync(contract);
            }
            
            return baseAmount;
        }

        /// <summary>
        /// Calcula IVA y monto total.
        /// </summary>
        private (decimal VatAmount, decimal TotalAmount) CalculateTaxAndTotal(decimal baseAmount, decimal vatRate)
        {
            decimal vatAmount = baseAmount * vatRate;
            decimal totalAmount = baseAmount + vatAmount;
            return (vatAmount, totalAmount);
        }

        /// <summary>
        /// Calcula fecha límite de pago basada en parámetro configurable.
        /// </summary>
        private async Task<DateTime> CalculateDueDateAsync(DateTime periodDate)
        {
            var paymentDueDay = await GetParameterIntAsync("DIA_LIMITE_PAGO", 5);
            var nextMonth = periodDate.AddMonths(1);
            var maxDay = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
            var dueDay = Math.Min(paymentDueDay, maxDay);
            
            return new DateTime(nextMonth.Year, nextMonth.Month, dueDay);
        }

        /// <summary>
        /// Guarda nueva obligación o actualiza existente.
        /// </summary>
        private async Task SaveOrUpdateObligationAsync(
            ObligationMonth? existing,
            Contract contract,
            DateTime periodDate,
            decimal uvtValue,
            decimal vatRate,
            decimal baseAmount,
            decimal vatAmount,
            decimal totalAmount,
            DateTime dueDate)
        {
            if (existing == null)
            {
                await CreateNewObligationAsync(contract, periodDate, uvtValue, vatRate, baseAmount, vatAmount, totalAmount, dueDate);
            }
            else
            {
                await UpdateExistingObligationAsync(existing, contract, uvtValue, vatRate, baseAmount, vatAmount, totalAmount, dueDate);
            }
        }

        /// <summary>
        /// Crea nueva obligación.
        /// </summary>
        private async Task CreateNewObligationAsync(
            Contract contract,
            DateTime periodDate,
            decimal uvtValue,
            decimal vatRate,
            decimal baseAmount,
            decimal vatAmount,
            decimal totalAmount,
            DateTime dueDate)
        {
            var obligation = new ObligationMonth
            {
                ContractId = contract.Id,
                Year = periodDate.Year,
                Month = periodDate.Month,
                DueDate = dueDate,
                UvtQtyApplied = contract.TotalUvtQtyAgreed,
                UvtValueApplied = uvtValue,
                VatRateApplied = vatRate,
                BaseAmount = baseAmount,
                VatAmount = vatAmount,
                TotalAmount = totalAmount,
                Status = Status.Pendiente
            };

            await _obligationRepository.AddAsync(obligation);
        }

        /// <summary>
        /// Actualiza obligación existente.
        /// </summary>
        private async Task UpdateExistingObligationAsync(
            ObligationMonth existing,
            Contract contract,
            decimal uvtValue,
            decimal vatRate,
            decimal baseAmount,
            decimal vatAmount,
            decimal totalAmount,
            DateTime dueDate)
        {
            existing.UvtQtyApplied = contract.TotalUvtQtyAgreed;
            existing.UvtValueApplied = uvtValue;
            existing.VatRateApplied = vatRate;
            existing.BaseAmount = baseAmount;
            existing.VatAmount = vatAmount;
            existing.TotalAmount = totalAmount;
            existing.DueDate = dueDate;

            if (existing.Status == Status.Rechazada)
                existing.Status = Status.Pendiente;

            await _obligationRepository.UpdateAsync(existing);
        }

        private (DateTime MonthStart, DateTime MonthEnd, DateTime DueDate) GetPeriodDates(int year, int month)
        {
            var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);
            var dueDate = new DateTime(year, month, DateTime.DaysInMonth(year, month), 0, 0, 0, DateTimeKind.Utc);
            return (monthStart, monthEnd, dueDate);
        }

        private (decimal BaseAmount, decimal VatAmount, decimal TotalAmount) CalculateAmounts(Contract contract, decimal uvtValue, decimal vatRate)
        {
            decimal baseAmount = contract.TotalBaseRentAgreed > 0m
                ? contract.TotalBaseRentAgreed
                : contract.TotalUvtQtyAgreed * uvtValue;

            decimal vatAmount = baseAmount * vatRate;
            return (baseAmount, vatAmount, baseAmount + vatAmount);
        }

        private async Task<decimal> GetParameterValueAsync(string key, DateTime date)
        {
            var param = await _systemParamRepository.GetAllQueryable()
                .Where(p => p.Key == key && p.EffectiveFrom <= date && (p.EffectiveTo == null || p.EffectiveTo >= date))
                .OrderByDescending(p => p.EffectiveFrom)
                .FirstOrDefaultAsync();

            if (param == null)
                throw new BusinessException($"Parámetro '{key}' no encontrado para la fecha {date:yyyy-MM-dd}.");

            if (!TryParseDecimalFlexible(param.Value, out var value))
                throw new BusinessException($"Valor inválido para parámetro '{key}': '{param.Value}'.");

            if (key.Equals("IVA", StringComparison.OrdinalIgnoreCase))
            {
                if (value >= 1m) value /= 100m;
                if (value < 0m || value > 1m)
                    throw new BusinessException($"El parámetro 'IVA' debe estar entre 0 y 1. Recibido: {value}.");
            }

            if (key.Equals("UVT", StringComparison.OrdinalIgnoreCase) && value <= 0m)
                throw new BusinessException("UVT debe ser mayor que 0.");

            return value;
        }

        private static bool TryParseDecimalFlexible(string raw, out decimal value)
        {
            raw = raw?.Trim() ?? "";
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out value)) return true;
            var es = CultureInfo.GetCultureInfo("es-CO");
            if (decimal.TryParse(raw, NumberStyles.Any, es, out value)) return true;
            return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }

        /// <summary>
        /// Obtiene un parámetro entero del sistema con valor por defecto.
        /// </summary>
        private async Task<int> GetParameterIntAsync(string key, int defaultValue)
        {
            try
            {
                var param = await _systemParamRepository.GetAllQueryable()
                    .Where(p => p.Key == key 
                        && p.EffectiveFrom <= DateTime.UtcNow 
                        && (p.EffectiveTo == null || p.EffectiveTo >= DateTime.UtcNow))
                    .OrderByDescending(p => p.EffectiveFrom)
                    .FirstOrDefaultAsync();

                if (param == null)
                    return defaultValue;

                if (!int.TryParse(param.Value, out var value))
                    return defaultValue;

                return value;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Calcula los días efectivos del contrato en un mes específico.
        /// </summary>
        private (int effectiveDays, int totalDaysInMonth) CalculateEffectiveDays(
            Contract contract, 
            DateTime periodDate)
        {
            var monthStart = new DateTime(periodDate.Year, periodDate.Month, 1);
            var monthEnd = monthStart.AddMonths(1);
            int totalDaysInMonth = DateTime.DaysInMonth(periodDate.Year, periodDate.Month);

            // Día inicial efectivo: mayor entre inicio del mes e inicio del contrato
            var effectiveStart = contract.StartDate > monthStart 
                ? contract.StartDate 
                : monthStart;

            // Día final efectivo: menor entre fin del mes y fin del contrato
            var effectiveEnd = contract.EndDate < monthEnd 
                ? contract.EndDate.AddDays(1) // Incluir el último día
                : monthEnd;

            int effectiveDays = (effectiveEnd - effectiveStart).Days;

            return (effectiveDays, totalDaysInMonth);
        }

        /// <summary>
        /// Determina si periodDate es el primer mes del contrato.
        /// </summary>
        private bool IsFirstMonthOfContract(Contract contract, DateTime periodDate)
        {
            return contract.StartDate.Year == periodDate.Year 
                && contract.StartDate.Month == periodDate.Month;
        }

        /// <summary>
        /// Determina si periodDate es el segundo mes del contrato.
        /// </summary>
        private bool IsSecondMonthOfContract(Contract contract, DateTime periodDate)
        {
            var secondMonth = contract.StartDate.AddMonths(1);
            return secondMonth.Year == periodDate.Year 
                && secondMonth.Month == periodDate.Month;
        }

        public async Task<decimal> GetTotalObligationsPaidByDayAsync(DateTime date)
        {
            return await _obligationRepository.GetTotalObligationsPaidByDayAsync(date);
        }

        public async Task<decimal> GetTotalObligationsPaidByMonthAsync(int year, int month)
        {
            return await _obligationRepository.GetTotalObligationsPaidByMonthAsync(year, month);
        }

        protected override Expression<Func<ObligationMonth, string>>[] SearchableFields() =>
        [
            e => e.Status.ToString()
        ];

        protected override string[] SortableFields() =>
        [
            nameof(ObligationMonth.Year),
            nameof(ObligationMonth.Month),
            nameof(ObligationMonth.DueDate),
            nameof(ObligationMonth.BaseAmount),
            nameof(ObligationMonth.TotalAmount),
            nameof(ObligationMonth.LateAmount),
            nameof(ObligationMonth.Status),
            nameof(ObligationMonth.Locked),
            nameof(ObligationMonth.Active),
            nameof(ObligationMonth.CreatedAt),
            nameof(ObligationMonth.Id)
        ];

        protected override IDictionary<string, Func<string, Expression<Func<ObligationMonth, bool>>>> AllowedFilters() =>
        new Dictionary<string, Func<string, Expression<Func<ObligationMonth, bool>>>>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(ObligationMonth.ContractId)] = val => e => e.ContractId == int.Parse(val),
            [nameof(ObligationMonth.Year)] = val => e => e.Year == int.Parse(val),
            [nameof(ObligationMonth.Month)] = val => e => e.Month == int.Parse(val),
            [nameof(ObligationMonth.Status)] = val => e => e.Status.ToString() == val,
            [nameof(ObligationMonth.Locked)] = val => e => e.Locked == bool.Parse(val),
            [nameof(ObligationMonth.Active)] = val => e => e.Active == bool.Parse(val),
            [nameof(ObligationMonth.DueDate)] = val => e => e.DueDate.Date == DateTime.Parse(val).Date
        };
    }
}
