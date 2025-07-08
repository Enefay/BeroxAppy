using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeroxAppy.Reservations
{
    public class AvailabilityCheckDto
    {
        public DateTime Date { get; set; }
        public Guid EmployeeId { get; set; }
        public Guid ServiceId { get; set; }
        public List<TimeSlotDto> AvailableSlots { get; set; } = new();
    }

    public class TimeSlotDto
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Display { get; set; } // "10:00 - 11:00"
        public bool IsAvailable { get; set; }
    }
}
