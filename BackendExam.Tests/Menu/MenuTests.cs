using System.Net;
using ExamContext;

namespace BackendExam.Tests.Menu;

internal class MenuTests
{
    private BackendExamApplication _application;

    [SetUp]
    public void Setup()
    {
        _application = new BackendExamApplication();
    }

    [Test]
    public async Task EmptyMenu()
    {
        var httpClient = _application.CreateDefaultClient();

        var response = await httpClient.GetAsync("/menu");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        List<string> pizzaList = await response.Content.ReadAsAsync<List<string>>();
        Assert.That(pizzaList, Is.Empty);
    }

    [Test]
    public async Task FilledMenu()
    {
        
        var testData = new List<MenuItem>()
        {
            new MenuItem(new PizzaType("Vesuvio"), 59.99),
            new MenuItem(new PizzaType("Capricciosa"), 59.99),
            new MenuItem(PizzaTypes.Favoriten, 69.99)
        };
        _application.MenuTestData.TestData.AddRange(testData);
        var httpClient = _application.CreateDefaultClient();


        var response = await httpClient.GetAsync("/menu");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var pizzaList = await response.Content.ReadAsAsync<List<Dictionary<string,object>>>();
        var expectedData = testData.Select(d =>
        {
            var Dict = new Dictionary<string, object>();
            Dict.Add("name", d.Name.Name);
            Dict.Add("price", d.Price);
            return Dict;
        }).ToList();
        Assert.That(pizzaList, Is.EquivalentTo(expectedData));
    }
}
