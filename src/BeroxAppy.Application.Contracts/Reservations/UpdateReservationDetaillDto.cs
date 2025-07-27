using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeroxAppy.Reservations
{
    public class UpdateReservationDetaillDto
    {
        public Guid Id { get; set; }
        [Required]
        public Guid ServiceId { get; set; }

        [Required]
        public Guid EmployeeId { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        public decimal ServicePrice { get; set; } 

        [MaxLength(200)]
        public string Note { get; set; }
    }
}
