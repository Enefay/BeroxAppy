using Microsoft.Extensions.Localization;
using BeroxAppy.Localization;
using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;

namespace BeroxAppy.Web;

[Dependency(ReplaceServices = true)]
public class BeroxAppyBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<BeroxAppyResource> _localizer;

    public BeroxAppyBrandingProvider(IStringLocalizer<BeroxAppyResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
