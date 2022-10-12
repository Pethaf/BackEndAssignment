using BackendExam.Tests.CookbookExtensions;
using BackendExam.Tests.OrderQueueExtensions;
using BackendExam.Tests.PlaceOrder;
using ExamContext;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Net;

namespace BackendExam.Tests.CompleteOrder;

internal class MultiThreadingTests
{
    private BackendExamApplication _application;

    [SetUp]
    public void Setup()
    {
        _application = new BackendExamApplication();
        _application.ChefManagerSettings.StartChefs = true;
        _application.CookbookTestData.TestData.Add(PizzaTypes.Amigo);
    }

    [Test]
    public async Task LoadsOfChefsGettingStuffFromTheWarehouse()
    {
        _application.ChefManagerSettings.NumberOfChefs = 10;
        _application.JobDurations.WarehouseWork = 10;
        AddAbundantIngredients();

        _application.CreateDefaultClient();
        var orderQueue = _application.Services.GetRequiredService<OrderQueue>();

        for (int i = 0; i < 100; i++)
        {
            orderQueue.EnqueueOrder((PizzaTypes.Amigo, 1));
        }
        await _application.WaitForDeliveryDesk(100, 60);

        // TODO assert no expcetions
        DeliveryDesk desk = _application.Services.GetRequiredService<DeliveryDesk>();
        Assert.That(desk.FinishedOrders, Has.Count.EqualTo(100));
    }

    [Test]
    public async Task ManyPizzas()
    {
        _application.ChefManagerSettings.NumberOfChefs = 1;
        _application.JobDurations.WarehouseWork = 10;
        AddAbundantIngredients();

        _application.CreateDefaultClient();
        var orderQueue = _application.Services.GetRequiredService<OrderQueue>();

        Order placedOrder = orderQueue.EnqueueOrder((PizzaTypes.Amigo, 10));
        bool completedOnTime = await _application.WaitForDeliveryDesk(1, 10);

        Assert.That(completedOnTime, Is.True);
        DeliveryDesk desk = _application.Services.GetRequiredService<DeliveryDesk>();
        Assert.That(desk.FinishedOrders, Has.Count.EqualTo(1));
        var pizzas = desk.FinishedOrders[placedOrder.Id];
        Assert.That(pizzas, Has.All.Property(nameof(Pizza.Name)).EqualTo(PizzaTypes.Amigo));
    }

    [Test]
    public async Task SlowOrdersManyChefs()
    {
        _application.ChefManagerSettings.NumberOfChefs = 10;
        _application.JobDurations.WarehouseWork = 10;
        AddAbundantIngredients();

        _application.CreateDefaultClient();
        var orderQueue = _application.Services.GetRequiredService<OrderQueue>();

        await Task.Delay(100); // wait for chefs to start up
        int orderCount = 5;
        var list = new List<Order>();
        for (int i = 0; i < orderCount; i++)
        {
            Order placedOrder = orderQueue.EnqueueOrder((PizzaTypes.Amigo, 1));
            list.Add(placedOrder);
            await Task.Delay(100);
        }
        bool completedOnTime = await _application.WaitForDeliveryDesk(orderCount, 20);

        Assert.That(completedOnTime, Is.True);
        DeliveryDesk desk = _application.Services.GetRequiredService<DeliveryDesk>();

        Assert.That(desk.FinishedOrders, Has.Count.EqualTo(orderCount));
        Assert.That(desk.FinishedOrders.Keys.ToList(), Is.EquivalentTo(list.Select(order => order.Id)));
        var pizzas = desk.FinishedOrders.SelectMany(order => order.Value);
        Assert.That(pizzas, Has.All.Property(nameof(Pizza.Name)).EqualTo(PizzaTypes.Amigo));
    }

    [Test]
    public async Task HandleManyOrders()
    {
        var pizzaType = PizzaTypes.LaBussola;
        _application.MenuTestData.TestData.Add(
            new MenuItem(pizzaType, 59.99)
        );
        _application.ChefManagerSettings.StartChefs = false;
        _application.ChefManagerSettings.NumberOfChefs = 0;

        var httpClient = _application.CreateDefaultClient();
        var orderQueue = _application.Services.GetRequiredService<OrderQueue>();

        var tasks = new List<Task<HttpStatusCode>>();
        for (int i = 1; i <= 100; i++)
        {
            var task = Task.Run(async () =>
            {
                Thread.Sleep(100); // not delay to make sure that as many threads as possible is running
                var createResponse = await httpClient.PostAsJsonAsync("/order", new List<string>() { pizzaType.Name });
                return createResponse.StatusCode;
            });
            tasks.Add(task);
        }
        await Task.WhenAll(tasks);

        Assert.That(tasks.Where(task => task.Result == HttpStatusCode.Created).ToList(), Has.Count.EqualTo(100));
        Assert.That(orderQueue.Queue, Has.Count.EqualTo(100));
        Assert.That(orderQueue.Queue, Has.All.Property("Pizzas").ItemAt(0).EqualTo((pizzaType, 1)));
    }

    [Test]
    public async Task OnlyOneRetrieveGetsTheOrder()
    {
        _application.ChefManagerSettings.StartChefs = false;
        _application.ChefManagerSettings.NumberOfChefs = 0;
        var pizzaName = PizzaTypes.Bambino;
        var expectedPizza = new Pizza(pizzaName, Guid.NewGuid());
        var orderId = 123;

        var httpClient = _application.CreateDefaultClient();

        var deliveryDesk = _application.Services.GetRequiredService<DeliveryDesk>();
        deliveryDesk.FinishedOrders.Add(
            orderId,
            new List<Pizza>() { expectedPizza }
        );

        var tasks = new List<Task<HttpStatusCode>>();
        for (int i = 1; i <= 100; i++)
        {
            var task = Task.Run(async () =>
            {
                Thread.Sleep(100); // not delay to make sure that as many threads as possible is running
                var response = await httpClient.GetAsync($"/order/{orderId}");
                return response.StatusCode;
            });
            tasks.Add(task);
        }
        await Task.WhenAll(tasks);

        Assert.That(tasks.Where(task => task.Result == HttpStatusCode.OK).ToList(), Has.Count.EqualTo(1));
        Assert.That(tasks.Where(task => task.Result == HttpStatusCode.NotFound).ToList(), Has.Count.EqualTo(99));
    }

    private void AddAbundantIngredients()
    {
        _application.WarehouseTestData.TestData.Add(IngredientType.MincedMeat, MakeIngredients(IngredientType.MincedMeat, 100));
        _application.WarehouseTestData.TestData.Add(IngredientType.RedOnion, MakeIngredients(IngredientType.RedOnion, 100));
        _application.WarehouseTestData.TestData.Add(IngredientType.Mushroom, MakeIngredients(IngredientType.Mushroom, 100));
        _application.WarehouseTestData.TestData.Add(IngredientType.Pepper, MakeIngredients(IngredientType.Pepper, 100));
    }

    private Stack<Ingredient> MakeIngredients(IngredientType type, int count)
    {
        return new Stack<Ingredient>(
            Enumerable
                .Range(1, count)
                .Select(_ => new Ingredient(type, Guid.NewGuid()))
            );
    }
}
