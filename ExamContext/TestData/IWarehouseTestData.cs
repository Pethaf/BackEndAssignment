using System.Collections.Immutable;

namespace ExamContext.TestData;

public interface IWarehouseTestData
{
    ImmutableDictionary<IngredientType, Stack<Ingredient>> Data { get; }
}
