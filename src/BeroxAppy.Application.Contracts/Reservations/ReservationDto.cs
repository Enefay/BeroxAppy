using BeroxAppy.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace BeroxAppy.Reservations
{
    public class ReservationDto : FullAuditedEntityDto<Guid>
    {
        public Guid CustomerId { get; set; }

        [MaxLength(500)]
        public string Note { get; set; }

        public DateTime ReservationDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public decimal TotalServicePrice { get; set; }
        public decimal FinalPrice { get; set; }

        public decimal? DiscountAmount { get; set; }
        public decimal? ExtraAmount { get; set; }

        public ReservationStatus Status { get; set; }
        public PaymentStatus PaymentStatus { get; set; }

        public bool IsWalkIn { get; set; } // true: adisyon, false: rezervasyon
        public bool ReminderSent { get; set; }

        // Display için hesaplanan alanlar
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string StatusDisplay { get; set; }
        public string PaymentStatusDisplay { get; set; }
        public string ReservationTimeDisplay { get; set; } // "10:00 - 11:30"
        public string DurationDisplay { get; set; } // "1 saat 30 dakika"
        public int TotalDurationMinutes { get; set; }
        public bool IsToday { get; set; }
        public bool IsPast { get; set; }
        public string ReservationTypeDisplay { get; set; } // "Rezervasyon" / "Adisyon"

        // İlişkili veriler
        public List<ReservationDetailDto> ReservationDetails { get; set; } = new();
    }
}
