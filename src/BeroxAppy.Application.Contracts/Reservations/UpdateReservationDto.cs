using BeroxAppy.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeroxAppy.Reservations
{
    public class UpdateReservationDto
    {
        public Guid Id { get; set; } 

        [Required]
        public Guid CustomerId { get; set; }

        [MaxLength(500)]
        public string Note { get; set; }

        [Required]
        public DateTime ReservationDate { get; set; }

        public decimal? DiscountAmount { get; set; }
        public decimal? ExtraAmount { get; set; }
        public bool IsWalkIn { get; set; } = false;
        public List<UpdateReservationDetaillDto> ReservationDetails { get; set; } = new();
        public ReservationStatus Status { get; set; }
    }
}
