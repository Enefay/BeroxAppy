﻿using BeroxAppy.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeroxAppy.Finances.FinanceAppDtos
{
    public class DashboardDto
    {
        public DateTime Date { get; set; }
        public decimal TodayIncome { get; set; }
        public decimal TodayExpenses { get; set; }
        public decimal TodayProfit { get; set; }
        public decimal PendingCommissions { get; set; }
        public List<EmployeeCommissionSummaryDto> EmployeeCommissions { get; set; }
    }

    public class EmployeeCommissionSummaryDto
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public decimal CurrentCommission { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public bool CanPay { get; set; }
    }

    public class PayCommissionDto
    {
        public Guid EmployeeId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string Note { get; set; }
    }

    public class EmployeePerformanceDto
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal TotalCommissionEarned { get; set; }
        public decimal TotalCommissionPaid { get; set; }
        public decimal TotalSalaryPaid { get; set; }
        public int ServiceCount { get; set; }
        public List<CommissionDetailDto> Commissions { get; set; }
    }

    public class CommissionDetailDto
    {
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public bool IsPaid { get; set; }
    }
}
