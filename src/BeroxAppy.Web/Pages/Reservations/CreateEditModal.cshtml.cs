using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BeroxAppy.Reservations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using BeroxAppy.Customers;
using BeroxAppy.Services;

namespace BeroxAppy.Web.Pages.Reservations
{
    public class CreateEditModalModel : BeroxAppyPageModel
    {
        private readonly IReservationAppService _reservationAppService;
        private readonly ICustomerAppService _customerAppService;
        private readonly IServiceAppService _serviceAppService;

        [BindProperty]
        public ReservationViewModel Reservation { get; set; }

        public List<SelectListItem> Customers { get; set; }
        public List<SelectListItem> Services { get; set; }

        public CreateEditModalModel(
            IReservationAppService reservationAppService,
            ICustomerAppService customerAppService,
            IServiceAppService serviceAppService)
        {
            _reservationAppService = reservationAppService;
            _customerAppService = customerAppService;
            _serviceAppService = serviceAppService;
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

        private async Task LoadLookups()
        {
            var customerList = await _customerAppService.GetActiveListAsync();
            Customers = customerList.Items.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.FullName} - {x.Phone}"
            }).ToList();

            var serviceList = await _serviceAppService.GetActiveListAsync();
            Services = serviceList.Items.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.Title} ({x.DurationDisplay} - {x.Price:C})"
            }).ToList();
        }

        public class ReservationViewModel
        {
            public Guid Id { get; set; }
            public Guid CustomerId { get; set; }
            public string Note { get; set; }
            public DateTime ReservationDate { get; set; }
            public decimal? DiscountAmount { get; set; }
            public decimal? ExtraAmount { get; set; }
            public bool IsWalkIn { get; set; }
            public List<ReservationDetailViewModel> ReservationDetails { get; set; }
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