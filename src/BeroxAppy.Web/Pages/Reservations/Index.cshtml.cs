using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using BeroxAppy.Customers;
using BeroxAppy.Employees;
using BeroxAppy.Services;
using System.Collections.Generic;
using System.Linq;
using BeroxAppy.Reservations;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace BeroxAppy.Web.Pages.Reservations
{
    public class IndexModel : BeroxAppyPageModel
    {
        private readonly IReservationAppService _reservationAppService;
        private readonly ICustomerAppService _customerAppService;
        private readonly IEmployeeAppService _employeeAppService;
        private readonly IServiceAppService _serviceAppService;

        public IndexModel(
            IReservationAppService reservationAppService,
            ICustomerAppService customerAppService,
            IEmployeeAppService employeeAppService,
            IServiceAppService serviceAppService)
        {
            _reservationAppService = reservationAppService;
            _customerAppService = customerAppService;
            _employeeAppService = employeeAppService;
            _serviceAppService = serviceAppService;
        }

        public List<SelectListItem> Customers { get; set; }
        public List<SelectListItem> Employees { get; set; }
        public List<SelectListItem> Services { get; set; }

        public async Task OnGetAsync()
        {
            // Müþteri listesi
            var customerList = await _customerAppService.GetActiveListAsync();
            Customers = customerList.Items.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.FullName} - {x.Phone}"
            }).ToList();

            // Çalýþan listesi
            var employeeList = await _employeeAppService.GetActiveListAsync();
            Employees = employeeList.Items.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.FullName
            }).ToList();

            // Hizmet listesi
            var serviceList = await _serviceAppService.GetActiveListAsync();
            Services = serviceList.Items.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.Title} ({x.DurationDisplay} - {x.Price:C})"
            }).ToList();
        }

        public async Task<IActionResult> OnGetCalendarEventsAsync(DateTime start, DateTime end)
        {
            var events = await _reservationAppService.GetCalendarEventsAsync(start, end);
            return new JsonResult(events);
        }

        public async Task<IActionResult> OnGetAvailableSlotsAsync(Guid employeeId, Guid serviceId, DateTime date)
        {
            var slots = await _reservationAppService.GetAvailableSlotsAsync(employeeId, serviceId, date);
            return new JsonResult(slots);
        }

        public async Task<IActionResult> OnGetEmployeesByServiceAsync(Guid serviceId)
        {
            var employees = await _employeeAppService.GetEmployeesByServiceAsync(serviceId);
            return new JsonResult(employees.Items);
        }

        public async Task<IActionResult> OnGetReservationDetailsAsync(Guid id)
        {
            var reservation = await _reservationAppService.GetAsync(id);
            return new JsonResult(reservation);
        }
    }
}