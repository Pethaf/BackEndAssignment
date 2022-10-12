using System.Net;
using ExamContext;

namespace BackendExam.Tests.Employee;

internal class EmployeeTests
{
    private BackendExamApplication _application;

    [SetUp]
    public void Setup()
    {
        _application = new BackendExamApplication();
        _application.UserRepositoryTestData.TestUsers.Add(new User("", "Linus", "myPassword", new List<Role>() {new Role("Employee")}));
        _application.UserRepositoryTestData.TestUsers.Add(new User("", "Olle", "myPassword2", new List<Role>() { new Role("Manager") }));
    }

    [Test]
    public async Task EmployeeCanLogin()
    {
        var httpClient = _application.CreateDefaultClient();

        var loginResponse =
            await httpClient.PostAsJsonAsync("/login", new LoginRequest("Linus", "myPassword"));

        var responseContent = await loginResponse.Content.ReadAsAsync<Dictionary<string, string>>();

        Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
        Assert.That(responseContent, Does.ContainKey("value"));
        Assert.That(responseContent["value"], Does.StartWith("ey"));
    }

    [Test]
    public async Task NonEmployeeCanNotLogin()
    {
        var httpClient = _application.CreateDefaultClient();

        var loginResponse =
            await httpClient.PostAsJsonAsync("/login", new LoginRequest("NonExistingUser", "myPassword"));
            //await httpClient.PostAsJsonAsync("/login", new Login() { UserName = "NonExistingUser", Password = "myPassword" });

        Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task EmployeeWithWrongPasswordCanNotLogin()
    {
        var httpClient = _application.CreateDefaultClient();

        var loginResponse =
            await httpClient.PostAsJsonAsync("/login", new LoginRequest("Linus", "myWrongPassword"));
            //await httpClient.PostAsJsonAsync("/login", new Login() { UserName = "Linus", Password = "myWrongPassword" });

        Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task EmployeeCanRegister()
    {
        var httpClient = _application.CreateDefaultClient();
        var requestObject = new Dictionary<string, object>();
        requestObject.Add("Username", "NewUser");
        requestObject.Add("Password", "password");
        requestObject.Add("Roles", new List<string>() { "Employee" });

        var registerResponse = 
            await httpClient.PostAsJsonAsync("/register", requestObject);

        Assert.That(registerResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(_application.UserRepositoryTestData.TestUsers.Any((testUser) => testUser.UserName == "NewUser"), Is.True);

    }

    record LoginRequest(string Username, string Password);
}