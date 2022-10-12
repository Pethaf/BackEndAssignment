namespace ExamContext;

public class DeliveryDesk
{
    public Dictionary<int, List<Pizza>> FinishedOrders { get; } = new();
}
