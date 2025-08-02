using BeroxAppy.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeroxAppy.Finances.FinanceAppDtos
{
    public class EmployeePaymentDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; }

        public decimal SalaryAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal BonusAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public DateTime PaymentDate { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string Note { get; set; }

        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public PaymentType PaymentType { get; set; }

        public DateTime CreationTime { get; set; }
    }
}
