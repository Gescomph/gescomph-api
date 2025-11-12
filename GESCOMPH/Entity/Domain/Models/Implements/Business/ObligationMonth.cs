using Entity.Domain.Models.ModelBase;

namespace Entity.Domain.Models.Implements.Business
{
    public class ObligationMonth : BaseModel
    {

        public int ContractId { get; set; }
        public Contract Contract { get; set; } = null!;

        public int Year { get; set; }
        public int Month { get; set; }

        public DateTime DueDate { get; set; }

        // Atributo nullable que cumple la funcion de registrar la
        // fecha en la que se pago la obligacion 
        public DateTime? PaymentDate { get; set; }

        // Fotocopia de los parámetros usados este mes
        public decimal UvtQtyApplied { get; set; }
        public decimal UvtValueApplied { get; set; }
        public decimal VatRateApplied { get; set; }

        // Cálculo congelado
        public decimal BaseAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalAmount { get; set; }


        // Fecha cuando se envió aviso previo al vencimiento
        public DateTime? NotifiedDueSoonAt { get; set; }


        // Fecha cuando se notificó que está en mora
        public DateTime? NotifiedOverdueAt { get; set; }

        // Mora
        public int? DaysLate { get; set; }
        public decimal? LateAmount { get; set; }
        public decimal? LateFeeAmount { get; set; } // Valor acumulado por mora

        // Estado
        public string Status { get; set; }

        public bool Locked { get; set; } = false;
    }
}