using BackendExam.Tests.CookbookExtensions;
using BackendExam.Tests.OrderQueueExtensions;
using BackendExam.Tests.StackExtensions;
using ExamContext;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using static System.Net.Mime.MediaTypeNames;

namespace BackendExam.Tests.CompleteOrder;

public class OvenTests
{
    private BackendExamApplication _application;

    [SetUp]
    public void Setup()
    {
        _application = new BackendExamApplication();
        _application.ChefManagerSettings.StartChefs = true;
        _application.ChefManagerSettings.NumberOfChefs = 1;

        _application.CookbookTestData.TestData.Add(PizzaTypes.Bambino);
        _application.CookbookTestData.TestData.Add(PizzaTypes.FavoritenExtraMeat);
        _application.CookbookTestData.TestData.Add(PizzaTypes.LaBussola);
        _application.CookbookTestData.TestData.Add(PizzaTypes.Napoli);
        _application.CookbookTestData.TestData.Add(PizzaTypes.NapoliExtraAll);
    }

    #region one order
    [Test]
    public async Task OneOrderWithOnePizzaIsBaked()
    {
        var expectdPizzaType = PizzaTypes.Bambino;
        var theHam = _application.AddIngredientToWareHouse(IngredientType.Ham, 1)[0];
        var theMushroom = _application.AddIngredientToWareHouse(IngredientType.Mushroom, 1)[0];
        var thePineapple = _application.AddIngredientToWareHouse(IngredientType.Pineapple, 1)[0];
        _application.CreateDefaultClient();

        (OrderQueue orderQueue, DeliveryDesk deliveryDesk) = GetServices();

        // Act
        var order = orderQueue.EnqueueOrder(expectdPizzaType, 1);
        await _application.WaitForDeliveryDesk();

        // Asserts
        Assume.That(orderQueue.Queue, Is.Empty);
        Assume.That(deliveryDesk.FinishedOrders, Has.Count.EqualTo(1));

        var deliveredPizza = AssertDeliveryDesk(deliveryDesk, order.Id, expectdPizzaType);

        // Assert: bake log looks OK
        Assert.That(_application.BakeLog.Pizzas, Has.Count.EqualTo(1));
        var (pizza, ingredients) = _application.BakeLog.Pizzas[0];
        Assert.That(ingredients, Is.EquivalentTo(new List<Ingredient>() { theHam, theMushroom, thePineapple }));

        // Assert delivery desk and bake log are in sync
        Assert.That(pizza, Is.EqualTo(deliveredPizza));
    }

    [Test]
    public async Task OneOrderWithTwoPizzasOfSameTypeAreBaked()
    {
        var expectdPizzaType = PizzaTypes.Bambino;
        var theHams = _application.AddIngredientToWareHouse(IngredientType.Ham, 2);
        var theMushrooms = _application.AddIngredientToWareHouse(IngredientType.Mushroom, 2);
        var thePineapples = _application.AddIngredientToWareHouse(IngredientType.Pineapple, 2);
        _application.CreateDefaultClient();

        (OrderQueue orderQueue, DeliveryDesk deliveryDesk) = GetServices();

        // Act
        var order = orderQueue.EnqueueOrder(expectdPizzaType, 2);
        await _application.WaitForDeliveryDesk();

        // Asserts
        Assume.That(orderQueue.Queue, Is.Empty);
        Assume.That(deliveryDesk.FinishedOrders, Has.Count.EqualTo(1));

        var (deliveredPizza1, deliveredPizza2) = AssertDeliveryDesk(deliveryDesk, order.Id, expectdPizzaType, expectdPizzaType);

        // Assert: bake log
        Assert.That(_application.BakeLog.Pizzas, Has.Count.EqualTo(2));
        var actualIngredients = _application.BakeLog.Pizzas.SelectMany(p => p.ingredients);
        var expectedIngredients = Merge(theHams, theMushrooms, thePineapples);
        Assert.That(actualIngredients, Is.EquivalentTo(expectedIngredients));

        // Assert: delivery desk + bake log
        var bakeLogPizzas = _application.BakeLog.Pizzas.Select((p) => p.pizza);
        var deliveryDeskPizzas = ImmutableList.Create(deliveredPizza1, deliveredPizza2);
        Assert.That(bakeLogPizzas, Is.EquivalentTo(deliveryDeskPizzas));
    }
    #endregion

    #region two orders
    [Test]
    public async Task TwoOrdersWithOnePizzaEachIsBaked()
    {
        var expectdPizzaType = PizzaTypes.Bambino;
        var theHams = _application.AddIngredientToWareHouse(IngredientType.Ham, 2);
        var theMushrooms = _application.AddIngredientToWareHouse(IngredientType.Mushroom, 2);
        var thePineapples = _application.AddIngredientToWareHouse(IngredientType.Pineapple, 2);
        _application.CreateDefaultClient();

        (OrderQueue orderQueue, DeliveryDesk deliveryDesk) = GetServices();

        // Act
        var order1 = orderQueue.EnqueueOrder(expectdPizzaType, 1);
        var order2 = orderQueue.EnqueueOrder(expectdPizzaType, 1);
        await _application.WaitForDeliveryDesk(2, 40);

        // Asserts
        Assume.That(orderQueue.Queue, Is.Empty);
        Assume.That(deliveryDesk.FinishedOrders, Has.Count.EqualTo(2));

        var (order1Pizza, order2Pizza) = AssertDeliveryDeskTwoOrders(deliveryDesk, order1.Id, order2.Id, expectdPizzaType, expectdPizzaType);

        // Assert: bake log
        Assert.That(_application.BakeLog.Pizzas, Has.Count.EqualTo(2));
        var actualIngredients = _application.BakeLog.Pizzas.SelectMany(p => p.ingredients);
        var expectedIngredients = Merge(theHams, theMushrooms, thePineapples);
        Assert.That(actualIngredients, Is.EquivalentTo(expectedIngredients));

        // Assert: delivery desk + bake log
        var bakeLogPizzas = _application.BakeLog.Pizzas.Select((p) => p.pizza);
        var deliveryDeskPizzas = ImmutableList.Create(order1Pizza, order2Pizza);
        Assert.That(bakeLogPizzas, Is.EquivalentTo(deliveryDeskPizzas));
    }

    [Test]
    public async Task TwoOrdersWithOneDifferentPizzaEachIsBaked()
    {
        var expectdPizzaType1 = PizzaTypes.Bambino;
        var expectdPizzaType2 = PizzaTypes.LaBussola;
        var theHams = _application.AddIngredientToWareHouse(IngredientType.Ham, 2);
        var theMushrooms = _application.AddIngredientToWareHouse(IngredientType.Mushroom, 1);
        var thePineapples = _application.AddIngredientToWareHouse(IngredientType.Pineapple, 1);
        var theShrimps = _application.AddIngredientToWareHouse(IngredientType.Shrimp, 1);
        _application.CreateDefaultClient();

        (OrderQueue orderQueue, DeliveryDesk deliveryDesk) = GetServices();

        // Act
        var order1 = orderQueue.EnqueueOrder(expectdPizzaType1, 1);
        var order2 = orderQueue.EnqueueOrder(expectdPizzaType2, 1);
        await _application.WaitForDeliveryDesk(2, 40);

        // Asserts
        Assume.That(orderQueue.Queue, Is.Empty);
        Assume.That(deliveryDesk.FinishedOrders, Has.Count.EqualTo(2));

        var (order1Pizza, order2Pizza) = AssertDeliveryDeskTwoOrders(deliveryDesk, order1.Id, order2.Id, expectdPizzaType1, expectdPizzaType2);

        // Assert: bake log
        Assert.That(_application.BakeLog.Pizzas, Has.Count.EqualTo(2));
        var actualIngredients = _application.BakeLog.Pizzas.SelectMany(p => p.ingredients);
        var expectedIngredients = Merge(theHams, theMushrooms, thePineapples, theShrimps);
        Assert.That(actualIngredients, Is.EquivalentTo(expectedIngredients));

        // Assert: delivery desk + bake log
        var bakeLogPizzas = _application.BakeLog.Pizzas.Select((p) => p.pizza);
        var deliveryDeskPizzas = ImmutableList.Create(order1Pizza, order2Pizza);
        Assert.That(bakeLogPizzas, Is.EquivalentTo(deliveryDeskPizzas));
    }
    #endregion

    #region invalid pizzas
    [Test]
    public async Task OneOrderWithMissingIngredientBakesAnotherTypeOfPizza()
    {
        var pizzaTypeToOrder = PizzaTypes.Napoli;
        var expectdPizzaType = PizzaTypes.LaBussola;
        var theHams = _application.AddIngredientToWareHouse(IngredientType.Ham, 1);
        var theShrimps = _application.AddIngredientToWareHouse(IngredientType.Shrimp, 1);

        _application.CreateDefaultClient();

        (OrderQueue orderQueue, DeliveryDesk deliveryDesk) = GetServices();

        // Act
        var order = orderQueue.EnqueueOrder(pizzaTypeToOrder, 1);
        await _application.WaitForDeliveryDesk();

        // Asserts
        Assume.That(orderQueue.Queue, Is.Empty);
        Assume.That(deliveryDesk.FinishedOrders, Has.Count.EqualTo(1));

        var deliveredPizza = AssertDeliveryDesk(deliveryDesk, order.Id, expectdPizzaType);

        // Assert: bake log looks OK
        Assert.That(_application.BakeLog.Pizzas, Has.Count.EqualTo(1));
        var (pizza, ingredients) = _application.BakeLog.Pizzas[0];
        Assert.That(ingredients, Is.EquivalentTo(Merge(theHams, theShrimps)));

        // Assert delivery desk and bake log are in sync
        Assert.That(pizza, Is.EqualTo(deliveredPizza));
    }

    [Test]
    public async Task OneOrderWithMissingIngredientsBakesInvalidPizza()
    {
        var pizzaTypeToOrder = PizzaTypes.Napoli;
        var theHams = _application.AddIngredientToWareHouse(IngredientType.Ham, 1);

        _application.CreateDefaultClient();

        (OrderQueue orderQueue, DeliveryDesk deliveryDesk) = GetServices();

        // Act
        var order = orderQueue.EnqueueOrder(pizzaTypeToOrder, 1);
        await _application.WaitForDeliveryDesk();

        // Asserts
        Assume.That(orderQueue.Queue, Is.Empty);
        Assume.That(deliveryDesk.FinishedOrders, Has.Count.EqualTo(1));

        var deliveredPizza = AssertDeliveryDesk(deliveryDesk, order.Id, new PizzaType("invalid-pizza"));

        // Assert: bake log looks OK
        Assert.That(_application.BakeLog.Pizzas, Has.Count.EqualTo(1));
        var (pizza, ingredients) = _application.BakeLog.Pizzas[0];
        Assert.That(ingredients, Is.EquivalentTo(Merge(theHams)));

        // Assert delivery desk and bake log are in sync
        Assert.That(pizza, Is.EqualTo(deliveredPizza));
        Assert.That(pizza, Is.TypeOf<InvalidPizza>());
        Assert.That(pizza as InvalidPizza, Has.Property("MissingIngredients").EquivalentTo(new List<IngredientType>() { IngredientType.Shrimp }));
    }
    #endregion

    #region multiple of same ingredient
    [Test]
    public async Task OneOrderWithOneExtraPizzaIsBaked()
    {
        var expectdPizzaType = PizzaTypes.FavoritenExtraMeat;
        var theHams = _application.AddIngredientToWareHouse(IngredientType.Ham, 1);
        var theKebabMeats = _application.AddIngredientToWareHouse(IngredientType.KebabMeat, 2);
        var theFriggitellos = _application.AddIngredientToWareHouse(IngredientType.Friggitello, 1);

        _application.CreateDefaultClient();

        (OrderQueue orderQueue, DeliveryDesk deliveryDesk) = GetServices();

        // Act
        var order = orderQueue.EnqueueOrder(expectdPizzaType, 1);
        await _application.WaitForDeliveryDesk();

        // Asserts
        Assume.That(orderQueue.Queue, Is.Empty);
        Assume.That(deliveryDesk.FinishedOrders, Has.Count.EqualTo(1));

        var deliveredPizza = AssertDeliveryDesk(deliveryDesk, order.Id, expectdPizzaType);

        // Assert: bake log looks OK
        Assert.That(_application.BakeLog.Pizzas, Has.Count.EqualTo(1));
        var (pizza, ingredients) = _application.BakeLog.Pizzas[0];
        Assert.That(ingredients, Is.EquivalentTo(theHams.Concat(theKebabMeats).Concat(theFriggitellos)));

        // Assert delivery desk and bake log are in sync
        Assert.That(pizza, Is.EqualTo(deliveredPizza));
    }

    [Test]
    public async Task OneOrderWithOneExtraEverythingPizzaIsBaked()
    {
        var expectdPizzaType = PizzaTypes.NapoliExtraAll;
        var theHams = _application.AddIngredientToWareHouse(IngredientType.Ham, 2);
        var theShrimps = _application.AddIngredientToWareHouse(IngredientType.Shrimp, 2);
        var thePineapples = _application.AddIngredientToWareHouse(IngredientType.Pineapple, 2);

        _application.CreateDefaultClient();

        (OrderQueue orderQueue, DeliveryDesk deliveryDesk) = GetServices();

        // Act
        var order = orderQueue.EnqueueOrder(expectdPizzaType, 1);
        await _application.WaitForDeliveryDesk();

        // Asserts
        Assume.That(orderQueue.Queue, Is.Empty);
        Assume.That(deliveryDesk.FinishedOrders, Has.Count.EqualTo(1));

        var deliveredPizza = AssertDeliveryDesk(deliveryDesk, order.Id, expectdPizzaType);

        // Assert: bake log looks OK
        Assert.That(_application.BakeLog.Pizzas, Has.Count.EqualTo(1));
        var (pizza, ingredients) = _application.BakeLog.Pizzas[0];
        Assert.That(ingredients, Is.EquivalentTo(theHams.Concat(theShrimps).Concat(thePineapples)));

        // Assert delivery desk and bake log are in sync
        Assert.That(pizza, Is.EqualTo(deliveredPizza));
    }

    public async Task OneOrderWithTwoPizzasOneExtraIsBaked()
    {
        var expectdPizzaType1 = PizzaTypes.FavoritenExtraMeat;
        var expectdPizzaType2 = PizzaTypes.Favoriten;
        var theHams = _application.AddIngredientToWareHouse(IngredientType.Ham, 2);
        var theKebabMeats = _application.AddIngredientToWareHouse(IngredientType.KebabMeat, 3);
        var theFriggitellos = _application.AddIngredientToWareHouse(IngredientType.Friggitello, 2);

        _application.CreateDefaultClient();

        (OrderQueue orderQueue, DeliveryDesk deliveryDesk) = GetServices();

        // Act
        var order = orderQueue.EnqueueOrder((expectdPizzaType1, 1), (expectdPizzaType2, 1));
        await _application.WaitForDeliveryDesk();

        // Asserts
        Assume.That(orderQueue.Queue, Is.Empty);
        Assume.That(deliveryDesk.FinishedOrders, Has.Count.EqualTo(1));

        var (deliveredPizza1, deliveredPizza2) = AssertDeliveryDesk(deliveryDesk, order.Id, expectdPizzaType1, expectdPizzaType2);

        // Assert: bake log looks OK
        Assert.That(_application.BakeLog.Pizzas, Has.Count.EqualTo(2));
        var actualIngredients = _application.BakeLog.Pizzas.SelectMany(p => p.ingredients);
        var expectedIngredients = Merge(theHams, theKebabMeats, theFriggitellos);
        Assert.That(actualIngredients, Is.EquivalentTo(expectedIngredients));

        // Assert: delivery desk + bake log
        var bakeLogPizzas = _application.BakeLog.Pizzas.Select((p) => p.pizza);
        var deliveryDeskPizzas = ImmutableList.Create(deliveredPizza1, deliveredPizza2);
        Assert.That(bakeLogPizzas, Is.EquivalentTo(deliveryDeskPizzas));
    }
    #endregion

    #region helper methods
    private IEnumerable<T> Merge<T>(params IEnumerable<T>[] parts) => parts.SelectMany(x => x);

    private (OrderQueue orderQueue, DeliveryDesk deliveryDesk) GetServices()
    {
        var orderQueue = _application.Services.GetRequiredService<OrderQueue>();
        var deliveryDesk = _application.Services.GetRequiredService<DeliveryDesk>();

        Assume.That(orderQueue.Queue, Is.Empty);
        Assume.That(deliveryDesk.FinishedOrders, Is.Empty);

        return (orderQueue, deliveryDesk);
    }

    //private List<Ingredient> AddIngredientToWareHouse(IngredientType type, int quantity)
    //{
    //    List<Ingredient> result = Enumerable.Range(1, quantity)
    //        .Select(_ => new Ingredient(type, Guid.NewGuid()))
    //        .ToList();

    //    _application.WarehouseTestData.TestData.Add(type, result.ToStack());

    //    return result;
    //}

    private Pizza AssertDeliveryDesk(DeliveryDesk deliveryDesk, int orderId, PizzaType expectdPizzaType)
    {
        Assert.That(deliveryDesk.FinishedOrders, Does.ContainKey(orderId));
        var deliveredOrder = deliveryDesk.FinishedOrders[orderId];
        Assert.That(deliveredOrder, Has.Count.EqualTo(1));
        var deliveredPizza = deliveredOrder[0];
        Assert.That(deliveredPizza, Has.Property(nameof(Pizza.Name)).EqualTo(expectdPizzaType));
        return deliveredPizza;
    }

    private (Pizza, Pizza) AssertDeliveryDesk(DeliveryDesk deliveryDesk, int orderId, PizzaType expectdPizzaType1, PizzaType expectdPizzaType2)
    {
        Assert.That(deliveryDesk.FinishedOrders, Does.ContainKey(orderId));
        var deliveredOrder = deliveryDesk.FinishedOrders[orderId];
        Assert.That(deliveredOrder, Has.Count.EqualTo(2));
        var deliveredPizza1 = deliveredOrder[0];
        var deliveredPizza2 = deliveredOrder[1];
        Assert.That(deliveredPizza1, Has.Property(nameof(Pizza.Name)).EqualTo(expectdPizzaType1));
        Assert.That(deliveredPizza2, Has.Property(nameof(Pizza.Name)).EqualTo(expectdPizzaType2));
        return (deliveredPizza1, deliveredPizza2);
    }

    private (Pizza, Pizza) AssertDeliveryDeskTwoOrders(DeliveryDesk deliveryDesk, int order1Id, int order2Id, PizzaType expectdPizzaType1, PizzaType expectdPizzaType2)
    {
        Assert.Multiple(() => {
            Assert.That(deliveryDesk.FinishedOrders, Does.ContainKey(order1Id));
            Assert.That(deliveryDesk.FinishedOrders, Does.ContainKey(order2Id));
        });
        var deliveredOrder1 = deliveryDesk.FinishedOrders[order1Id];
        var deliveredOrder2 = deliveryDesk.FinishedOrders[order2Id];

        Assert.Multiple(() =>
        {
            Assert.That(deliveredOrder1, Has.Count.EqualTo(1));
            Assert.That(deliveredOrder2, Has.Count.EqualTo(1));
        });
        
        var deliveredPizza1 = deliveredOrder1[0];
        var deliveredPizza2 = deliveredOrder2[0];

        Assert.That(deliveredPizza1, Has.Property(nameof(Pizza.Name)).EqualTo(expectdPizzaType1));
        Assert.That(deliveredPizza2, Has.Property(nameof(Pizza.Name)).EqualTo(expectdPizzaType2));
        return (deliveredPizza1, deliveredPizza2);
    }
    #endregion
}
