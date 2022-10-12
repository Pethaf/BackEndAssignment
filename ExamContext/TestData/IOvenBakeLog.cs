namespace ExamContext.TestData;

public interface IOvenBakeLog
{
    void RegisterPizza(Pizza pizza, List<Ingredient> ingredients);
}
