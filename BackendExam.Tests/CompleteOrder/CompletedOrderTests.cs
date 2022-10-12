using BackendExam.Tests.CookbookExtensions;
using ExamContext;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Net;

namespace BackendExam.Tests.CompleteOrder;

public class CompletedOrderTests
{
    private BackendExamApplication _application;

    [SetUp]
    public void Setup()
    {
        _application = new BackendExamApplication();
        _application.ChefManagerSettings.StartChefs = true;
        _application.ChefManagerSettings.NumberOfChefs = 1;
    }

    [Test]
    public async Task OneOrderPizzaDone()
    {
        var pizzaType = PizzaTypes.Hawaii;
        _application.CookbookTestData.TestData.Add(pizzaType);
        _application.MenuTestData.TestData.Add(new MenuItem(pizzaType, 123.45));
        var theHams = _application.AddIngredientToWareHouse(IngredientType.Ham, 1);
        var thePineapples = _application.AddIngredientToWareHouse(IngredientType.Pineapple, 1);
        var pizzaName = pizzaType.Name;

        var httpClient = _application.CreateDefaultClient();
        var deliveryDesk = _application.Services.GetRequiredService<DeliveryDesk>();
        var createResponse = await httpClient.PostAsJsonAsync("/order", new List<string>()
        {
            pizzaName
        });
        var orderLocation = createResponse.Headers.Location?.OriginalString;
        var success = await _application.WaitForDeliveryDesk();

        if (!success)
        {
            Assert.Fail("No finished orders were put in the delivery desk within a reasonable time.");
        }

        // Assert delivery queue after order is done
        var orderId = int.Parse(orderLocation![7..]); // TODO might need to adjust the number
        Assert.That(deliveryDesk.FinishedOrders, Has.Count.EqualTo(1));
        Assert.That(deliveryDesk.FinishedOrders, Does.ContainKey(orderId));
        var pizzaList = deliveryDesk.FinishedOrders[orderId];
        Assert.Multiple(() =>
        {
            Assert.That(pizzaList, Has.ItemAt(0).Property(nameof(Pizza.Name)).EqualTo(pizzaType));
            Assert.That(pizzaList, Has.Count.EqualTo(1));
        });

        // Assert bake log
        Assert.That(_application.BakeLog.Pizzas, Has.Count.EqualTo(1));
        Pizza? expectedPizza = null;
        Assert.Multiple(() =>
        {
            var (pizza, ingredients) = _application.BakeLog.Pizzas[0];
            Assert.That(pizza, Has.Property(nameof(Pizza.Name)).EqualTo(pizzaType));
            Assert.That(ingredients, Is.EquivalentTo(theHams.Concat(thePineapples)));
            expectedPizza = pizza;
        });

        // Remove pizza from delivery desk when retrieved
        var response = await httpClient.GetAsync(orderLocation);
        Assert.That(response, Has.Property("StatusCode").EqualTo(HttpStatusCode.OK));
        Assert.That(deliveryDesk.FinishedOrders, Has.Count.EqualTo(0));

        // Assert delivered pizza response
        var data = await response.Content.ReadAsAsync<Dictionary<string, object>>();
        Assert.That(data, Does.ContainKey("status").WithValue("done"));
        Assert.That(data, Does.ContainKey("order"));
        Assert.That(data["order"], Is.TypeOf(typeof(JArray)));
        var pizzaArray = data["order"] as JArray;
        Assert.That(pizzaArray, Has.Count.EqualTo(1));
        var thePizza = pizzaArray[0].ToObject<Dictionary<string, string>>();
        Assert.That(thePizza, Does.ContainKey("id").WithValue(expectedPizza!.Id.ToString()));
        Assert.That(thePizza, Does.ContainKey("type").WithValue(expectedPizza.Name.Name));


    }
}
