using System.Net;
using System.Net.Http.Headers;
using ExamContext;
using Microsoft.Extensions.DependencyInjection;

namespace BackendExam.Tests.Employee;

public class TimeClockTests
{
    private BackendExamApplication _application;

    private User _testUser1 = new User("asd", "Linus", "myPassword", new List<Role>() { new Role("Employee") });

    [SetUp]
    public void Setup()
    {
        _application = new BackendExamApplication();
        _application.UserRepositoryTestData.TestUsers.Add(_testUser1);
        _application.UserRepositoryTestData.TestUsers.Add(new User("asd2", "Olle", "myPassword2", new List<Role>() { new Role("Manager") }));
        _application.UserRepositoryTestData.TestUsers.Add(new User("asd3", "Lisa", "myPassword3", new List<Role>() { new Role("Employee") }));
    }

    [Test]
    public async Task EnteringEmployeesRegisterInTimeClock()
    {
        var httpClient = _application.CreateDefaultClient();

        var loginResponse =
            await httpClient.PostAsJsonAsync("/login", new Login() { UserName = "Linus", Password = "myPassword" });

        var responseContent = await loginResponse.Content.ReadAsAsync<Dictionary<string, string>>();

        var jwt = responseContent["value"];

        Assert.That(responseContent, Does.ContainKey("value"));
        Assert.That(responseContent["value"], Does.StartWith("ey"));
        Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));


        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        var response = await httpClient.PostAsync("/restaurant/enter", null);

        var timeClock = _application.Services.GetRequiredService<TimeClock>();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
        Assert.That(timeClock.GetUsers(), Is.EquivalentTo(new List<User>() { _testUser1 }));
    }

    [Test]
    public async Task EnteringManagerCanNotRegisterInTimeClock()
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
        var response = await httpClient.PostAsync("/restaurant/enter", null);


        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task EmployeeCanLeave()
    {
        var timeClock = _application.Services.GetRequiredService<TimeClock>();
        _application.TimeClockTestData.TestUsers.Add(new User("asd3", "Lisa", "myPassword3", new List<Role>() { new Role("Employee") }));
        var httpClient = _application.CreateDefaultClient();

        var loginResponse =
            await httpClient.PostAsJsonAsync("/login", new Login() { UserName = "Lisa", Password = "myPassword3" });

        var responseContent = await loginResponse.Content.ReadAsAsync<Dictionary<string, string>>();

        var jwt = responseContent["value"];

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        var response = await httpClient.PostAsync("/restaurant/leave", null);

        
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
        Assert.That(timeClock.GetUsers().Count, Is.EqualTo(0));
    }

    [Test]
    public async Task ManyEmployeesRegisterInTimeClock()
    {
        var httpClient = _application.CreateDefaultClient();

        var loginResponse =
            await httpClient.PostAsJsonAsync("/login", new Login() { UserName = "Linus", Password = "myPassword" });

        var responseContent = await loginResponse.Content.ReadAsAsync<Dictionary<string, string>>();

        var jwt = responseContent["value"];

        Assert.That(responseContent, Does.ContainKey("value"));
        Assert.That(responseContent["value"], Does.StartWith("ey"));
        Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));


        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var tasks = new List<Task<HttpStatusCode>>();
        for (int i = 1; i <= 100; i++)
        {
            var task = Task.Run(async () =>
            {
                Thread.Sleep(100); // not delay to make sure that as many threads as possible is running
                var response = await httpClient.PostAsync("/restaurant/enter", null);
                return response.StatusCode;
            });
            tasks.Add(task);
        }
        await Task.WhenAll(tasks);

        Assert.That(tasks.Where(task => task.Result == HttpStatusCode.Accepted).ToList(), Has.Count.EqualTo(1));
        Assert.That(tasks.Select((task) => task.Result).Where((s) => s != HttpStatusCode.Accepted), Has.All.EqualTo(HttpStatusCode.BadRequest));
        var timeClock = _application.Services.GetRequiredService<TimeClock>();
        Assert.That(timeClock.GetUsers(), Is.EquivalentTo(new List<User>() { _testUser1 }));
    }

    [Test]
    public async Task ManyEmployeesLeaveTimeClock()
    {

        _application.TimeClockTestData.TestUsers.Add(_testUser1);
        var httpClient = _application.CreateDefaultClient();

        var loginResponse =
            await httpClient.PostAsJsonAsync("/login", new Login() { UserName = "Linus", Password = "myPassword" });

        var responseContent = await loginResponse.Content.ReadAsAsync<Dictionary<string, string>>();

        var jwt = responseContent["value"];

        Assert.That(responseContent, Does.ContainKey("value"));
        Assert.That(responseContent["value"], Does.StartWith("ey"));
        Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));


        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var tasks = new List<Task<HttpStatusCode>>();
        for (int i = 1; i <= 100; i++)
        {
            var task = Task.Run(async () =>
            {
                Thread.Sleep(100); // not delay to make sure that as many threads as possible is running
                var response = await httpClient.PostAsync("/restaurant/leave", null);
                return response.StatusCode;
            });
            tasks.Add(task);
        }
        await Task.WhenAll(tasks);

        Assert.That(tasks.Where(task => task.Result == HttpStatusCode.Accepted).ToList(), Has.Count.EqualTo(1));
        Assert.That(tasks.Select((task) => task.Result).Where((s) => s != HttpStatusCode.Accepted), Has.All.EqualTo(HttpStatusCode.BadRequest));
        var timeClock = _application.Services.GetRequiredService<TimeClock>();
        Assert.That(timeClock.GetUsers(), Is.Empty);
    }
}