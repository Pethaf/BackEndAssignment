using Microsoft.Extensions.DependencyInjection;
using System.Net;
using ExamContext;

namespace BackendExam.Tests.PlaceOrder;

public class PlaceOrderTests
{
    private BackendExamApplication _application;

    [SetUp]
    public void Setup()
    {
        _application = new BackendExamApplication();
        _application.ChefManagerSettings.StartChefs = false;
        _application.MenuTestData.TestData.AddRange(new List<MenuItem>()
        {
            new MenuItem(PizzaTypes.Hawaii, 109),
            new MenuItem(PizzaTypes.Parma, 119)
        });
    }


    [Test]
    public async Task EmptyOrder()
    {
        var httpClient = _application.CreateDefaultClient();

        var response = await httpClient.PostAsJsonAsync("/order", new List<string>());

        Assert.That(response, Has.Property("StatusCode").EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task UnknownPizzaOrder()
    {
        var httpClient = _application.CreateDefaultClient();

        var response = await httpClient.PostAsJsonAsync("/order", new List<string>()
        {
            "Made up pizza"
        });

        Assert.That(response, Has.Property("StatusCode").EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task SomeUnknownPizzaOrder()
    {
        var httpClient = _application.CreateDefaultClient();

        var response = await httpClient.PostAsJsonAsync("/order", new List<string>()
        {
            PizzaTypes.Hawaii.Name,
            "Made up pizza"
        });

        Assert.That(response, Has.Property("StatusCode").EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task SinglePizzaOrder()
    {
        var expectedPizzaName = PizzaTypes.Hawaii.Name;
        var httpClient = _application.CreateDefaultClient();

        var response = await httpClient.PostAsJsonAsync("/order", new List<string>()
        {
            expectedPizzaName
        });

        Assert.That(response, Has.Property("StatusCode").EqualTo(HttpStatusCode.Created));

        var orderQueue = _application.Services.GetService<OrderQueue>();
        Assert.That(orderQueue!.Queue, Has.Count.EqualTo(1));
        
        Order order = orderQueue!.Queue.Peek();
        var id = order.Id;
        Assert.That(response.Headers.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()?[0])["Location"] ?? "", Is.EqualTo($"/order/{id}"));
        
        Assert.That(order.Pizzas, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            var (pizzaType, quantity) = order.Pizzas[0];
            Assert.That(pizzaType.Name, Is.EqualTo(expectedPizzaName));
            Assert.That(quantity, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task TwoPizzaSameTypeOrder()
    {
        var expectedPizzaName = PizzaTypes.Hawaii.Name;
        var httpClient = _application.CreateDefaultClient();

        var response = await httpClient.PostAsJsonAsync("/order", new List<string>()
        {
            expectedPizzaName,
            expectedPizzaName
        });

        Assert.That(response, Has.Property("StatusCode").EqualTo(HttpStatusCode.Created));

        var orderQueue = _application.Services.GetService<OrderQueue>();
        Assert.That(orderQueue!.Queue, Has.Count.EqualTo(1));

        Order order = orderQueue!.Queue.Peek();
        var id = order.Id;
        Assert.That(response.Headers.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()?[0])["Location"] ?? "", Is.EqualTo($"/order/{id}"));

        Assert.That(order.Pizzas, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            var (pizzaType, quantity) = order.Pizzas[0];
            Assert.That(pizzaType.Name, Is.EqualTo(expectedPizzaName));
            Assert.That(quantity, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task TwoPizzaDifferntTypeOrder()
    {
        var expectedPizza1Name = PizzaTypes.Hawaii.Name;
        var expectedPizza2Name = PizzaTypes.Parma.Name;
        var httpClient = _application.CreateDefaultClient();

        var response = await httpClient.PostAsJsonAsync("/order", new List<string>()
        {
            expectedPizza1Name,
            expectedPizza2Name
        });

        Assert.That(response, Has.Property("StatusCode").EqualTo(HttpStatusCode.Created));

        var orderQueue = _application.Services.GetService<OrderQueue>();
        Assert.That(orderQueue!.Queue, Has.Count.EqualTo(1));

        Order order = orderQueue!.Queue.Peek();
        var id = order.Id;
        Assert.That(response.Headers.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()?[0])["Location"] ?? "", Is.EqualTo($"/order/{id}"));

        Assert.That(order.Pizzas, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            var pizzas = order.Pizzas.OrderBy(p => p.pizzaType.Name).ToList();
            var (pizza1Type, quantity1) = pizzas[0];
            Assert.That(pizza1Type.Name, Is.EqualTo(expectedPizza1Name));
            Assert.That(quantity1, Is.EqualTo(1));

            var (pizza2Type, quantity2) = pizzas[1];
            Assert.That(pizza2Type.Name, Is.EqualTo(expectedPizza2Name));
            Assert.That(quantity2, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ThreePizzaOrder()
    {
        var expectedPizza1Name = PizzaTypes.Hawaii.Name;
        var expectedPizza2Name = PizzaTypes.Parma.Name;
        var expectedPizza3Name = PizzaTypes.Parma.Name;
        var httpClient = _application.CreateDefaultClient();

        var response = await httpClient.PostAsJsonAsync("/order", new List<string>()
        {
            expectedPizza1Name,
            expectedPizza2Name,
            expectedPizza3Name
        });

        Assert.That(response, Has.Property("StatusCode").EqualTo(HttpStatusCode.Created));

        var orderQueue = _application.Services.GetService<OrderQueue>();
        Assert.That(orderQueue!.Queue, Has.Count.EqualTo(1));

        Order order = orderQueue!.Queue.Peek();
        var id = order.Id;
        Assert.That(response.Headers.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()?[0])["Location"] ?? "", Is.EqualTo($"/order/{id}"));

        Assert.That(order.Pizzas, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            var pizzas = order.Pizzas.OrderBy(p => p.pizzaType.Name).ToList();
            var (pizza1Type, quantity1) = pizzas[0];
            Assert.That(pizza1Type.Name, Is.EqualTo(expectedPizza1Name));
            Assert.That(quantity1, Is.EqualTo(1));

            var (pizza2Type, quantity2) = pizzas[1];
            Assert.That(pizza2Type.Name, Is.EqualTo(expectedPizza2Name));
            Assert.That(quantity2, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task OnePizzaTwoOrders()
    {
        var expectedPizzaName = PizzaTypes.Hawaii.Name;
        var httpClient = _application.CreateDefaultClient();
        var orderQueue = _application.Services.GetService<OrderQueue>();

        var response1 = await httpClient.PostAsJsonAsync("/order", new List<string>()
        {
            expectedPizzaName
        });

        Assert.That(response1, Has.Property("StatusCode").EqualTo(HttpStatusCode.Created));
        Assert.That(orderQueue!.Queue, Has.Count.EqualTo(1));

        Order order1 = orderQueue!.Queue.ToList()[0];
        var id1 = order1.Id;
        Assert.That(response1.Headers.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()?[0])["Location"] ?? "", Is.EqualTo($"/order/{id1}"));
        Assert.That(order1.Pizzas, Has.Count.EqualTo(1));

        var response2 = await httpClient.PostAsJsonAsync("/order", new List<string>()
        {
            expectedPizzaName
        });

        Assert.That(response2, Has.Property("StatusCode").EqualTo(HttpStatusCode.Created));
        Assert.That(orderQueue!.Queue, Has.Count.EqualTo(2));

        Order order2 = orderQueue!.Queue.ToList()[1];
        var id2 = order2.Id;
        Assert.That(response2.Headers.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()?[0])["Location"] ?? "", Is.EqualTo($"/order/{id2}"));
        Assert.That(order2.Pizzas, Has.Count.EqualTo(1));

        var allPizzas = orderQueue!.Queue.SelectMany(order => order.Pizzas).ToList();
        Assert.That(allPizzas, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            var (pizza1Type, quantity1) = allPizzas[0];
            Assert.That(pizza1Type.Name, Is.EqualTo(expectedPizzaName));
            Assert.That(quantity1, Is.EqualTo(1));

            var (pizza2Type, quantity2) = allPizzas[1];
            Assert.That(pizza2Type.Name, Is.EqualTo(expectedPizzaName));
            Assert.That(quantity2, Is.EqualTo(1));
        });
    }
}
