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

        public async Task<IActionResult> OnPostCloseCashAsync([FromForm] decimal actualClosingBalance, [FromForm] string note = null)
        {
            try
            {
                var cashRegister = await GetCashRegisterForDateAsync(SelectedDate);

                if (cashRegister.IsClosed)
                {
                    return new JsonResult(new { success = false, message = "Bu kasa zaten kapatýlmýþ!" });
                }

                var result = await _paymentAppService.CloseCashRegisterAsync(
                    cashRegister.Id,
                    actualClosingBalance,
                    note ?? $"Kasa kapanýþý - {DateTime.Now:dd.MM.yyyy HH:mm}"
                );

                return new JsonResult(new
                {
                    success = true,
                    message = "Kasa baþarýyla kapatýldý.",
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
                // Sadece bugünkü kasayý tekrar açabilirsin
                if (SelectedDate.Date != DateTime.Now.Date)
                {
                    return new JsonResult(new { success = false, message = "Sadece bugünkü kasa tekrar açýlabilir!" });
                }

                var cashRegister = await GetCashRegisterForDateAsync(SelectedDate);

                if (!cashRegister.IsClosed)
                {
                    return new JsonResult(new { success = false, message = "Kasa zaten açýk!" });
                }

                // ReopenCashRegisterAsync metodunu kullan
                var result = await _paymentAppService.ReopenCashRegisterAsync(cashRegister.Id);

                return new JsonResult(new
                {
                    success = true,
                    message = "Kasa tekrar açýldý. Dikkatli kullanýn!",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostCashTransactionAsync(
            [FromForm] string transactionType,
            [FromForm] decimal amount,
            [FromForm] string description,
            [FromForm] string note = null)
        {
            try
            {
                // Debug için log
                Console.WriteLine($"CashTransaction çaðrýldý: Type={transactionType}, Amount={amount}, Description={description}, Note={note}");

                // Validation
                if (string.IsNullOrWhiteSpace(transactionType))
                {
                    return new JsonResult(new { success = false, message = "Ýþlem tipi belirtilmedi!" });
                }

                if (amount <= 0)
                {
                    return new JsonResult(new { success = false, message = "Tutar sýfýrdan büyük olmalýdýr!" });
                }

                if (string.IsNullOrWhiteSpace(description))
                {
                    return new JsonResult(new { success = false, message = "Açýklama giriniz!" });
                }

                var cashRegister = await GetCashRegisterForDateAsync(SelectedDate);

                if (cashRegister.IsClosed)
                {
                    return new JsonResult(new { success = false, message = "Kapalý kasaya iþlem yapýlamaz!" });
                }

                var isRefund = transactionType.ToLower() == "out";
                var fullDescription = string.IsNullOrWhiteSpace(note)
                    ? description.Trim()
                    : $"{description.Trim()} - {note.Trim()}";

                var paymentDto = new CreatePaymentDto
                {
                    CustomerId = null, // Sistem müþterisi otomatik atanacak
                    ReservationId = null, // Rezervasyon dýþý iþlem
                    Amount = amount,
                    PaymentMethod = PaymentMethod.Cash,
                    PaymentDate = DateTime.Now,
                    Description = fullDescription,
                    IsRefund = isRefund
                };

                var result = await _paymentAppService.CreateAsync(paymentDto);

                return new JsonResult(new
                {
                    success = true,
                    message = $"Nakit {(isRefund ? "çýkýþ" : "giriþ")} iþlemi baþarýyla kaydedildi.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CashTransaction hatasý: {ex.Message}");
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
            // Seçili tarih için kasa bilgilerini getir
            CashRegister = await GetCashRegisterForDateAsync(SelectedDate);

            // Teorik bakiyeyi hesapla
            TheoreticalBalance = CashRegister.OpeningBalance + CashRegister.TotalCashIn - CashRegister.TotalCashOut;

            // Farký hesapla
            Difference = CashRegister.ClosingBalance - TheoreticalBalance;

            // O gün yapýlan nakit iþlemleri getir
            await LoadCashPaymentsAsync();
        }

        private async Task<CashRegisterDto> GetCashRegisterForDateAsync(DateTime date)
        {
            if (date.Date == DateTime.Now.Date)
            {
                // Bugün ise GetTodaysCashRegisterAsync kullan
                return await _paymentAppService.GetTodaysCashRegisterAsync();
            }
            else
            {
                // Geçmiþ tarih için günlük rapor kullan
                var report = await _paymentAppService.GetDailyCashReportAsync(date);

                if (!report.HasData)
                {
                    // O gün kasa açýlmamýþsa boþ kasa döndür
                    return new CashRegisterDto
                    {
                        Id = Guid.Empty,
                        Date = date.Date,
                        OpeningBalance = 0,
                        ClosingBalance = 0,
                        TotalCashIn = 0,
                        TotalCashOut = 0,
                        IsClosed = true,
                        Note = "Bu gün kasa açýlmamýþ"
                    };
                }

                // Rapor verisinden CashRegisterDto oluþtur
                return new CashRegisterDto
                {
                    Id = Guid.Empty, // Geçmiþ veriler için ID yok
                    Date = date.Date,
                    OpeningBalance = report.OpeningBalance,
                    ClosingBalance = report.ActualClosing,
                    TotalCashIn = report.TotalCashIn,
                    TotalCashOut = report.TotalCashOut,
                    IsClosed = report.IsClosed,
                    Note = "Geçmiþ tarih verisi"
                };
            }
        }

        private async Task LoadCashPaymentsAsync()
        {
            try
            {
                // Seçili tarihte yapýlan tüm nakit ödemeleri getir
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
                // Hata durumunda boþ liste döndür
                CashPayments = new List<PaymentDto>();
            }
        }
    }
}