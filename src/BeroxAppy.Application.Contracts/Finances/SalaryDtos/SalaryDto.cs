using BeroxAppy.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeroxAppy.Finances.SalaryDtos
{
    // Maaş özeti DTO'su
    public class EmployeeSalarySummaryDto
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public decimal FixedSalary { get; set; }
        public SalaryPeriodType SalaryPeriod { get; set; }
        public int PaymentDay { get; set; }
        public int? PaymentWeekday { get; set; } //Haftalık ödemeler için
        public DateTime? LastSalaryPaymentDate { get; set; }
        public DateTime NextPaymentDue { get; set; }
        public bool IsDue { get; set; }
        public bool CanPay { get; set; }
        public int DaysOverdue { get; set; }
        public PaymentMethod PreferredPaymentMethod { get; set; }
        public string SalaryPeriodDisplay { get; set; }
        public decimal CalculatedAmount { get; set; } // Dönem başına hesaplanan tutar
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string PeriodDisplay { get; set; }
    }

    // Maaş ödeme özeti
    public class SalaryPaymentSummaryDto
    {
        public decimal TotalDueSalaries { get; set; }
        public int DueEmployeeCount { get; set; }
        public decimal ThisMonthPaidSalaries { get; set; }
        public int TotalEmployeeCount { get; set; }
        public Dictionary<SalaryPeriodType, SalaryPeriodSummary> PeriodSummaries { get; set; }
    }

    public class SalaryPeriodSummary
    {
        public int EmployeeCount { get; set; }
        public decimal TotalAmount { get; set; }
        public int DueCount { get; set; }
    }

    // Maaş ödeme isteği
    public class PaySalaryRequestDto
    {
        public List<EmployeeSalaryDto> EmployeeSalaries { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string Note { get; set; }
    }

    public class EmployeeSalaryDto
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public decimal Amount { get; set; }
    }

    // Maaş performans raporu
    public class EmployeeSalaryPerformanceDto
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal TotalSalaryPaid { get; set; }
        public decimal TotalCommissionPaid { get; set; }
        public decimal TotalBonusPaid { get; set; }
        public decimal GrandTotal { get; set; }
        public int PaymentCount { get; set; }
        public List<SalaryPaymentDetailDto> PaymentHistory { get; set; }
    }

    public class SalaryPaymentDetailDto
    {
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string PaymentMethodDisplay { get; set; }
        public string Note { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    public class SalaryPeriodInfo
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public DateTime PaymentDueDate { get; set; }
    }
}
