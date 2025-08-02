using BeroxAppy.Employees;
using BeroxAppy.Enums;
using BeroxAppy.Finance;
using BeroxAppy.Finances;
using BeroxAppy.Finances.FinanceAppDtos;
using BeroxAppy.Reservations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace BeroxAppy.Services
{
    public class FinanceAppService : ApplicationService, IFinanceAppService
    {
        private readonly IRepository<Employee, Guid> _employeeRepository;
        private readonly IRepository<EmployeeCommission, Guid> _commissionRepository;
        private readonly IRepository<EmployeePayment, Guid> _employeePaymentRepository;
        private readonly IRepository<Payment, Guid> _paymentRepository;
        private readonly IRepository<DailyFinancialSummary, Guid> _dailySummaryRepository;
        private readonly IRepository<Reservation, Guid> _reservationRepository;

        public FinanceAppService(
            IRepository<Employee, Guid> employeeRepository,
            IRepository<EmployeeCommission, Guid> commissionRepository,
            IRepository<EmployeePayment, Guid> employeePaymentRepository,
            IRepository<Payment, Guid> paymentRepository,
            IRepository<DailyFinancialSummary, Guid> dailySummaryRepository,
            IRepository<Reservation, Guid> reservationRepository)
        {
            _employeeRepository = employeeRepository;
            _commissionRepository = commissionRepository;
            _employeePaymentRepository = employeePaymentRepository;
            _paymentRepository = paymentRepository;
            _dailySummaryRepository = dailySummaryRepository;
            _reservationRepository = reservationRepository;
        }

        // Dashboard verileri
        public async Task<DashboardDto> GetDashboardAsync(DateTime? date = null)
        {
            var targetDate = date?.Date ?? DateTime.Now.Date;

            // Günlük gelir hesapla
            var todayPayments = await _paymentRepository.GetListAsync(x =>
                x.PaymentDate.Date == targetDate && !x.IsRefund);
            var todayIncome = todayPayments.Sum(x => x.Amount);

            // Günlük giderler (personel ödemeleri)
            var todayEmployeePayments = await _employeePaymentRepository.GetListAsync(x =>
                x.PaymentDate.Date == targetDate);
            var todayExpenses = todayEmployeePayments.Sum(x => x.TotalAmount);

            // Bekleyen komisyonlar
            var employees = await _employeeRepository.GetListAsync(x => x.IsActive);
            var totalPendingCommissions = employees.Sum(x => x.CurrentPeriodCommission);

            return new DashboardDto
            {
                Date = targetDate,
                TodayIncome = todayIncome,
                TodayExpenses = todayExpenses,
                TodayProfit = todayIncome - todayExpenses,
                PendingCommissions = totalPendingCommissions,
                EmployeeCommissions = await GetEmployeeCommissionsAsync()
            };
        }

        // Çalışan komisyonları listesi
        public async Task<List<EmployeeCommissionSummaryDto>> GetEmployeeCommissionsAsync()
        {
            var employees = await _employeeRepository.GetListAsync(x => x.IsActive);
            var result = new List<EmployeeCommissionSummaryDto>();

            foreach (var employee in employees)
            {
                var lastPayment = await _employeePaymentRepository
                    .FindAsync(x => x.EmployeeId == employee.Id && x.PaymentType == PaymentType.Commission);

                result.Add(new EmployeeCommissionSummaryDto
                {
                    EmployeeId = employee.Id,
                    EmployeeName = $"{employee.FirstName} {employee.LastName}",
                    CurrentCommission = employee.CurrentPeriodCommission,
                    LastPaymentDate = lastPayment?.PaymentDate,
                    CanPay = employee.CurrentPeriodCommission > 0
                });
            }

            return result.OrderByDescending(x => x.CurrentCommission).ToList();
        }

        // Komisyon öde
        public async Task PayCommissionAsync(PayCommissionDto input)
        {
            var employee = await _employeeRepository.GetAsync(input.EmployeeId);

            if (employee.CurrentPeriodCommission <= 0)
            {
                throw new UserFriendlyException("Bu çalışanın ödenecek komisyonu bulunmuyor!");
            }

            if (input.Amount > employee.CurrentPeriodCommission)
            {
                throw new UserFriendlyException("Ödeme tutarı toplam komisyondan fazla olamaz!");
            }

            // Ödeme kaydı oluştur
            var payment = new EmployeePayment
            {
                EmployeeId = input.EmployeeId,
                SalaryAmount = 0,
                CommissionAmount = input.Amount,
                BonusAmount = 0,
                TotalAmount = input.Amount,
                PaymentDate = DateTime.Now,
                PaymentMethod = input.PaymentMethod,
                Note = input.Note ?? $"Komisyon ödemesi - {DateTime.Now:dd.MM.yyyy}",
                PeriodStart = employee.LastCommissionResetDate,
                PeriodEnd = DateTime.Now,
                PaymentType = PaymentType.Commission
            };

            await _employeePaymentRepository.InsertAsync(payment);

            // Çalışanın komisyonunu düş
            employee.CurrentPeriodCommission -= input.Amount;

            // Eğer tam ödeme yapıldıysa komisyonları "ödendi" olarak işaretle
            if (input.Amount == employee.CurrentPeriodCommission + input.Amount)
            {
                var unpaidCommissions = await _commissionRepository.GetListAsync(x =>
                    x.EmployeeId == input.EmployeeId && !x.IsPaid);

                foreach (var commission in unpaidCommissions)
                {
                    commission.IsPaid = true;
                    commission.PaidDate = DateTime.Now;
                    await _commissionRepository.UpdateAsync(commission);
                }

                employee.LastCommissionResetDate = DateTime.Now;
            }

            await _employeeRepository.UpdateAsync(employee);
        }

        // Çalışan performans raporu
        public async Task<EmployeePerformanceDto> GetEmployeePerformanceAsync(Guid employeeId, DateTime startDate, DateTime endDate)
        {
            var employee = await _employeeRepository.GetAsync(employeeId);

            var commissions = await _commissionRepository.GetListAsync(x =>
                x.EmployeeId == employeeId &&
                x.EarnedDate >= startDate &&
                x.EarnedDate <= endDate);

            var payments = await _employeePaymentRepository.GetListAsync(x =>
                x.EmployeeId == employeeId &&
                x.PaymentDate >= startDate &&
                x.PaymentDate <= endDate);

            return new EmployeePerformanceDto
            {
                EmployeeId = employeeId,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                PeriodStart = startDate,
                PeriodEnd = endDate,
                TotalCommissionEarned = commissions.Sum(x => x.Amount),
                TotalCommissionPaid = payments.Where(x => x.PaymentType == PaymentType.Commission).Sum(x => x.CommissionAmount),
                TotalSalaryPaid = payments.Where(x => x.PaymentType == PaymentType.Salary).Sum(x => x.SalaryAmount),
                ServiceCount = commissions.Count(x => x.Type == CommissionType.Service),
                Commissions = commissions.Select(x => new CommissionDetailDto
                {
                    Amount = x.Amount,
                    Date = x.EarnedDate,
                    Description = x.Description,
                    IsPaid = x.IsPaid
                }).ToList()
            };
        }

        // Günlük özet getir veya oluştur
        public async Task<DailyFinancialSummaryDto> GetOrCreateDailySummaryAsync(DateTime date)
        {
            var targetDate = date.Date;

            var summary = await _dailySummaryRepository.FindAsync(x => x.Date == targetDate);

            if (summary == null)
            {
                summary = await CreateDailySummaryAsync(targetDate);
            }
            else
            {

                // Gün kapatılmamışsa güncel verileri hesapla ve güncelle
                // Kapatılmış günler için veriyi koruma
                if (!summary.IsClosed) //todo kontrol
                {
                    await UpdateDailySummaryAsync(summary);
                }
            }

            return ObjectMapper.Map<DailyFinancialSummary, DailyFinancialSummaryDto>(summary);
        }


        // Günü kapat
        public async Task<DailyFinancialSummaryDto> CloseDayAsync(CloseDayDto input)
        {
            var summary = await _dailySummaryRepository.FindAsync(x => x.Date == input.Date.Date);

            if (summary == null)
            {
                summary = await CreateDailySummaryAsync(input.Date.Date);
            }

            if (summary.IsClosed)
            {
                throw new UserFriendlyException("Bu gün zaten kapatılmış!");
            }

            // Son hesaplamaları yap
            await CalculateDailySummaryAsync(summary);

            // Günü kapat
            summary.IsClosed = true;
            summary.ClosedDate = DateTime.Now;
            summary.ClosedByUserId = CurrentUser.Id;
            summary.Note = input.Note;

            await _dailySummaryRepository.UpdateAsync(summary);

            return ObjectMapper.Map<DailyFinancialSummary, DailyFinancialSummaryDto>(summary);
        }

        // Haftalık/Aylık rapor
        public async Task<List<DailyFinancialSummaryDto>> GetPeriodSummaryAsync(DateTime startDate, DateTime endDate)
        {
            var summaries = await _dailySummaryRepository.GetListAsync(x =>
                x.Date >= startDate.Date && x.Date <= endDate.Date);

            return ObjectMapper.Map<List<DailyFinancialSummary>, List<DailyFinancialSummaryDto>>(
                summaries.OrderBy(x => x.Date).ToList());
        }

        //komisyon ödeme metodu:
        public async Task PayEmployeeCommissionAsync(Guid employeeId, decimal amount, PaymentMethod paymentMethod, string? note = null)
        {
            var employee = await _employeeRepository.GetAsync(employeeId);

            if (employee.CurrentPeriodCommission <= 0)
            {
                throw new UserFriendlyException("Bu çalışanın ödenecek komisyonu yok!");
            }

            if (amount > employee.CurrentPeriodCommission)
            {
                throw new UserFriendlyException("Ödeme tutarı toplam komisyondan fazla!");
            }

            // Ödeme kaydı
            var payment = new EmployeePayment
            {
                EmployeeId = employeeId,
                SalaryAmount = 0,
                CommissionAmount = amount,
                BonusAmount = 0,
                TotalAmount = amount,
                PaymentDate = DateTime.Now,
                PaymentMethod = paymentMethod,
                Note = note ?? $"Komisyon ödemesi - {DateTime.Now:dd.MM.yyyy}",
                PeriodStart = employee.LastCommissionResetDate,
                PeriodEnd = DateTime.Now,
                PaymentType = PaymentType.Commission
            };

            await _employeePaymentRepository.InsertAsync(payment);

            // Komisyonu düş
            employee.CurrentPeriodCommission -= amount;

            // Tam ödeme yapıldıysa komisyonları "ödendi" yap
            if (employee.CurrentPeriodCommission <= 0)
            {
                var unpaidCommissions = await _commissionRepository.GetListAsync(x =>
                    x.EmployeeId == employeeId && !x.IsPaid);

                foreach (var commission in unpaidCommissions)
                {
                    commission.IsPaid = true;
                    commission.PaidDate = DateTime.Now;
                    await _commissionRepository.UpdateAsync(commission);
                }

                employee.LastCommissionResetDate = DateTime.Now;
                employee.CurrentPeriodCommission = 0;
            }

            await _employeeRepository.UpdateAsync(employee);
        }


        // Günlük özet oluştur
        private async Task<DailyFinancialSummary> CreateDailySummaryAsync(DateTime date)
        {
            var summary = new DailyFinancialSummary
            {
                Date = date.Date,
                IsClosed = false
            };

            // İlk oluşturulduğunda hesaplamaları yap
            await CalculateDailySummaryAsync(summary);
            // Veritabanına kaydet
            await _dailySummaryRepository.InsertAsync(summary);

            return summary;
        }

        // Günlük özeti güncelle
        private async Task UpdateDailySummaryAsync(DailyFinancialSummary summary)
        {
            if (summary.IsClosed) return; // Kapalı günler güncellenmez

            // Güncel hesaplamaları yap
            await CalculateDailySummaryAsync(summary);
            await _dailySummaryRepository.UpdateAsync(summary);
        }

        // Günlük özet hesapla
        private async Task CalculateDailySummaryAsync(DailyFinancialSummary summary)
        {
            var date = summary.Date;

            // GELİRLER - İade edilmeyenler
            var payments = await _paymentRepository.GetListAsync(x =>
                x.PaymentDate.Date == date && !x.IsRefund);

            // İADELER - Ayrı hesapla
            var refunds = await _paymentRepository.GetListAsync(x =>
                x.PaymentDate.Date == date && x.IsRefund);

            // Net hizmet geliri (gelir - iade)
            var grossServiceIncome = payments.Where(x => x.ReservationId.HasValue).Sum(x => x.Amount);
            var serviceRefunds = refunds.Where(x => x.ReservationId.HasValue).Sum(x => x.Amount);
            summary.ServiceIncome = Math.Max(0, grossServiceIncome - serviceRefunds);

            // Diğer gelirler (rezervasyon dışı ödemeler)
            var otherIncomePayments = payments.Where(x => !x.ReservationId.HasValue).Sum(x => x.Amount);
            var otherRefunds = refunds.Where(x => !x.ReservationId.HasValue).Sum(x => x.Amount);
            summary.OtherIncome = Math.Max(0, otherIncomePayments - otherRefunds);

            // Ürün gelirleri (şimdilik 0, ileride ürün satışları için)
            summary.ProductIncome = 0;

            // Toplam gelir
            summary.TotalIncome = summary.ServiceIncome + summary.ProductIncome + summary.OtherIncome;

            // GİDERLER
            var employeePayments = await _employeePaymentRepository.GetListAsync(x =>
                x.PaymentDate.Date == date);
            summary.EmployeePayments = employeePayments.Sum(x => x.TotalAmount);

            // Diğer giderler ve operasyonel giderler şimdilik aynı (manuel girişse 0 kalır)
            // İleride ayrı gider tablosu eklerseniz buraya ekleyebilirsiniz
            summary.TotalExpenses = summary.EmployeePayments + summary.OperationalExpenses + summary.OtherExpenses;

            // NET KAR/ZARAR
            summary.NetProfit = summary.TotalIncome - summary.TotalExpenses;

            // ÖDEME YÖNTEMLERİ DAĞILIMI (sadece net ödemeler, iadeler düşülmüş)
            var allNetPayments = payments.Concat(refunds.Select(r => new Payment
            {
                PaymentMethod = r.PaymentMethod,
                Amount = -r.Amount // İadeleri negatif yap
            }));

            summary.CashAmount = allNetPayments.Where(x => x.PaymentMethod == PaymentMethod.Cash).Sum(x => x.Amount);
            summary.CreditCardAmount = allNetPayments.Where(x => x.PaymentMethod == PaymentMethod.CreditCard).Sum(x => x.Amount);
            summary.DebitCardAmount = allNetPayments.Where(x => x.PaymentMethod == PaymentMethod.DebitCard).Sum(x => x.Amount);
            summary.BankTransferAmount = allNetPayments.Where(x => x.PaymentMethod == PaymentMethod.BankTransfer).Sum(x => x.Amount);

            // İSTATİSTİKLER
            var reservations = await _reservationRepository.GetListAsync(x => x.ReservationDate.Date == date);
            summary.TotalReservations = reservations.Count;
            summary.CompletedReservations = reservations.Count(x => x.Status == ReservationStatus.Arrived);
            summary.CancelledReservations = reservations.Count(x => x.Status == ReservationStatus.NoShow);

            // Ortalama işlem tutarı (tamamlanan rezervasyonlara göre)
            summary.AverageTicketSize = summary.CompletedReservations > 0
                ? summary.ServiceIncome / summary.CompletedReservations
                : 0;
        }

    }
}
