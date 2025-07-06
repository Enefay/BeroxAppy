using BeroxAppy.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace BeroxAppy.Web.Pages;

/* Inherit your PageModel classes from this class.
 */
public abstract class BeroxAppyPageModel : AbpPageModel
{
    protected BeroxAppyPageModel()
    {
        LocalizationResourceType = typeof(BeroxAppyResource);
    }
}
