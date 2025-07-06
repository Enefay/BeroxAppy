using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BeroxAppy.Services;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace BeroxAppy.Web.Pages.Services.ServiceCategories
{
    public class CreateEditModalModel : AbpPageModel
    {
        [HiddenInput]
        [BindProperty(SupportsGet = true)]
        public Guid Id { get; set; }

        [BindProperty]
        public ServiceCategoryDto ServiceCategory { get; set; }

        private readonly IServiceCategoryAppService _serviceCategoryAppService;

        public CreateEditModalModel(IServiceCategoryAppService serviceCategoryAppService)
        {
            _serviceCategoryAppService = serviceCategoryAppService;
        }

        public async Task OnGetAsync()
        {
            if (Id != Guid.Empty)
            {
                var serviceCategoryDto = await _serviceCategoryAppService.GetAsync(Id);
                ServiceCategory = new ServiceCategoryDto
                {
                    Name = serviceCategoryDto.Name,
                    Color = serviceCategoryDto.Color,
                    DisplayOrder = serviceCategoryDto.DisplayOrder,
                    IsActive = serviceCategoryDto.IsActive
                };
            }
            else
            {
                ServiceCategory = new ServiceCategoryDto
                {
                    Color = "#3498db",
                    IsActive = true,
                    DisplayOrder = 0
                };
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (Id != Guid.Empty)
                {
                    await _serviceCategoryAppService.UpdateAsync(Id, ServiceCategory);
                }
                else
                {
                    await _serviceCategoryAppService.CreateAsync(ServiceCategory);
                }

                return NoContent();
            }
            catch (Volo.Abp.BusinessException ex)
            {
                // Ýþ mantýðý hatalarýný yakala
                ModelState.AddModelError("", ex.Message);
                return Page();
            }
        }
    }
}