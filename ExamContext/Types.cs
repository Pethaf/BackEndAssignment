namespace ExamContext;

public record IdGenerator
{
    private static int Ids = 100;
    public int Id { get; } = Interlocked.Increment(ref Ids);
}

public record Ingredient(IngredientType Type, Guid Id);

public enum IngredientType
{
    TomatoSauce, Ham, Mushroom, KebabMeat, Friggitello, Shrimp, Pineapple,
    MincedMeat, RedOnion, Pepper, ParmaHam, SundriedTomatoe, Olive, Arugula
}

public record MenuItem(PizzaType Name, double Price);

public record Order(List<(PizzaType pizzaType, int quantity)> Pizzas) : IdGenerator;

public record Pizza(PizzaType Name, Guid Id);

public record InvalidPizza(PizzaType Name, Guid Id, IEnumerable<IngredientType> MissingIngredients) : Pizza(Name, Id);

public record PizzaType(string Name);

public record User(string Id, string UserName, string Password, List<Role> Roles);

public record Role(string Name);