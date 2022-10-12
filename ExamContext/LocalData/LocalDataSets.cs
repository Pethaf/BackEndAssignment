using ExamContext.TestData;
using System.Collections.Immutable;

namespace ExamContext.LocalData;

internal static class PizzaTypes
{
    public static readonly PizzaType Vesuvio = new("Vesuvio");
    public static readonly PizzaType AlFunghi = new("Al funghi");
    public static readonly PizzaType Capricciosa = new("Capricciosa");
}

public abstract class LocalData<T> where T : new()
{
    public static T Create() => new();
}

public class LocalCookbookData : LocalData<LocalCookbookData>, ICookbookData
{
    public ImmutableDictionary<PizzaType, ImmutableList<(IngredientType ingredient, int quantity)>> Data =>
        ImmutableDictionary.CreateBuilder<PizzaType, ImmutableList<(IngredientType ingredient, int quantity)>>()
            .Append(
                PizzaTypes.Vesuvio,
                ImmutableList.CreateBuilder<(IngredientType ingredient, int quantity)>()
                    .Append((IngredientType.TomatoSauce, 1))
                    .Append((IngredientType.Ham, 1))
                    .ToImmutable()
            )
            .Append(
                PizzaTypes.AlFunghi,
                ImmutableList.CreateBuilder<(IngredientType ingredient, int quantity)>()
                    .Append((IngredientType.TomatoSauce, 1))
                    .Append((IngredientType.Mushroom, 1))
                    .ToImmutable()
            )
            .Append(
                PizzaTypes.Capricciosa,
                ImmutableList.CreateBuilder<(IngredientType ingredient, int quantity)>()
                    .Append((IngredientType.TomatoSauce, 1))
                    .Append((IngredientType.Ham, 1))
                    .Append((IngredientType.Mushroom, 1))
                    .ToImmutable()
            )
            .ToImmutable();
}

public class LocalMenuData : LocalData<LocalMenuData>, IMenuTestData
{
    public ImmutableList<MenuItem> Data => ImmutableList.CreateBuilder<MenuItem>()
        .Append(new MenuItem(PizzaTypes.Vesuvio, 99))
        .Append(new MenuItem(PizzaTypes.AlFunghi, 99))
        .Append(new MenuItem(PizzaTypes.Capricciosa, 109))
        .ToImmutable();
}

public class LocalWarehouseData : LocalData<LocalWarehouseData>, IWarehouseTestData
{
    public ImmutableDictionary<IngredientType, Stack<Ingredient>> Data => new Dictionary<IngredientType, Stack<Ingredient>>(new List<KeyValuePair<IngredientType, Stack<Ingredient>>>()
    {
        Helpers.IngredientKeyValuePair(IngredientType.TomatoSauce, 10),
        Helpers.IngredientKeyValuePair(IngredientType.Ham, 5),
        Helpers.IngredientKeyValuePair(IngredientType.Mushroom, 5)
    }).ToImmutableDictionary();
}

public class LocalChefManagerSettings : IChefManagerSettings
{
    public int NumberOfChefs => 3;
    public bool StartChefs => true;
}

public class LoggingBakeLog : LocalData<LoggingBakeLog>, IOvenBakeLog
{
    public void RegisterPizza(Pizza pizza, List<Ingredient> ingredients)
    {
        Console.WriteLine($"Pizza done! {pizza} -- {string.Join(',', ingredients)}");
    }
}

public class LocalTimeClockTestData : LocalData<LocalTimeClockTestData>, ITimeClockTestData
{
    public List<User> Users { get; } = new();
}

public class LocalUserRepositoryTestData : LocalData<LocalUserRepositoryTestData>, IUserRepositoryTestData
{
    public List<User> Users { get; } = new()
    {
        new User("", "Manager1", "asd", new List<Role>() { new Role("Manager"), new Role("Employee") }),
        new User("", "Employee1", "asd", new List<Role>() { new Role("Employee") }),
    };
}

public class LocalJobDurations : IJobDurations
{
    public int WarehouseWork => 500;
}

internal static class Helpers
{
    public static KeyValuePair<IngredientType, Stack<Ingredient>> IngredientKeyValuePair(IngredientType type, int count) =>
    new KeyValuePair<IngredientType, Stack<Ingredient>>(
        type,
        new Stack<Ingredient>(Enumerable.Range(0, count).Select(_ => new Ingredient(type, Guid.NewGuid())))
    );
}

public static class ImmutableDictionaryExtensions
{
    public static ImmutableDictionary<TKey, TValue>.Builder Append<TKey, TValue>(
        this ImmutableDictionary<TKey, TValue>.Builder builder,
        TKey key,
        TValue value
    ) where TKey : notnull
    {
        builder.Add(key, value);
        return builder;
    }

    public static ImmutableList<TValue>.Builder Append<TValue>(this ImmutableList<TValue>.Builder builder, TValue value)
    {
        builder.Add(value);
        return builder;
    }
}
