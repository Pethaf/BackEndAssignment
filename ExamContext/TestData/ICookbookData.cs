using System.Collections.Immutable;

namespace ExamContext.TestData;

public interface ICookbookData
{
    ImmutableDictionary<PizzaType, ImmutableList<(IngredientType ingredient, int quantity)>> Data { get; }
}
