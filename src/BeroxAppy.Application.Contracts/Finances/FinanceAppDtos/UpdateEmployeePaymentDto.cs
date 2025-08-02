using BeroxAppy.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeroxAppy.Finances.FinanceAppDtos
{
    public class UpdateEmployeePaymentDto
    {
        public decimal SalaryAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal BonusAmount { get; set; }

        public DateTime PaymentDate { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string Note { get; set; }

        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }
}
