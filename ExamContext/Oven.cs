using ExamContext.LocalData;
using ExamContext.TestData;
using System.Collections.Immutable;
using System.Linq;

namespace ExamContext;

public class Oven
{
    private readonly Cookbook _cookbook;
    private readonly IOvenBakeLog _bakeLog;

    public Oven(Cookbook cookbook, IOvenBakeLog? bakeLog = null)
    {
        _cookbook = cookbook;
        _bakeLog = bakeLog ?? LoggingBakeLog.Create();
    }

    public async Task<Pizza> Bake(List<Ingredient> ingredients)
    {
        await Task.Delay(1000);

        var ingredientGroups = ingredients
            .GroupBy(x => x.Type)
            .Select<IGrouping<IngredientType, Ingredient>, (IngredientType ingredient, int quantity)>(g => (g.Key, g.Count()))
            .OrderBy(g => g.ingredient)
            .ToList();

        var possibleRecipies = _cookbook.CookbookList;

        Pizza? pizza = null;
        IEnumerable<IngredientType>? missingIngredients = null;
        foreach (var pair in possibleRecipies)
        {
            var possibleMatch = pair.Value;
            var intersection = possibleMatch.Except(ingredientGroups);

            if (intersection.Count() == 0)
            {
                pizza = new Pizza(pair.Key, Guid.NewGuid());
                break;
            }
            else
            {
                var list = intersection
                    .SelectMany(x =>
                    {
                        return Enumerable.Range(0, x.quantity).Select(_ => x.ingredient);
                    });
                if (list.Count() < (missingIngredients?.Count() ?? int.MaxValue))
                {
                    missingIngredients = list;
                }
            }
        }

        if (pizza is null)
        {
            pizza = new InvalidPizza(new PizzaType("invalid-pizza"), Guid.NewGuid(), missingIngredients!.ToList());
        }

        _bakeLog.RegisterPizza(pizza, ingredients);
        return pizza;
    }
}
