using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace BeroxAppy.Employees
{
    // Çalışma Saatleri
    public class EmployeeWorkingHours : Entity<Guid>, IMultiTenant
    {
        public Guid? TenantId { get; set; }

        public Guid EmployeeId { get; set; }
        public Employee Employee { get; set; }

        public DayOfWeek DayOfWeek { get; set; }

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public TimeSpan? BreakStartTime { get; set; }
        public TimeSpan? BreakEndTime { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
