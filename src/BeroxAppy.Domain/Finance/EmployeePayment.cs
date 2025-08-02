using BeroxAppy.Employees;
using BeroxAppy.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BeroxAppy.Finance
{
    //Maaş/komisyon ödemeleri
    public class EmployeePayment : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }

        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; }

        public decimal SalaryAmount { get; set; } // Sabit maaş
        public decimal CommissionAmount { get; set; } // Komisyon
        public decimal BonusAmount { get; set; } // Ek prim/kesinti (+/-)
        public decimal TotalAmount { get; set; } // Net toplam

        public DateTime PaymentDate { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? Note { get; set; }

        // Dönem bilgisi
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public PaymentType PaymentType { get; set; } // Salary, Commission, Bonus

    }
}
