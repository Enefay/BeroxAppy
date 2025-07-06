using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BeroxAppy.Data;
using Volo.Abp.DependencyInjection;

namespace BeroxAppy.EntityFrameworkCore;

public class EntityFrameworkCoreBeroxAppyDbSchemaMigrator
    : IBeroxAppyDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreBeroxAppyDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolve the BeroxAppyDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<BeroxAppyDbContext>()
            .Database
            .MigrateAsync();
    }
}
