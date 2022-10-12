using ExamContext.LocalData;
using ExamContext.TestData;
using System.Collections.Immutable;

namespace ExamContext;

public class Cookbook
{

    public ImmutableDictionary<PizzaType, ImmutableList<(IngredientType ingredient, int quantity)>> CookbookList { get; private set; }

    public Cookbook(ICookbookData? data = null)
    {
        data = data ?? LocalCookbookData.Create();
        CookbookList = data.Data;
    }
}
