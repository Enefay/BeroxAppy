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
                return Page();
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

                return NoContent();
            }
            catch (Exception ex)
            {
                // Log the error
                Logger.LogError(ex, "Rezervasyon kaydedilirken hata oluþtu");
                throw;
            }
        }

        // AJAX Handler Methods
        public async Task<IActionResult> OnGetEmployeesByServiceAsync(Guid serviceId)
        {
            var employees = await _employeeAppService.GetEmployeesByServiceAsync(serviceId);
            return new JsonResult(employees.Items);
        }

        public async Task<IActionResult> OnGetAvailableSlotsAsync(Guid employeeId, Guid serviceId, DateTime date)
        {
            var slots = await _reservationAppService.GetAvailableSlotsAsync(employeeId, serviceId, date);
            return new JsonResult(new { availableSlots = slots });
        }

        private async Task LoadLookups()
        {
            // Müþteri listesi
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
                Text = $"{x.Title} ({x.DurationDisplay} - {x.Price:C})"
            }).ToList();

            // Çalýþan listesi
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
            [Display(Name = "Müþteri")]
            public Guid CustomerId { get; set; }

            [Display(Name = "Not")]
            public string Note { get; set; }

            [Required]
            [Display(Name = "Rezervasyon Tarihi")]
            public DateTime ReservationDate { get; set; }

            [Display(Name = "Ýndirim Tutarý")]
            [Range(0, double.MaxValue, ErrorMessage = "Ýndirim tutarý negatif olamaz")]
            public decimal? DiscountAmount { get; set; }

            [Display(Name = "Ekstra Tutar")]
            [Range(0, double.MaxValue, ErrorMessage = "Ekstra tutar negatif olamaz")]
            public decimal? ExtraAmount { get; set; }

            [Display(Name = "Adisyon / Rezervasyonsuz Müþteri")]
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