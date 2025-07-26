using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BeroxAppy.Reservations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using BeroxAppy.Customers;
using BeroxAppy.Services;
using BeroxAppy.Employees;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;

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
            if (!ModelState.IsValid)
            {
                await LoadLookups();
                var errors = ModelState
                           .Where(x => x.Value.Errors.Count > 0)
                           .ToDictionary(
                               kvp => kvp.Key,
                               kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                           );

                return new JsonResult(new
                {
                    success = false,
                    errors = errors,
                    message = "Lütfen tüm gerekli alanları doldurun."
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
            catch (Exception ex)
            {
                Logger.LogError(ex, "Rezervasyon kaydedilirken hata oluştu");
                return new JsonResult(new
                {
                    success = false,
                    message = "Rezervasyon kaydedilirken bir hata oluştu: " + ex.Message
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



        public class ReservationViewModel
        {
            public Guid Id { get; set; }

            [Required]
            [Display(Name = "Müşteri")]
            public Guid CustomerId { get; set; }

            [Display(Name = "Not")]
            public string Note { get; set; }

            [Required]
            [Display(Name = "Rezervasyon Tarihi")]
            public DateTime ReservationDate { get; set; }

            [Display(Name = "İndirim Tutarı")]
            [Range(0, double.MaxValue, ErrorMessage = "İndirim tutarı negatif olamaz")]
            public decimal? DiscountAmount { get; set; }

            [Display(Name = "Ekstra Tutar")]
            [Range(0, double.MaxValue, ErrorMessage = "Ekstra tutar negatif olamaz")]
            public decimal? ExtraAmount { get; set; }

            [Display(Name = "Adisyon / Rezervasyonsuz Müşteri")]
            public bool IsWalkIn { get; set; }

            public List<ReservationDetailViewModel> ReservationDetails { get; set; } = new();
        }

        public class ReservationDetailViewModel
        {
            public Guid ServiceId { get; set; }
            public Guid EmployeeId { get; set; }
            public TimeSpan StartTime { get; set; }
            public decimal? CustomPrice { get; set; }
            public string Note { get; set; }
        }
    }
}