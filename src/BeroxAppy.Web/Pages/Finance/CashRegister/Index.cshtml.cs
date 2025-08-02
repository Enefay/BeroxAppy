using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BeroxAppy.Services;
using BeroxAppy.Finances;
using BeroxAppy.Enums;

namespace BeroxAppy.Web.Pages.Finance.CashRegister
{
    public class IndexModel : BeroxAppyPageModel
    {
        private readonly IPaymentAppService _paymentAppService;

        public IndexModel(IPaymentAppService paymentAppService)
        {
            _paymentAppService = paymentAppService;
        }

        [BindProperty]
        public DateTime SelectedDate { get; set; } = DateTime.Now.Date;

        public CashRegisterDto CashRegister { get; set; }
        public List<PaymentDto> CashPayments { get; set; } = new List<PaymentDto>();
        public decimal TheoreticalBalance { get; set; }
        public decimal Difference { get; set; }

        public async Task OnGetAsync(DateTime? date = null)
        {
            SelectedDate = date?.Date ?? DateTime.Now.Date;
            await LoadCashRegisterDataAsync();
        }

        public async Task<IActionResult> OnPostCloseCashAsync(decimal actualClosingBalance, string note = null)
        {
            try
            {
                var cashRegister = await GetCashRegisterForDateAsync(SelectedDate);

                if (cashRegister.IsClosed)
                {
                    return new JsonResult(new { success = false, message = "Bu kasa zaten kapat�lm��!" });
                }

                var result = await _paymentAppService.CloseCashRegisterAsync(
                    cashRegister.Id,
                    actualClosingBalance,
                    note ?? $"Kasa kapan��� - {DateTime.Now:dd.MM.yyyy HH:mm}"
                );

                return new JsonResult(new
                {
                    success = true,
                    message = "Kasa ba�ar�yla kapat�ld�.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostReopenCashAsync()
        {
            try
            {
                // Sadece bug�nk� kasay� tekrar a�abilirsin
                if (SelectedDate.Date != DateTime.Now.Date)
                {
                    return new JsonResult(new { success = false, message = "Sadece bug�nk� kasa tekrar a��labilir!" });
                }

                var cashRegister = await GetCashRegisterForDateAsync(SelectedDate);

                if (!cashRegister.IsClosed)
                {
                    return new JsonResult(new { success = false, message = "Kasa zaten a��k!" });
                }

                // ReopenCashRegisterAsync metodunu kullan
                var result = await _paymentAppService.ReopenCashRegisterAsync(cashRegister.Id);

                return new JsonResult(new
                {
                    success = true,
                    message = "Kasa tekrar a��ld�. Dikkatli kullan�n!",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostCashTransactionAsync(
            string transactionType,
            decimal amount,
            string description,
            string note = null)
        {
            try
            {
                if (amount <= 0)
                {
                    return new JsonResult(new { success = false, message = "Tutar s�f�rdan b�y�k olmal�d�r!" });
                }

                var cashRegister = await GetCashRegisterForDateAsync(SelectedDate);

                if (cashRegister.IsClosed)
                {
                    return new JsonResult(new { success = false, message = "Kapal� kasaya i�lem yap�lamaz!" });
                }

                var isRefund = transactionType.ToLower() == "out";
                var fullDescription = string.IsNullOrWhiteSpace(note)
                    ? description
                    : $"{description} - {note}";

                var paymentDto = new CreatePaymentDto
                {
                    CustomerId = null,
                    ReservationId = null, // Rezervasyon d��� i�lem
                    Amount = amount,
                    PaymentMethod = PaymentMethod.Cash,
                    PaymentDate = DateTime.Now,
                    Description = fullDescription,
                    IsRefund = isRefund
                };

                await _paymentAppService.CreateAsync(paymentDto);

                return new JsonResult(new
                {
                    success = true,
                    message = $"Nakit {(isRefund ? "��k��" : "giri�")} i�lemi kaydedildi."
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnGetCashReportAsync(DateTime? date = null)
        {
            try
            {
                var targetDate = date?.Date ?? SelectedDate;
                var report = await _paymentAppService.GetDailyCashReportAsync(targetDate);

                return new JsonResult(new { success = true, data = report });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        private async Task LoadCashRegisterDataAsync()
        {
            // Se�ili tarih i�in kasa bilgilerini getir
            CashRegister = await GetCashRegisterForDateAsync(SelectedDate);

            // Teorik bakiyeyi hesapla
            TheoreticalBalance = CashRegister.OpeningBalance + CashRegister.TotalCashIn - CashRegister.TotalCashOut;

            // Fark� hesapla
            Difference = CashRegister.ClosingBalance - TheoreticalBalance;

            // O g�n yap�lan nakit i�lemleri getir
            await LoadCashPaymentsAsync();
        }

        private async Task<CashRegisterDto> GetCashRegisterForDateAsync(DateTime date)
        {
            if (date.Date == DateTime.Now.Date)
            {
                // Bug�n ise GetTodaysCashRegisterAsync kullan
                return await _paymentAppService.GetTodaysCashRegisterAsync();
            }
            else
            {
                // Ge�mi� tarih i�in g�nl�k rapor kullan
                var report = await _paymentAppService.GetDailyCashReportAsync(date);

                if (!report.HasData)
                {
                    // O g�n kasa a��lmam��sa bo� kasa d�nd�r
                    return new CashRegisterDto
                    {
                        Id = Guid.Empty,
                        Date = date.Date,
                        OpeningBalance = 0,
                        ClosingBalance = 0,
                        TotalCashIn = 0,
                        TotalCashOut = 0,
                        IsClosed = true,
                        Note = "Bu g�n kasa a��lmam��"
                    };
                }

                // Rapor verisinden CashRegisterDto olu�tur
                return new CashRegisterDto
                {
                    Id = Guid.Empty, // Ge�mi� veriler i�in ID yok
                    Date = date.Date,
                    OpeningBalance = report.OpeningBalance,
                    ClosingBalance = report.ActualClosing,
                    TotalCashIn = report.TotalCashIn,
                    TotalCashOut = report.TotalCashOut,
                    IsClosed = report.IsClosed,
                    Note = "Ge�mi� tarih verisi"
                };
            }
        }

        private async Task LoadCashPaymentsAsync()
        {
            try
            {
                // Se�ili tarihte yap�lan t�m nakit �demeleri getir
                var paymentsInput = new GetPaymentsInput
                {
                    PaymentMethod = PaymentMethod.Cash,
                    StartDate = SelectedDate.Date,
                    EndDate = SelectedDate.Date.AddDays(1).AddSeconds(-1),
                    MaxResultCount = 1000,
                    SkipCount = 0
                };

                var paymentsResult = await _paymentAppService.GetListAsync(paymentsInput);
                CashPayments = paymentsResult.Items.ToList();
            }
            catch (Exception)
            {
                // Hata durumunda bo� liste d�nd�r
                CashPayments = new List<PaymentDto>();
            }
        }
     
    }
}