using ExamContext.Chef;
using ExamContext.LocalData;
using ExamContext.TestData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ExamContext;

public static class ExamContext
{
    public static void AddExamContext(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        AddServices(services);
    }

    public static void BootstrapExamContext(this IHost host)
    {
        StartUpChefs(host);
    }

    private static void AddServices(IServiceCollection services)
    {
        services.TryAddSingleton<ChefManager>();
        services.TryAddSingleton<Cookbook>();
        services.TryAddSingleton<DeliveryDesk>();
        services.TryAddSingleton<IChefManagerSettings, LocalChefManagerSettings>();
        services.TryAddSingleton<IJobDurations, LocalJobDurations>();
        services.TryAddSingleton<Menu>();
        services.TryAddSingleton<OrderQueue>();
        services.TryAddSingleton<Oven>();
        services.TryAddSingleton<TimeClock>();
        services.TryAddSingleton<UserRepository>();
        services.TryAddSingleton<Warehouse>();
    }

    private static void StartUpChefs(IHost host)
    {
        var chefManager = host.Services.GetService<ChefManager>();
        if (chefManager is null)
        {
            Console.WriteLine("ERROR: failed to get chef manager. Cannot put chefs to work");
        }
        else
        {
            chefManager.StartChefs();
        }
    }
}
