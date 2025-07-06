using BeroxAppy.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace BeroxAppy.Permissions;

public class BeroxAppyPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(BeroxAppyPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(BeroxAppyPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<BeroxAppyResource>(name);
    }
}
