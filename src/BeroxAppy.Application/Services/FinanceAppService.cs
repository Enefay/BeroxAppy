using BeroxAppy.Employees;
using BeroxAppy.Enums;
using BeroxAppy.Finance;
using BeroxAppy.Finances;
using BeroxAppy.Finances.FinanceAppDtos;
using BeroxAppy.Finances.SalaryDtos;
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


            // MAAŞ HESAPLAMALARI
            var employeeSalaries = await GetEmployeeSalariesAsync();
            var dueSalaries = employeeSalaries.Where(x => x.IsDue).ToList();
            var urgentSalaries = dueSalaries.Where(x => x.DaysOverdue >= 7)
                .Select(x => new UrgentSalaryDto
                {
                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.EmployeeName,
                    Amount = x.CalculatedAmount,
                    DaysOverdue = x.DaysOverdue,
                    Period = x.SalaryPeriod
                }).ToList();

            var dueCountByPeriod = dueSalaries.GroupBy(x => x.SalaryPeriod)
                .ToDictionary(g => g.Key, g => g.Count());

            var thisMonthStart = new DateTime(targetDate.Year, targetDate.Month, 1);
            var thisMonthSalaryPayments = await _employeePaymentRepository.GetListAsync(x =>
                x.PaymentDate >= thisMonthStart &&
                x.PaymentType == PaymentType.Salary);

            return new DashboardDto
            {
                Date = targetDate,
                TodayIncome = todayIncome,
                TodayExpenses = todayExpenses,
                TodayProfit = todayIncome - todayExpenses,
                PendingCommissions = totalPendingCommissions,
                EmployeeCommissions = await GetEmployeeCommissionsAsync(),
                //maas
                PendingSalaries = dueSalaries.Sum(x => x.CalculatedAmount),
                DueSalaryCount = dueSalaries.Count,
                EmployeeSalaries = employeeSalaries,
                SalarySummary = new SalaryDashboardSummaryDto
                {
                    DueCountByPeriod = dueCountByPeriod,
                    TotalDueAmount = dueSalaries.Sum(x => x.CalculatedAmount),
                    TotalDueEmployees = dueSalaries.Count,
                    ThisMonthPaidSalaries = thisMonthSalaryPayments.Sum(x => x.SalaryAmount),
                    UrgentSalaries = urgentSalaries
                }
            };
        }

        // Çalışan komisyonları listesi
        public async Task<List<EmployeeCommissionSummaryDto>> GetEmployeeCommissionsAsync()
        {
            var employees = await _employeeRepository.GetListAsync(x => x.IsActive);
            var result = new List<EmployeeCommissionSummaryDto>();

            foreach (var employee in employees)
            {
                var lastPaymentList = await _employeePaymentRepository
                .GetListAsync(x => x.EmployeeId == employee.Id && x.PaymentType == PaymentType.Commission);

                var lastPayment = lastPaymentList
                    .OrderByDescending(x => x.PaymentDate)
                    .LastOrDefault();

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
            endDate = endDate.Date.AddDays(1).AddTicks(-1);
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


     


            var isPartial = amount < employee.CurrentPeriodCommission + amount; // Ödenen, toplamdan azsa kısmi

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
                Note = string.IsNullOrWhiteSpace(note)
                ? (isPartial
                    ? $"Kısmi komisyon ödemesi - {DateTime.Now:dd.MM.yyyy}"
                    : $"Komisyon ödemesi - {DateTime.Now:dd.MM.yyyy}")
                : note,
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


        public async Task<List<EmployeeSalarySummaryDto>> GetEmployeeSalariesAsync()
        {
            var employees = await _employeeRepository.GetListAsync(x => x.IsActive);
            var result = new List<EmployeeSalarySummaryDto>();

            foreach (var employee in employees)
            {
                //maaş almayanlar
                if (employee.FixedSalary <= 0)
                {
                    continue;
                }

                var lastSalaryPayment = await _employeePaymentRepository
                    .GetListAsync(x => x.EmployeeId == employee.Id && x.PaymentType == PaymentType.Salary);

                var lastPayment = lastSalaryPayment.OrderByDescending(x => x.PaymentDate).FirstOrDefault();

                // Sonraki ödeme tarihini hesapla
                var nextPaymentDue = CalculateNextPaymentDate(employee, lastPayment?.PaymentDate);
                var isDue = DateTime.Now.Date >= nextPaymentDue.Date;
                var daysOverdue = isDue ? (DateTime.Now.Date - nextPaymentDue.Date).Days : 0;

                // Dönem tutarını hesapla
                var calculatedAmount = CalculatePeriodAmount(employee.FixedSalary, employee.SalaryPeriod);

                result.Add(new EmployeeSalarySummaryDto
                {
                    EmployeeId = employee.Id,
                    EmployeeName = $"{employee.FirstName} {employee.LastName}",
                    FixedSalary = employee.FixedSalary,
                    SalaryPeriod = employee.SalaryPeriod,
                    PaymentDay = employee.PaymentDay,
                    LastSalaryPaymentDate = lastPayment?.PaymentDate,
                    NextPaymentDue = nextPaymentDue,
                    IsDue = isDue,
                    CanPay = isDue || AllowEarlyPayment(employee, lastPayment?.PaymentDate), // Erken ödeme izni
                    DaysOverdue = daysOverdue,
                    PreferredPaymentMethod = employee.PreferredPaymentMethod,
                    SalaryPeriodDisplay = GetSalaryPeriodDisplay(employee.SalaryPeriod),
                    CalculatedAmount = calculatedAmount
                });
            }

            return result.OrderByDescending(x => x.IsDue)
                         .ThenByDescending(x => x.DaysOverdue)
                         .ThenBy(x => x.NextPaymentDue)
                         .ToList();
        }

        // Maaş öde
        public async Task PayEmployeeSalaryAsync(Guid employeeId, decimal amount, PaymentMethod paymentMethod, string? note = null)
        {
            var employee = await _employeeRepository.GetAsync(employeeId);

            if (amount <= 0)
            {
                throw new UserFriendlyException("Ödeme tutarı 0'dan büyük olmalı!");
            }

            // Maksimum ödeme tutarını kontrol et (dönem tutarından fazla olmasın)
            var maxAmount = CalculatePeriodAmount(employee.FixedSalary, employee.SalaryPeriod);
            if (amount > maxAmount)
            {
                throw new UserFriendlyException($"Ödeme tutarı dönem tutarından (₺{maxAmount:N2}) fazla olamaz!");
            }

            // Dönem bilgilerini hesapla
            var lastPayment = await _employeePaymentRepository
                .GetListAsync(x => x.EmployeeId == employeeId && x.PaymentType == PaymentType.Salary);

            var lastSalaryPayment = lastPayment.OrderByDescending(x => x.PaymentDate).FirstOrDefault();

            var periodStart = lastSalaryPayment?.PeriodEnd.AddDays(1) ??
                             CalculatePeriodStart(employee.SalaryPeriod, employee.PaymentDay);
            var periodEnd = CalculatePeriodEnd(periodStart, employee.SalaryPeriod);

            var isPartial = amount < employee.CurrentPeriodCommission + amount; // Ödenen, toplamdan azsa kısmi

            // Ödeme kaydı oluştur
            var payment = new EmployeePayment
            {
                EmployeeId = employeeId,
                SalaryAmount = amount,
                CommissionAmount = 0,
                BonusAmount = 0,
                TotalAmount = amount,
                PaymentDate = DateTime.Now,
                PaymentMethod = paymentMethod,
                Note = string.IsNullOrWhiteSpace(note)
                 ? (isPartial
                     ? $"Kısmi maaş ödemesi - {DateTime.Now:dd.MM.yyyy}"
                     : $"Maaş ödemesi - {DateTime.Now:dd.MM.yyyy}")
                 : note,
                PeriodEnd = periodEnd,
                PaymentType = PaymentType.Salary
            };

            await _employeePaymentRepository.InsertAsync(payment);

            // Çalışanın son maaş ödeme tarihini güncelle
            employee.LastSalaryPaymentDate = DateTime.Now;
            await _employeeRepository.UpdateAsync(employee);
        }

        // Çalışan maaş performansı
        public async Task<EmployeeSalaryPerformanceDto> GetEmployeeSalaryPerformanceAsync(Guid employeeId, DateTime startDate, DateTime endDate)
        {
            var employee = await _employeeRepository.GetAsync(employeeId);

            var payments = await _employeePaymentRepository.GetListAsync(x =>
                x.EmployeeId == employeeId &&
                x.PaymentDate >= startDate &&
                x.PaymentDate <= endDate);

            var salaryPayments = payments.Where(x => x.PaymentType == PaymentType.Salary).ToList();
            var commissionPayments = payments.Where(x => x.PaymentType == PaymentType.Commission).ToList();
            var bonusPayments = payments.Where(x => x.PaymentType == PaymentType.Bonus).ToList();

            var paymentHistory = salaryPayments.Select(x => new SalaryPaymentDetailDto
            {
                Amount = x.SalaryAmount,
                PaymentDate = x.PaymentDate,
                PaymentMethod = x.PaymentMethod,
                PaymentMethodDisplay = GetPaymentMethodDisplay(x.PaymentMethod),
                Note = x.Note,
                PeriodStart = x.PeriodStart,
                PeriodEnd = x.PeriodEnd
            }).OrderByDescending(x => x.PaymentDate).ToList();

            return new EmployeeSalaryPerformanceDto
            {
                EmployeeId = employeeId,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                PeriodStart = startDate,
                PeriodEnd = endDate,
                TotalSalaryPaid = salaryPayments.Sum(x => x.SalaryAmount),
                TotalCommissionPaid = commissionPayments.Sum(x => x.CommissionAmount),
                TotalBonusPaid = bonusPayments.Sum(x => x.BonusAmount),
                GrandTotal = payments.Sum(x => x.TotalAmount),
                PaymentCount = salaryPayments.Count,
                PaymentHistory = paymentHistory
            };
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


        //maas

        // Yardımcı metodlar
        private DateTime CalculateNextPaymentDate(Employee employee, DateTime? lastPaymentDate)
        {
            var baseDate = lastPaymentDate ?? DateTime.Now.AddDays(-30); // İlk ödeme için geçmiş tarih

            return employee.SalaryPeriod switch
            {
                SalaryPeriodType.Daily => baseDate.AddDays(1),
                SalaryPeriodType.Weekly => baseDate.AddDays(7),
                SalaryPeriodType.BiWeekly => baseDate.AddDays(14),
                SalaryPeriodType.Monthly => baseDate.AddMonths(1),
                _ => baseDate.AddMonths(1)
            };
        }

        private decimal CalculatePeriodAmount(decimal fixedSalary, SalaryPeriodType period)
        {
            return period switch
            {
                SalaryPeriodType.Daily => fixedSalary / 30, // Aylık maaşı 30'a böl
                SalaryPeriodType.Weekly => fixedSalary / 4, // Aylık maaşı 4'e böl
                SalaryPeriodType.BiWeekly => fixedSalary / 2, // Aylık maaşı 2'ye böl
                SalaryPeriodType.Monthly => fixedSalary, // Tam maaş
                _ => fixedSalary
            };
        }

        private bool AllowEarlyPayment(Employee employee, DateTime? lastPaymentDate)
        {
            // Erken ödeme politikası - son ödemeden en az 1 gün geçmişse ödeme yapılabilir
            if (lastPaymentDate == null) return true;

            return (DateTime.Now.Date - lastPaymentDate.Value.Date).Days >= 1;
        }

        private DateTime CalculatePeriodStart(SalaryPeriodType period, int paymentDay)
        {
            var now = DateTime.Now;

            return period switch
            {
                SalaryPeriodType.Daily => now.Date,
                SalaryPeriodType.Weekly => now.Date.AddDays(-(int)now.DayOfWeek),
                SalaryPeriodType.BiWeekly => now.Date.AddDays(-14),
                SalaryPeriodType.Monthly => new DateTime(now.Year, now.Month, 1),
                _ => new DateTime(now.Year, now.Month, 1)
            };
        }

        private DateTime CalculatePeriodEnd(DateTime periodStart, SalaryPeriodType period)
        {
            return period switch
            {
                SalaryPeriodType.Daily => periodStart,
                SalaryPeriodType.Weekly => periodStart.AddDays(6),
                SalaryPeriodType.BiWeekly => periodStart.AddDays(13),
                SalaryPeriodType.Monthly => periodStart.AddMonths(1).AddDays(-1),
                _ => periodStart.AddMonths(1).AddDays(-1)
            };
        }

        private string GetSalaryPeriodDisplay(SalaryPeriodType period)
        {
            return period switch
            {
                SalaryPeriodType.Daily => "Günlük",
                SalaryPeriodType.Weekly => "Haftalık",
                SalaryPeriodType.BiWeekly => "2 Haftalık",
                SalaryPeriodType.Monthly => "Aylık",
                _ => "Bilinmiyor"
            };
        }

        private string GetPaymentMethodDisplay(PaymentMethod method)
        {
            return method switch
            {
                PaymentMethod.Cash => "Nakit",
                PaymentMethod.CreditCard => "Kredi Kartı",
                PaymentMethod.DebitCard => "Banka Kartı",
                PaymentMethod.BankTransfer => "Havale/EFT",
                PaymentMethod.Check => "Çek",
                PaymentMethod.Other => "Diğer",
                _ => "Bilinmiyor"
            };
        }

    }
}
