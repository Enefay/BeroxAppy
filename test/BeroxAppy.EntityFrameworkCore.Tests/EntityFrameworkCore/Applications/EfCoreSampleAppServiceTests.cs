using BeroxAppy.Samples;
using Xunit;

namespace BeroxAppy.EntityFrameworkCore.Applications;

[Collection(BeroxAppyTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<BeroxAppyEntityFrameworkCoreTestModule>
{

}
