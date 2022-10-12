using BackendExam.Tests.OrderQueueExtensions;
using ExamContext;
using Microsoft.Extensions.DependencyInjection;

namespace BackendExam.Tests.CompleteOrder;

public class DeliveredToDeliveryDesk
{
    private BackendExamApplication _application;

    [SetUp]
    public void Setup()
    {
        _application = new BackendExamApplication();
        _application.ChefManagerSettings.StartChefs = true;
        _application.ChefManagerSettings.NumberOfChefs = 1;

        _application.CookbookTestData.TestData.Add(
            PizzaTypes.Favoriten,
            new List<(IngredientType type, int quantity)>()
            {
                (IngredientType.Ham, 1),
                (IngredientType.KebabMeat, 1),
                (IngredientType.Friggitello, 1)
            }
        );
        _application.CookbookTestData.TestData.Add(
            PizzaTypes.Napoli,
            new List<(IngredientType type, int quantity)>()
            {
                (IngredientType.Ham, 1),
                (IngredientType.Shrimp, 1),
                (IngredientType.Pineapple, 1)
            }
        );
    }

    #region Single order tests
    [Test]
    public async Task OneOrderMovesFromOrderToDelivered()
    {
        AddAbundantIngredients();
        var expectdPizzaType = PizzaTypes.Favoriten;
        _application.CreateDefaultClient();

        (OrderQueue orderQueue, DeliveryDesk deliveryDesk) = GetServices();

        // Act
        var order = orderQueue.EnqueueOrder(expectdPizzaType, 1);
        await _application.WaitForDeliveryDesk();

        // Asserts
        AssertOneOrder(
            orderQueue,
            deliveryDesk,
            order.Id,
            1,
            expectdPizzaType
        );
    }

    [Test]
    public async Task OneOrderWithTwoPizzasSameTypeMovesFromOrderToDelivered()
    {
        AddAbundantIngredients();
        var expectdPizzaType = PizzaTypes.Favoriten;
        _application.CreateDefaultClient();

        (OrderQueue orderQueue, DeliveryDesk deliveryDesk) = GetServices();

        // Act
        var order = orderQueue.EnqueueOrder(expectdPizzaType, 2);
        await _application.WaitForDeliveryDesk();

        // Asserts
        AssertOneOrder(
            orderQueue,
            deliveryDesk,
            order.Id,
            2,
            expectdPizzaType,
            expectdPizzaType
        );
    }

    [Test]
    public async Task OneOrderWithTwoPizzasDifferentTypeMovesFromOrderToDelivered()
    {
        AddAbundantIngredients();
        var expectdPizzaType1 = PizzaTypes.Favoriten;
        var expectdPizzaType2 = PizzaTypes.Napoli;
        _application.CreateDefaultClient();

        (OrderQueue orderQueue, DeliveryDesk deliveryDesk) = GetServices();

        // Act
        var order = orderQueue.EnqueueOrder(
            (expectdPizzaType1, 1),
            (expectdPizzaType2, 1)
        );
        await _application.WaitForDeliveryDesk();

        // Asserts
        AssertOneOrder(
            orderQueue,
            deliveryDesk,
            order.Id,
            2,
            expectdPizzaType1,
            expectdPizzaType2
        );
    }
    #endregion

    #region Multiple orders tests
    [Test]
    public async Task TwoOrdersWithOnePizzaMovesFromOrderToDelivered()
    {
        AddAbundantIngredients();
        var expectdPizzaTypeOrder1 = PizzaTypes.Favoriten;
        var expectdPizzaTypeOrder2 = PizzaTypes.Napoli;
        _application.CreateDefaultClient();

        (OrderQueue orderQueue, DeliveryDesk deliveryDesk) = GetServices();

        // Act
        var order1 = orderQueue.EnqueueOrder(expectdPizzaTypeOrder1, 1);
        var order2 = orderQueue.EnqueueOrder(expectdPizzaTypeOrder2, 1);
        await _application.WaitForDeliveryDesk(2, 40);

        // Asserts
        AssertMultipleOrders(
            orderQueue,
            deliveryDesk,
            new (int orderId, int numberOfPizzas, PizzaType[] pizzaTypes)[]
            {
                (order1.Id, 1, new PizzaType[] { expectdPizzaTypeOrder1 }),
                (order2.Id, 1, new PizzaType[] { expectdPizzaTypeOrder2 })
            }
        );
    }

    [Test]
    public async Task TwoOrdersWithTwoPizzasMovesFromOrderToDelivered()
    {
        AddAbundantIngredients();
        var expectdPizzaTypeOrder1_12 = PizzaTypes.Favoriten;
        var expectdPizzaTypeOrder2_1 = PizzaTypes.Favoriten;
        var expectdPizzaTypeOrder2_2 = PizzaTypes.Napoli;
        _application.CreateDefaultClient();

        (OrderQueue orderQueue, DeliveryDesk deliveryDesk) = GetServices();

        // Act
        var order1 = orderQueue.EnqueueOrder((expectdPizzaTypeOrder1_12, 2));
        var order2 = orderQueue.EnqueueOrder((expectdPizzaTypeOrder2_1, 1), (expectdPizzaTypeOrder2_2, 1));
        await _application.WaitForDeliveryDesk(2, 40);

        // Asserts
        AssertMultipleOrders(
            orderQueue,
            deliveryDesk,
            new (int orderId, int numberOfPizzas, PizzaType[] pizzaTypes)[]
            {
                (order1.Id, 2, new PizzaType[] { expectdPizzaTypeOrder1_12, expectdPizzaTypeOrder1_12 }),
                (order2.Id, 2, new PizzaType[] { expectdPizzaTypeOrder2_1, expectdPizzaTypeOrder2_2 })
            }
        );
    }
    #endregion

    #region Missing ingredients tests

    #endregion

    #region helper methods
    private (OrderQueue orderQueue, DeliveryDesk deliveryDesk) GetServices()
    {
        var orderQueue = _application.Services.GetRequiredService<OrderQueue>();
        var deliveryDesk = _application.Services.GetRequiredService<DeliveryDesk>();

        Assume.That(orderQueue.Queue, Is.Empty);
        Assume.That(deliveryDesk.FinishedOrders, Is.Empty);

        return (orderQueue, deliveryDesk);
    }

    // TODO if foreach loops in assert works. Change this to call orders-version
    private void AssertOneOrder(OrderQueue orderQueue, DeliveryDesk deliveryDesk, int orderId, int numberOfPizzas, params PizzaType[] pizzaTypes)
    {
        Assert.Multiple(() =>
        {
            Assert.That(orderQueue.Queue, Has.Count.EqualTo(0));
            Assert.That(deliveryDesk.FinishedOrders, Has.Count.EqualTo(1));
        });

        // The created order is available on the delivery desk
        Assert.That(deliveryDesk.FinishedOrders, Does.ContainKey(orderId));
        var pizzaList = deliveryDesk.FinishedOrders[orderId];

        Assert.That(pizzaList, Has.Count.EqualTo(numberOfPizzas));
        Assert.That(pizzaList.Select(p => p.Name), Is.EquivalentTo(pizzaTypes));
    }

    private void AssertMultipleOrders(OrderQueue orderQueue, DeliveryDesk deliveryDesk, params (int orderId, int numberOfPizzas, PizzaType[] pizzaTypes)[] orders)
    {
        Assert.Multiple(() =>
        {
            Assert.That(orderQueue.Queue, Has.Count.EqualTo(0));
            Assert.That(deliveryDesk.FinishedOrders, Has.Count.EqualTo(orders.Length));
        });

        foreach ((int orderId, int numberOfPizzas, PizzaType[] pizzaTypes) in orders)
        {
            Assert.That(deliveryDesk.FinishedOrders, Does.ContainKey(orderId));
            var pizzaList = deliveryDesk.FinishedOrders[orderId];

            Assert.That(pizzaList, Has.Count.EqualTo(numberOfPizzas));
            Assert.That(pizzaList.Select(p => p.Name), Is.EquivalentTo(pizzaTypes));
        }
    }

    private void AddAbundantIngredients()
    {
        _application.WarehouseTestData.TestData.Add(IngredientType.Ham, MakeIngredients(IngredientType.Ham, 100));
        _application.WarehouseTestData.TestData.Add(IngredientType.KebabMeat, MakeIngredients(IngredientType.KebabMeat, 100));
        _application.WarehouseTestData.TestData.Add(IngredientType.Friggitello, MakeIngredients(IngredientType.Friggitello, 100));
        _application.WarehouseTestData.TestData.Add(IngredientType.Shrimp, MakeIngredients(IngredientType.Shrimp, 100));
        _application.WarehouseTestData.TestData.Add(IngredientType.Pineapple, MakeIngredients(IngredientType.Pineapple, 100));
    }

    private Stack<Ingredient> MakeIngredients(IngredientType type, int count)
    {
        return new Stack<Ingredient>(
            Enumerable
                .Range(1, count)
                .Select(_ => new Ingredient(type, Guid.NewGuid()))
            );
    }
    #endregion
}
