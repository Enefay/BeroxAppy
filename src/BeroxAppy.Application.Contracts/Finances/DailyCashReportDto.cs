using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeroxAppy.Finances
{
    public class DailyCashReportDto
    {
        public DateTime Date { get; set; }
        public bool HasData { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal TotalCashIn { get; set; }
        public decimal TotalCashOut { get; set; }
        public decimal TheoreticalClosing { get; set; }
        public decimal ActualClosing { get; set; }
        public bool IsClosed { get; set; }
        public List<PaymentDto> Payments { get; set; } = new();
    }
}
