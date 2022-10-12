using System.Collections.Immutable;
using ExamContext.Chef;
using ExamContext.LocalData;
using ExamContext.TestData;

namespace ExamContext;

public class Warehouse
{
    private IChef? _currentOccupant;
    private readonly object _occupantLock = new();
    private ImmutableDictionary<IngredientType, Stack<Ingredient>> Ingredients { get; init; }
    public Queue<(IngredientType type, Guid? id)> RequestLog { get; } = new();
    private IJobDurations _jobDurations;

    public Warehouse(IJobDurations jobDurations, IWarehouseTestData? testData = null)
    {
        _currentOccupant = null;
        testData = testData ?? LocalWarehouseData.Create();
        Ingredients = testData.Data;
        _jobDurations = jobDurations;
    }

    /// <summary>
    /// Fetches an ingredient from the warehouse. Will return null if the requested ingredient
    /// is not present in the warehouse.
    /// </summary>
    /// <param name="ingredientType"></param>
    /// <param name="enteringChef"></param>
    /// <returns></returns>
    public Ingredient? GetIngredient(IngredientType ingredientType, IChef enteringChef)
    {
        lock (_occupantLock)
        {
            if (_currentOccupant is not null)
            {
                throw new ExamException($"The chef {enteringChef.Name} has collided with {_currentOccupant.Name} in the warehouse door!");
            }
            _currentOccupant = enteringChef;
        }

        Thread.Sleep(millisecondsTimeout: _jobDurations.WarehouseWork);
        Ingredient? result = null;
        if (Ingredients.TryGetValue(ingredientType, out var queue))
        {
            if (queue.TryPop(out var ingredient))
            {
                //RequestLog.Enqueue((ingredientType, ingredient.Id));
                result = ingredient;
            }
        }

        RequestLog.Enqueue((ingredientType, result?.Id));

        lock (_occupantLock)
        {
            _currentOccupant = null;
        }

        return result;
    }

    public void AddIngredient(IngredientType ingredientType, Guid id)
    {
        if (Ingredients.ContainsKey(ingredientType))
        {
            Ingredients[ingredientType].Push(new Ingredient(ingredientType, id));
        }
        else
        {
            throw new InvalidIngredientException();
        }

    }

    public List<Ingredient> PeekIngredient(IngredientType ingredientType)
    {
        return Ingredients[ingredientType].ToList();
    }
}


public class InvalidIngredientException : Exception {}