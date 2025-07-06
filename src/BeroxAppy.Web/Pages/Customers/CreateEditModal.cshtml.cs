using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BeroxAppy.Customers;
using BeroxAppy.Enums;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace BeroxAppy.Web.Pages.Customers
{
    public class CreateEditModalModel : AbpPageModel
    {
        [HiddenInput]
        [BindProperty(SupportsGet = true)]
        public Guid Id { get; set; }

        [BindProperty]
        public CustomerDto Customer { get; set; }

        private readonly ICustomerAppService _customerAppService;

        public CreateEditModalModel(ICustomerAppService customerAppService)
        {
            _customerAppService = customerAppService;
        }

        public async Task OnGetAsync()
        {
            if (Id != Guid.Empty)
            {
                var customerDto = await _customerAppService.GetAsync(Id);
                Customer = new CustomerDto
                {
                    FullName = customerDto.FullName,
                    Phone = customerDto.Phone,
                    Email = customerDto.Email,
                    BirthDate = customerDto.BirthDate,
                    Gender = customerDto.Gender,
                    Note = customerDto.Note,
                    Instagram = customerDto.Instagram,
                    DiscountRate = customerDto.DiscountRate,
                    TotalDebt = customerDto.TotalDebt,
                    IsActive = customerDto.IsActive
                };
            }
            else
            {
                Customer = new CustomerDto
                {
                    Gender = Gender.Unisex,
                    DiscountRate = 0,
                    TotalDebt = 0,
                    IsActive = true
                };
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Telefon numarasý temizleme
                if (!string.IsNullOrWhiteSpace(Customer.Phone))
                {
                    Customer.Phone = Customer.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
                }

                // Instagram username temizleme
                if (!string.IsNullOrWhiteSpace(Customer.Instagram))
                {
                    Customer.Instagram = Customer.Instagram.TrimStart('@');
                }

                if (Id != Guid.Empty)
                {
                    await _customerAppService.UpdateAsync(Id, Customer);
                }
                else
                {
                    await _customerAppService.CreateAsync(Customer);
                }

                return NoContent();
            }
            catch (Volo.Abp.BusinessException ex)
            {
                // Telefon numarasý duplicate hatasý
                if (ex.Message.Contains("telefon"))
                {
                    ModelState.AddModelError("Customer.Phone", ex.Message);
                    return Page();
                }

                ModelState.AddModelError("", ex.Message);
                return Page();
            }
        }
    }
}