using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace BeroxAppy.Employees
{
    public class EmployeeWorkingHoursDto : EntityDto<Guid>
    {
        public Guid EmployeeId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public TimeSpan? BreakStartTime { get; set; }
        public TimeSpan? BreakEndTime { get; set; }
        public bool IsActive { get; set; } = true;

        // Display alanları
        public string? DayOfWeekDisplay { get; set; } // "Pazartesi", "Salı" vb.
        public string? WorkingHoursDisplay { get; set; } // "09:00 - 18:00"
        public string? BreakDisplay { get; set; } // "12:00 - 13:00" veya "Mola Yok"
    }
}
