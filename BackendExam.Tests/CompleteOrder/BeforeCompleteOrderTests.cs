using ExamContext;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace BackendExam.Tests.CompleteOrder;

public class BeforeCompleteOrderTests
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
    public async Task CheckStatusBeforeDone()
    {
        var pizzaName = PizzaTypes.Hawaii.Name;
        _application.MenuTestData.TestData.Add(
            new MenuItem(new PizzaType(pizzaName), 109.99)
        );
        var expectedResponse = new Dictionary<string, string?>();
        expectedResponse.Add("status", "not-available");
        expectedResponse.Add("order", null);
        var httpClient = _application.CreateDefaultClient();
        var orderQueue = _application.Services.GetRequiredService<OrderQueue>();
        var createResponse = await httpClient.PostAsJsonAsync("/order", new List<string>()
        {
            pizzaName
        });
        Assume.That(orderQueue.Queue, Has.Count.EqualTo(1));
        var orderLocation = createResponse.Headers.Location;

        var response = await httpClient.GetAsync(orderLocation);

        Assert.That(response, Has.Property("StatusCode").EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task CheckStatusNonExisting()
    {
        var httpClient = _application.CreateDefaultClient();

        var response = await httpClient.GetAsync("/order/12345");

        Assert.That(response, Has.Property("StatusCode").EqualTo(HttpStatusCode.NotFound));
    }
}
