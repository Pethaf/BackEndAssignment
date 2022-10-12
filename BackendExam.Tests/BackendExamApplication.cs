using ExamContext.TestData;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Collections.Immutable;
using ExamContext;
using BackendExam.Tests.StackExtensions;

namespace BackendExam.Tests;

internal static class PizzaTypes
{
    public static readonly PizzaType Amigo = new("Amigo");
    public static readonly PizzaType Bambino = new("Bambino");
    public static readonly PizzaType Favoriten = new("Favoriten");
    public static readonly PizzaType FavoritenExtraMeat = new("FavoritenExtraMeat");
    public static readonly PizzaType Hawaii = new("Hawaii");
    public static readonly PizzaType LaBussola = new("LaBussola");
    public static readonly PizzaType Napoli = new("Napoli");
    public static readonly PizzaType NapoliExtraAll = new("NapoliExtraAll");
    public static readonly PizzaType Parma = new("Parma");
}

internal class BackendExamApplication : WebApplicationFactory<Program>
{
    public MenuTestData MenuTestData { get; } = new();
    public WarehouseTestData WarehouseTestData { get; } = new();
    public ChefManagerSettings ChefManagerSettings { get; } = new();
    public CookbookTestData CookbookTestData { get; } = new();
    public BakeLog BakeLog { get; } = new();
    public UserRepositoryTestData UserRepositoryTestData { get; } = new();
    public TimeClockTestData TimeClockTestData { get; } = new();
    public JobDurations JobDurations { get; } = new();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(cs =>
        {
            cs.AddSingleton<IJobDurations>(JobDurations);
            cs.AddSingleton<IMenuTestData>(MenuTestData);
            cs.TryAddSingleton<IWarehouseTestData>(WarehouseTestData);
            cs.AddSingleton<IOvenBakeLog>(BakeLog);
            cs.AddSingleton<ICookbookData>(CookbookTestData);
            cs.AddSingleton<IUserRepositoryTestData>(UserRepositoryTestData);
            cs.AddSingleton<ITimeClockTestData>(TimeClockTestData);
            cs.Replace(new ServiceDescriptor(
                typeof(IChefManagerSettings),
                ChefManagerSettings
            ));
        });

        return base.CreateHost(builder);
    }

    public Task<bool> WaitForDeliveryDesk(int count = 1, int timeout = 20) // TODO assert that 20s is enougt time as a timeout
    {
        var desk = Services.GetRequiredService<DeliveryDesk>();
        return Task.Run(async () => {
            var start = DateTimeOffset.Now;
            while (true)
            {
                if (desk.FinishedOrders.Count >= count)
                {
                    return true;
                }
                var duration = DateTimeOffset.Now - start;
                if (duration.TotalSeconds >= timeout)
                {
                    return false;
                }
                await Task.Delay(50);
            }
        });
    }

    public List<Ingredient> AddIngredientToWareHouse(IngredientType type, int quantity)
    {
        List<Ingredient> result = Enumerable.Range(1, quantity)
        .Select(_ => new Ingredient(type, Guid.NewGuid()))
            .ToList();

        WarehouseTestData.TestData.Add(type, result.ToStack());

        return result;
    }
}

public class MenuTestData : IMenuTestData
{
    public List<MenuItem> TestData { get; } = new();
    public ImmutableList<MenuItem> Data => TestData.ToImmutableList();
}

public class WarehouseTestData : IWarehouseTestData
{
    public Dictionary<IngredientType, Stack<Ingredient>> TestData { get; } = new();
    public ImmutableDictionary<IngredientType, Stack<Ingredient>> Data => TestData.ToImmutableDictionary();
}

public class ChefManagerSettings : IChefManagerSettings
{
    public int NumberOfChefs { get; set; } = 1;

    public bool StartChefs { get; set; } = false;
}

public class BakeLog : IOvenBakeLog
{
    private object _lock = new object();
    public List<(Pizza pizza, List<Ingredient> ingredients)> Pizzas { get; } = new();

    public void RegisterPizza(Pizza pizza, List<Ingredient> ingredients)
    {
        lock (_lock)
        {
            Pizzas.Add((pizza, ingredients));
        }
    }
}

public class CookbookTestData : ICookbookData
{
    public Dictionary<PizzaType, List<(IngredientType ingredient, int quantity)>> TestData { get; } = new();
    public ImmutableDictionary<PizzaType, ImmutableList<(IngredientType ingredient, int quantity)>> Data => TestData
        .Select(kv => KeyValuePair.Create(kv.Key, kv.Value.ToImmutableList()))
        .ToImmutableDictionary();
}

public class UserRepositoryTestData : IUserRepositoryTestData
{
    // Känns overkill
    public List<User> TestUsers { get; } = new();
    public List<User> Users => TestUsers;
}

public class TimeClockTestData : ITimeClockTestData
{
    // Känns overkill
    public List<User> TestUsers { get; } = new();
    public List<User> Users => TestUsers;
}

public class JobDurations : IJobDurations
{
    public int WarehouseWork { get; set; }
}
