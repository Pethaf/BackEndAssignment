using BackendExam.Tests.PlaceOrder;
using ExamContext;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Net;

namespace BackendExam.Tests.CompleteOrder;

public class RetrieveTests
{
    private BackendExamApplication _application;

    [SetUp]
    public void Setup()
    {
        _application = new BackendExamApplication();
        _application.ChefManagerSettings.StartChefs = false;
        _application.ChefManagerSettings.NumberOfChefs = 0;
    }

    [Test]
    public async Task CheckStatusOneOrderOnePizzaDone()
    {
        var pizzaName = PizzaTypes.Hawaii.Name;
        var expectedPizza = new Pizza(new PizzaType(pizzaName), Guid.NewGuid());
        var orderId = 123;
        var httpClient = _application.CreateDefaultClient();
        var deliveryDesk = _application.Services.GetRequiredService<DeliveryDesk>();
        deliveryDesk.FinishedOrders.Add(
            orderId,
            new List<Pizza>() { expectedPizza }
        );

        var response = await httpClient.GetAsync($"/order/{orderId}");

        Assert.That(response, Has.Property("StatusCode").EqualTo(HttpStatusCode.OK));
        var data = await response.Content.ReadAsAsync<Dictionary<string, object>>();
        Assert.That(data, Does.ContainKey("status").WithValue("done"));
        Assert.That(data, Does.ContainKey("order"));
        Assert.That(data["order"], Is.TypeOf(typeof(JArray)));
        var pizzaArray = data["order"] as JArray;
        Assert.That(pizzaArray, Has.Count.EqualTo(1));
        var thePizza = pizzaArray[0].ToObject<Dictionary<string, string>>();
        Assert.That(thePizza, Does.ContainKey("id").WithValue(expectedPizza.Id.ToString()));
        Assert.That(thePizza, Does.ContainKey("type").WithValue(expectedPizza.Name.Name));
    }

    [Test]
    public async Task CheckFetchedOrderIsRemoved()
    {
        var pizzaName = PizzaTypes.Hawaii.Name;
        var expectedPizza = new Pizza(new PizzaType(pizzaName), Guid.NewGuid());
        var orderId = 123;
        var httpClient = _application.CreateDefaultClient();
        var deliveryDesk = _application.Services.GetRequiredService<DeliveryDesk>();
        deliveryDesk.FinishedOrders.Add(
            orderId,
            new List<Pizza>() { expectedPizza }
        );

        var response1 = await httpClient.GetAsync($"/order/{orderId}");
        Assume.That(response1, Has.Property("StatusCode").EqualTo(HttpStatusCode.OK));
        var data = await response1.Content.ReadAsAsync<Dictionary<string, object>>();
        Assume.That(data, Does.ContainKey("status").WithValue("done"));

        var response2 = await httpClient.GetAsync($"/order/{orderId}");
        Assert.That(response2, Has.Property("StatusCode").EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task CheckNonFetchedOrdersAreKept()
    {
        var pizzaName1 = PizzaTypes.Hawaii.Name;
        var pizzaName2 = PizzaTypes.Bambino.Name;
        var expectedPizza1 = new Pizza(new PizzaType(pizzaName1), Guid.NewGuid());
        var expectedPizza2 = new Pizza(new PizzaType(pizzaName2), Guid.NewGuid());
        var orderId1 = 123;
        var orderId2 = 321;
        var httpClient = _application.CreateDefaultClient();
        var deliveryDesk = _application.Services.GetRequiredService<DeliveryDesk>();
        deliveryDesk.FinishedOrders.Add(
            orderId1,
            new List<Pizza>() { expectedPizza1 }
        );
        deliveryDesk.FinishedOrders.Add(
            orderId2,
            new List<Pizza>() { expectedPizza2 }
        );

        var response1 = await httpClient.GetAsync($"/order/{orderId1}");
        Assume.That(response1, Has.Property("StatusCode").EqualTo(HttpStatusCode.OK));
        var data = await response1.Content.ReadAsAsync<Dictionary<string, object>>();
        Assume.That(data, Does.ContainKey("status").WithValue("done"));

        Assert.That(deliveryDesk.FinishedOrders, Does.ContainKey(orderId2).WithValue(new List<Pizza>() { expectedPizza2 }));
    }
}
