using System.Net;
using System.Net.Http.Headers;
using ExamContext;
using Microsoft.Extensions.DependencyInjection;

namespace BackendExam.Tests.Manager;

internal class ManagerTests
{
    private BackendExamApplication _application;

    [SetUp]
    public void Setup()
    {
        _application = new BackendExamApplication();
        _application.AddIngredientToWareHouse(IngredientType.Ham, 0);
        _application.AddIngredientToWareHouse(IngredientType.Mushroom, 0);
        _application.UserRepositoryTestData.TestUsers.Add(new User("asd2", "Olle", "myPassword2", new List<Role>() { new Role("Manager") }));
        _application.UserRepositoryTestData.TestUsers.Add(new User("asd3", "Lisa", "myPassword3", new List<Role>() { new Role("Employee") }));
    }

    [Test]
    public async Task ManagerCanAddIngredient()
    {
        var httpClient = _application.CreateDefaultClient();

        var loginResponse =
            await httpClient.PostAsJsonAsync("/login", new Login() { UserName = "Olle", Password = "myPassword2" });

        var responseContent = await loginResponse.Content.ReadAsAsync<Dictionary<string, string>>();

        var jwt = responseContent["value"];

        Assert.That(responseContent, Does.ContainKey("value"));
        Assert.That(responseContent["value"], Does.StartWith("ey"));
        Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var ingredientDto = new AddIngredients(IngredientType.Ham, Guid.NewGuid());
        var response = await httpClient.PostAsJsonAsync("/restaurant/add-ingredients", ingredientDto);

        var warehouse = _application.Services.GetRequiredService<Warehouse>();
        var hamList = warehouse.PeekIngredient(IngredientType.Ham);
        Assert.That(hamList.Count, Is.EqualTo(1)); 
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));

    }

    [Test]
    public async Task NonManagerCanNotAddIngredients()
    {
        var httpClient = _application.CreateDefaultClient();

        var loginResponse =
            await httpClient.PostAsJsonAsync("/login", new Login() { UserName = "Lisa", Password = "myPassword3" });

        var responseContent = await loginResponse.Content.ReadAsAsync<Dictionary<string, string>>();

        var jwt = responseContent["value"];

        Assert.That(responseContent, Does.ContainKey("value"));
        Assert.That(responseContent["value"], Does.StartWith("ey"));
        Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var ingredientDto = new AddIngredients(IngredientType.Ham, Guid.NewGuid());
        var response = await httpClient.PostAsJsonAsync("/restaurant/add-ingredients", ingredientDto);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task ManagerCanNotAddNonExistingIngredient()
    {
        var httpClient = _application.CreateDefaultClient();

        var loginResponse =
            await httpClient.PostAsJsonAsync("/login", new Login() { UserName = "Olle", Password = "myPassword2" });

        var responseContent = await loginResponse.Content.ReadAsAsync<Dictionary<string, string>>();

        var jwt = responseContent["value"];

        Assert.That(responseContent, Does.ContainKey("value"));
        Assert.That(responseContent["value"], Does.StartWith("ey"));
        Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var ingredientDto = new AddIngredients(IngredientType.TomatoSauce, Guid.NewGuid());
        var response = await httpClient.PostAsJsonAsync("/restaurant/add-ingredients", ingredientDto);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    record AddIngredients(IngredientType IngredientType, Guid Id);
}