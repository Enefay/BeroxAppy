using Xunit;

namespace BeroxAppy.EntityFrameworkCore;

[CollectionDefinition(BeroxAppyTestConsts.CollectionDefinitionName)]
public class BeroxAppyEntityFrameworkCoreCollection : ICollectionFixture<BeroxAppyEntityFrameworkCoreFixture>
{

}
