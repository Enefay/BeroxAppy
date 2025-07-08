using BeroxAppy.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace BeroxAppy.Reservations
{
    public class GetReservationsInput : PagedAndSortedResultRequestDto
    {
        public string? Filter { get; set; } // Müşteri adı, telefon
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? EmployeeId { get; set; }
        public Guid? ServiceId { get; set; }
        public ReservationStatus? Status { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
        public bool? IsWalkIn { get; set; }
        public bool? IsToday { get; set; }
        public bool? IsPast { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
    }
}
