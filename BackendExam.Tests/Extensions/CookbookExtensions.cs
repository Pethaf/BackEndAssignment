using ExamContext;

namespace BackendExam.Tests.CookbookExtensions;

internal static class CookbookExtensions
{
    public static void Add(this Dictionary<PizzaType, List<(IngredientType ingredient, int quantity)>> dict, PizzaType type)
    {
        List<(IngredientType ingredient, int quantity)> value = type switch
        {
            PizzaType { Name: "Amigo" } => new()
                {
                    (IngredientType.MincedMeat, 1),
                    (IngredientType.RedOnion, 1),
                    (IngredientType.Mushroom, 1),
                    (IngredientType.Pepper, 1),
                },
            PizzaType { Name: "Bambino" } => new()
                {
                    (IngredientType.Ham, 1),
                    (IngredientType.Mushroom, 1),
                    (IngredientType.Pineapple, 1),
                },
            PizzaType { Name: "Favoriten" } => new()
                {
                    (IngredientType.Ham, 1),
                    (IngredientType.KebabMeat, 1),
                    (IngredientType.Friggitello, 1)
                },
            PizzaType { Name: "FavoritenExtraMeat" } => new()
                {
                    (IngredientType.Ham, 1),
                    (IngredientType.KebabMeat, 2),
                    (IngredientType.Friggitello, 1)
                },
            PizzaType { Name: "Hawaii" } => new()
                {
                    (IngredientType.Ham, 1),
                    (IngredientType.Pineapple, 1),
                },
            PizzaType { Name: "LaBussola" } => new()
                {
                    (IngredientType.Ham, 1),
                    (IngredientType.Shrimp, 1),
                },
            PizzaType { Name: "Napoli" } => new()
                {
                    (IngredientType.Ham, 1),
                    (IngredientType.Shrimp, 1),
                    (IngredientType.Pineapple, 1),
                },
            PizzaType { Name: "NapoliExtraAll" } => new()
                {
                    (IngredientType.Ham, 2),
                    (IngredientType.Shrimp, 2),
                    (IngredientType.Pineapple, 2),
                },
            PizzaType { Name: "Parma" } => new()
                {
                    (IngredientType.ParmaHam, 1),
                    (IngredientType.SundriedTomatoe, 1),
                    (IngredientType.Olive, 1),
                    (IngredientType.Arugula, 1),
                },
            _ => throw new Exception($"Unknown pizza type: {type}"),
        };

        dict.Add(
            type,
            value
        );
    }
}
