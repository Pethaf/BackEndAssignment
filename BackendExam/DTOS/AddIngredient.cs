using ExamContext;

namespace BackendExam.DTOS
{
    public class AddIngredient
    {
        public Guid Id { get; set; }
        public IngredientType ingredientType { get; set; }
    }
}
