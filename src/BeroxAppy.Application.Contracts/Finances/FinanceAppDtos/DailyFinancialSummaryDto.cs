using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeroxAppy.Finances.FinanceAppDtos

{
    public class DailyFinancialSummaryDto
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }

        // Gelirler
        public decimal ServiceIncome { get; set; }
        public decimal ProductIncome { get; set; }
        public decimal OtherIncome { get; set; }
        public decimal TotalIncome { get; set; }

        // Giderler
        public decimal EmployeePayments { get; set; }
        public decimal OperationalExpenses { get; set; }
        public decimal OtherExpenses { get; set; }
        public decimal TotalExpenses { get; set; }

        // Net
        public decimal NetProfit { get; set; }

        // Ödeme yöntemleri
        public decimal CashAmount { get; set; }
        public decimal CreditCardAmount { get; set; }
        public decimal DebitCardAmount { get; set; }
        public decimal BankTransferAmount { get; set; }

        // İstatistikler
        public int TotalReservations { get; set; }
        public int CompletedReservations { get; set; }
        public int CancelledReservations { get; set; }
        public decimal AverageTicketSize { get; set; }

        // Durum
        public bool IsClosed { get; set; }
        public DateTime? ClosedDate { get; set; }
        public string Note { get; set; }

        // Hesaplanan
        public decimal ProfitMargin { get; set; }
    }

    public class CreateDailyFinancialSummaryDto
    {
        public DateTime Date { get; set; } = DateTime.Now.Date;
        public string Note { get; set; }
    }

    public class UpdateDailyFinancialSummaryDto
    {
        public decimal OtherIncome { get; set; }
        public decimal OperationalExpenses { get; set; }
        public decimal OtherExpenses { get; set; }
        public string Note { get; set; }
    }

    public class CloseDayDto
    {
        public DateTime Date { get; set; }
        public string Note { get; set; }
    }
}
