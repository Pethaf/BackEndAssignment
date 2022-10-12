using ExamContext;

namespace BackendExam.Tests.OrderQueueExtensions;

internal static class OrderQueueHelpers
{
    public static Order EnqueueOrder(this OrderQueue orderQueue, PizzaType type, int quantity)
    {
        return orderQueue.EnqueueOrder((type, quantity));
    }

    public static Order EnqueueOrder(this OrderQueue orderQueue, params (PizzaType type, int quantity)[] pizzas)
    {
        var order = new Order(
            pizzas.ToList()
        );
        orderQueue.Queue.Enqueue(order);
        return order;
    }
}
