using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeroxAppy.Reservations
{
    public class CreateReservationDetailDto
    {
        [Required]
        public Guid ServiceId { get; set; }

        [Required]
        public Guid EmployeeId { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        public decimal? CustomPrice { get; set; } // Özel fiyat (null ise service fiyatı)

        [MaxLength(200)]
        public string Note { get; set; }
    }
}
