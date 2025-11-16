using Business.Interfaces.Implements.Business;
namespace WebGESCOMPH.RealTime.Contract
{
    public sealed class ContractJobs
    {
        private readonly ILogger<ContractJobs> _logger;
        private readonly IContractService _contractService;

        public ContractJobs(
            ILogger<ContractJobs> logger,
            IContractService contractService)
        {
            _logger = logger;
            _contractService = contractService;
        }

        public async Task RunExpirationSweepAsync()
        {
            try
            {
                _logger.LogInformation("Barrido de contratos vencidos: INICIO");

                var result = await _contractService.RunExpirationSweepAsync();

                _logger.LogInformation(
                    "Barrido de contratos vencidos: FIN OK. Deactivated={Deactivated}, Reactivated={Reactivated}",
                    result.DeactivatedContractIds.Count,
                    result.ReactivatedEstablishments
                );  
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ContractJobs.RunExpirationSweepAsync");
            }
        }
    }

}