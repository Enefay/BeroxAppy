using System.Threading.Tasks;
using BeroxAppy.Services;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace BeroxAppy.Web.Pages.Services.Services
{
    public class IndexModel : AbpPageModel
    {
        private readonly IServiceAppService _serviceAppService;

        public IndexModel(IServiceAppService serviceAppService)
        {
            _serviceAppService = serviceAppService;
        }

        public async Task OnGetAsync()
        {

        }
    }
}