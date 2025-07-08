using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeroxAppy.Reservations
{
    public class CreateReservationDto
    {
        [Required]
        public Guid CustomerId { get; set; }

        [MaxLength(500)]
        public string Note { get; set; }

        [Required]
        public DateTime ReservationDate { get; set; }

        public decimal? DiscountAmount { get; set; }
        public decimal? ExtraAmount { get; set; }

        public bool IsWalkIn { get; set; } = false;

        [Required]
        public List<CreateReservationDetailDto> ReservationDetails { get; set; } = new();
    }
}
