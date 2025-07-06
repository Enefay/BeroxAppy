using Volo.Abp.Modularity;

namespace BeroxAppy;

[DependsOn(
    typeof(BeroxAppyDomainModule),
    typeof(BeroxAppyTestBaseModule)
)]
public class BeroxAppyDomainTestModule : AbpModule
{

}
