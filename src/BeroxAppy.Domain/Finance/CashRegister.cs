using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BeroxAppy.Finance
{
    // Kasa (Gün sonu için)
    public class CashRegister : FullAuditedAggregateRoot<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }

        public DateTime Date { get; set; }

        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }

        public decimal TotalCashIn { get; set; }
        public decimal TotalCashOut { get; set; }

        public bool IsClosed { get; set; } = false;

        [MaxLength(500)]
        public string Note { get; set; }

        // Navigation property
        public ICollection<Payment> Payments { get; set; }
    }
}
