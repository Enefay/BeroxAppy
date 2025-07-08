using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeroxAppy.Reservations
{
    public class CalendarEventDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Color { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string Services { get; set; }
        public string Status { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsWalkIn { get; set; }
    }
}
