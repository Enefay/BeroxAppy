using System.Threading.Tasks;
using BeroxAppy.Services;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace BeroxAppy.Web.Pages.Services.ServiceCategories
{
    public class IndexModel : AbpPageModel
    {
        private readonly IServiceCategoryAppService _serviceCategoryAppService;

        public IndexModel(IServiceCategoryAppService serviceCategoryAppService)
        {
            _serviceCategoryAppService = serviceCategoryAppService;
        }

        public async Task OnGetAsync()
        {

        }
    }
}