using BeroxAppy.Employees;
using BeroxAppy.Enums;
using BeroxAppy.Reservations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace BeroxAppy.Finance
{
    public class EmployeeCommission : Entity<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }

        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; }

        public Guid? ReservationDetailId { get; set; }
        public ReservationDetail ReservationDetail { get; set; }

        //public Guid? ProductSaleId { get; set; } //todo İleride ürün satışları için

        public decimal Amount { get; set; }
        public DateTime EarnedDate { get; set; } //kaznılan tarih
        public bool IsPaid { get; set; } = false;
        public DateTime? PaidDate { get; set; }

        public CommissionType Type { get; set; } // Service, Product, Package

        public string? Description { get; set; }


    }
}
