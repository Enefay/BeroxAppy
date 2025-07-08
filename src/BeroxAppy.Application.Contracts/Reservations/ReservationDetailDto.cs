using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace BeroxAppy.Reservations
{
    public class ReservationDetailDto : EntityDto<Guid>
    {
        public Guid ReservationId { get; set; }
        public Guid ServiceId { get; set; }
        public Guid EmployeeId { get; set; }

        public decimal ServicePrice { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal CommissionRate { get; set; }
        public decimal CommissionAmount { get; set; }

        [MaxLength(200)]
        public string Note { get; set; }

        // Display alanları
        public string ServiceTitle { get; set; }
        public string ServiceCategoryName { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeColor { get; set; } // Calendar color
        public string TimeDisplay { get; set; } // "10:00 - 11:00"
        public int DurationMinutes { get; set; }
        public string DurationDisplay { get; set; } // "1 saat"
    }
}
