using BackendExam.Shared;
using ExamContext;

namespace BackendExam.Controllers
{
    public class PizzaOrderDoneDTO
    {
        public string status { get; set; } = "done";
        public List<DeliveredPizza> order {get; set;} = new();
    }
}