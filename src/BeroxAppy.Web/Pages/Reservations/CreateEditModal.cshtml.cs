using BeroxAppy.Customers;
using BeroxAppy.Employees;
using BeroxAppy.Reservations;
using BeroxAppy.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;

namespace BeroxAppy.Web.Pages.Reservations
{
    public class CreateEditModalModel : BeroxAppyPageModel
    {
        private readonly IReservationAppService _reservationAppService;
        private readonly ICustomerAppService _customerAppService;
        private readonly IServiceAppService _serviceAppService;
        private readonly IEmployeeAppService _employeeAppService;

        [BindProperty]
        public ReservationViewModel Reservation { get; set; }

        public List<SelectListItem> Customers { get; set; }
        public List<SelectListItem> Services { get; set; }
        public List<SelectListItem> Employees { get; set; }

        public CreateEditModalModel(
            IReservationAppService reservationAppService,
            ICustomerAppService customerAppService,
            IServiceAppService serviceAppService,
            IEmployeeAppService employeeAppService)
        {
            _reservationAppService = reservationAppService;
            _customerAppService = customerAppService;
            _serviceAppService = serviceAppService;
            _employeeAppService = employeeAppService;
        }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id.HasValue)
            {
                var dto = await _reservationAppService.GetAsync(id.Value);
                Reservation = ObjectMapper.Map<ReservationDto, ReservationViewModel>(dto);
            }
            else
            {
                Reservation = new ReservationViewModel
                {
                    ReservationDate = DateTime.Now.Date,
                    ReservationDetails = new List<ReservationDetailViewModel>()
                };
            }

            await LoadLookups();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {

            CleanupModelStateErrors();


            // Manuel validasyon kontrolleri
            if (Reservation.CustomerId == Guid.Empty)
            {
                ModelState.AddModelError("Reservation.CustomerId", "Müşteri seçimi zorunludur.");
            }

            // Rezervasyon tarihi geçmiş tarih kontrolü
            if (Reservation.ReservationDate.Date < DateTime.Now.Date)
            {
                ModelState.AddModelError("Reservation.ReservationDate", "Rezervasyon tarihi bugünden önce olamaz.");
            }

            if (Reservation.ReservationDetails == null || !Reservation.ReservationDetails.Any())
            {
                ModelState.AddModelError("Reservation.ReservationDetails", "En az bir hizmet eklemelisiniz.");

            }
            else
            {
                // Her hizmet detayı için kontrol
                for (int i = 0; i < Reservation.ReservationDetails.Count; i++)
                {
                    var detail = Reservation.ReservationDetails[i];


                    if (detail.ServiceId == Guid.Empty && detail.EmployeeId == Guid.Empty && detail.StartTime == TimeSpan.Zero)
                    {
                        ModelState.AddModelError($"{i + 1}", "Hizmetin tüm bilgilerini doldurmanız gerekmektedir.");
                        continue;
                    }


                    if (detail.ServiceId == Guid.Empty)
                    {
                        ModelState.AddModelError($"Reservation.ReservationDetails[{i}].ServiceId", $"{i + 1}. hizmet için servis seçimi zorunludur.");
                    }

                    if (detail.EmployeeId == Guid.Empty)
                    {
                        ModelState.AddModelError($"Reservation.ReservationDetails[{i}].EmployeeId", $"{i + 1}. hizmet için çalışan seçimi zorunludur.");
                    }

                    if (detail.StartTime == TimeSpan.Zero)
                    {
                        ModelState.AddModelError($"Reservation.ReservationDetails[{i}].StartTime", $"{i + 1}. hizmet için saat seçimi zorunludur.");
                    }

                    // Fiyat kontrolü - eğer CustomPrice null ise service price'ı kullan
                    if (!detail.CustomPrice.HasValue || detail.CustomPrice <= 0)
                    {
                        if (detail.ServiceId != Guid.Empty)
                        {
                            try
                            {
                                var service = await _serviceAppService.GetAsync(detail.ServiceId);
                                if (service.Price <= 0)
                                {
                                    ModelState.AddModelError($"Reservation.ReservationDetails[{i}].CustomPrice", $"{i + 1}. hizmet için geçerli bir fiyat giriniz.");
                                }
                                else
                                {
                                    // Service price'ı custom price olarak ata
                                    detail.CustomPrice = service.Price;
                                }
                            }
                            catch
                            {
                                ModelState.AddModelError($"Reservation.ReservationDetails[{i}].CustomPrice", $"{i + 1}. hizmet için fiyat bilgisi alınamadı.");
                            }
                        }
                        else
                        {
                            ModelState.AddModelError($"Reservation.ReservationDetails[{i}].CustomPrice", $"{i + 1}. hizmet için geçerli bir fiyat giriniz.");
                        }
                    }

                    // Çakışma kontrolü - aynı çalışanın aynı saatte başka rezervasyonu var mı?
                    //if (detail.EmployeeId != Guid.Empty && detail.StartTime != TimeSpan.Zero)
                    //{
                    //    try
                    //    {
                    //        var conflicts = await _reservationAppService.CheckConflictsAsync(
                    //            detail.EmployeeId,
                    //            Reservation.ReservationDate,
                    //            detail.StartTime,
                    //            Reservation.Id == Guid.Empty ? null : Reservation.Id
                    //        );

                    //        if (conflicts.Any())
                    //        {
                    //            ModelState.AddModelError($"Reservation.ReservationDetails[{i}].StartTime",
                    //                $"{i + 1}. hizmet için seçilen saat dolu. Lütfen başka bir saat seçiniz.");
                    //        }
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        Logger.LogWarning(ex, "Çakışma kontrolü yapılamadı");
                    //    }
                    //}
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadLookups();

                var errors = ModelState
                           .Where(x => x.Value.Errors.Count > 0)
                           .ToDictionary(
                               kvp => kvp.Key,
                               kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                           );

                // Tüm hata mesajlarını birleştir
                var allErrorMessages = errors.Values
                    .SelectMany(errorArray => errorArray)
                    .ToList();

                return new JsonResult(new
                {
                    success = false,
                    errors = errors,
                    message = string.Join(" ", allErrorMessages)
                });
            }

            try
            {
                if (Reservation.Id == Guid.Empty)
                {
                    var createDto = ObjectMapper.Map<ReservationViewModel, CreateReservationDto>(Reservation);
                    await _reservationAppService.CreateAsync(createDto);
                }
                else
                {
                    var updateDto = ObjectMapper.Map<ReservationViewModel, UpdateReservationDto>(Reservation);
                    await _reservationAppService.UpdateAsync(Reservation.Id, updateDto);
                }

                return new JsonResult(new { success = true });
            }
            catch (BusinessException ex)
            {
                Logger.LogWarning(ex, "İş kuralı hatası: {Message}", ex.Message);
                return new JsonResult(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Rezervasyon kaydedilirken hata oluştu");
                return new JsonResult(new
                {
                    success = false,
                    message = "Rezervasyon kaydedilirken bir hata oluştu. Lütfen tekrar deneyin."
                });
            }
        }
        // AJAX Handler Methods
        public async Task<IActionResult> OnGetEmployeesByServiceAsync(Guid serviceId)
        {
            var employees = await _employeeAppService.GetEmployeesByServiceAsync(serviceId);
            return new JsonResult(employees.Items);
        }

        // Servis fiyatını getir
        public async Task<IActionResult> OnGetServicePriceAsync(Guid serviceId)
        {
            try
            {
                var service = await _serviceAppService.GetAsync(serviceId);
                return new JsonResult(new { price = service.Price });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Servis fiyatı alınırken hata oluştu: {ServiceId}", serviceId);
                return new JsonResult(new { price = 0 });
            }
        }

        public async Task<IActionResult> OnGetAvailableSlotsAsync(Guid employeeId, Guid serviceId, DateTime date)
        {
            var slots = await _reservationAppService.GetAvailableSlotsAsync(employeeId, serviceId, date);
            return new JsonResult(new { availableSlots = slots });
        }
        //musteri arama
        public async Task<IActionResult> OnGetSearchCustomersAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 1)
            {
                return new JsonResult(new List<object>());
            }

            try
            {
                // Müşteri arama - isim ve telefona göre arama yap
                var customers = await _customerAppService.SearchCustomersAsync(query, 5);

                var result = customers.Select(c => new
                {
                    id = c.Id,
                    fullName = c.FullName,
                    phone = c.Phone,
                    email = c.Email
                });

                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Müşteri arama sırasında hata oluştu: {Query}", query);
                return new JsonResult(new List<object>());
            }
        }
        //hizmet arama
        public async Task<IActionResult> OnGetSearchServicesAsync(string? query)
        {
            try
            {
                var services = await _serviceAppService.SearchServicesAsync(query, 5);

                var result = services.Select(s => new
                {
                    id = s.Id,
                    title = s.Title,
                    price = s.Price,
                    duration = s.DurationDisplay
                });

                return new JsonResult(result);

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Hizmet arama sırasında hata oluştu: {Query}", query);
                return new JsonResult(new List<object>());
            }
        }

        //calisan arama
        public async Task<IActionResult> OnGetSearchEmployeesByServiceAsync(Guid serviceId, string? query)
        {
            try
            {
                var employees = await _employeeAppService.GetEmployeesByServiceAsync(serviceId, query, 5);

                var result = employees.Select(e => new
                {
                    id = e.Id,
                    fullName = e.FullName
                });

                return new JsonResult(result);

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Çalışan arama sırasında hata oluştu: {Query}", query);
                return new JsonResult(new List<object>());
            }
        }



        public async Task<IActionResult> OnGetCustomerInfoAsync(Guid customerId)
        {
            try
            {
                var customer = await _customerAppService.GetAsync(customerId);
                return new JsonResult(new
                {
                    id = customer.Id,
                    fullName = customer.FullName,
                    phone = customer.Phone,
                    email = customer.Email
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Müşteri bilgisi alınırken hata oluştu: {CustomerId}", customerId);
                return new JsonResult(null);
            }
        }

        private async Task LoadLookups()
        {
            // Müşteri listesi
            var customerList = await _customerAppService.GetActiveListAsync();
            Customers = customerList.Items.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.FullName} - {x.Phone}"
            }).ToList();

            // Hizmet listesi
            var serviceList = await _serviceAppService.GetActiveListAsync();
            Services = serviceList.Items.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.Title} ({x.DurationDisplay} - ₺{x.Price:F2})"
            }).ToList();

            // Çalışan listesi
            var employeeList = await _employeeAppService.GetActiveListAsync();
            Employees = employeeList.Items.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.FullName
            }).ToList();
        }



        private void CleanupModelStateErrors()
        {
            var keysToRemove = new List<string>();

            foreach (var modelState in ModelState)
            {
                var errorsToRemove = new List<ModelError>();

                foreach (var error in modelState.Value.Errors)
                {
                    // "The value '' is invalid." gibi mesajları temizle
                    if (error.ErrorMessage.Contains("The value") && error.ErrorMessage.Contains("is invalid"))
                    {
                        errorsToRemove.Add(error);
                    }
                    // Boş string mesajları da temizle
                    else if (string.IsNullOrWhiteSpace(error.ErrorMessage))
                    {
                        errorsToRemove.Add(error);
                    }
                }

                foreach (var errorToRemove in errorsToRemove)
                {
                    modelState.Value.Errors.Remove(errorToRemove);
                }

                // Eğer hiç hata kalmadıysa key'i de temizle
                if (!modelState.Value.Errors.Any())
                {
                    keysToRemove.Add(modelState.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }
        }


        public class ReservationViewModel
        {
            public Guid Id { get; set; }

            [Required(ErrorMessage = "Müşteri seçimi zorunludur.")]
            [Display(Name = "Müşteri")]
            public Guid CustomerId { get; set; }

            [Display(Name = "Not")]
            [StringLength(500, ErrorMessage = "Not en fazla 500 karakter olabilir.")]
            public string? Note { get; set; }

            [Required(ErrorMessage = "Rezervasyon tarihi zorunludur.")]
            [Display(Name = "Rezervasyon Tarihi")]
            [DataType(DataType.Date)]
            public DateTime ReservationDate { get; set; }

            [Display(Name = "İndirim Tutarı")]
            [Range(0, double.MaxValue, ErrorMessage = "İndirim tutarı negatif olamaz.")]
            public decimal? DiscountAmount { get; set; }

            [Display(Name = "Ekstra Tutar")]
            [Range(0, double.MaxValue, ErrorMessage = "Ekstra tutar negatif olamaz.")]
            public decimal? ExtraAmount { get; set; }

            [Display(Name = "Adisyon / Rezervasyonsuz Müşteri")]
            public bool IsWalkIn { get; set; }

            [Required(ErrorMessage = "En az bir hizmet eklemelisiniz.")]
            [MinLength(1, ErrorMessage = "En az bir hizmet eklemelisiniz.")]
            public List<ReservationDetailViewModel> ReservationDetails { get; set; } = new();
        }

        public class ReservationDetailViewModel
        {
            [Required(ErrorMessage = "Hizmet seçimi zorunludur.")]
            public Guid ServiceId { get; set; }

            [Required(ErrorMessage = "Çalışan seçimi zorunludur.")]
            public Guid EmployeeId { get; set; }

            [Required(ErrorMessage = "Saat seçimi zorunludur.")]
            public TimeSpan StartTime { get; set; }

            [Range(0.01, double.MaxValue, ErrorMessage = "Geçerli bir fiyat giriniz.")]
            public decimal? CustomPrice { get; set; }

            [StringLength(200, ErrorMessage = "Hizmet notu en fazla 200 karakter olabilir.")]
            public string? Note { get; set; }
        }
    }
}