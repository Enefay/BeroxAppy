using BeroxAppy.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace BeroxAppy.Employees
{
    // Çalışan-Hizmet İlişkisi (Hangi çalışan hangi hizmetleri verebilir)
    public class EmployeeService : Entity<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }

        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; }

        public Guid ServiceId { get; set; }
        public Service Service { get; set; }
    }
}
