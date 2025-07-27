using BeroxAppy.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace BeroxAppy.Finances
{
    public class GetPaymentsInput : PagedAndSortedResultRequestDto
    {
        public Guid? CustomerId { get; set; }
        public Guid? ReservationId { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsRefund { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
    }
}
