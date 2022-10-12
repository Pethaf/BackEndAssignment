using Microsoft.Extensions.DependencyInjection;
using ExamContext.Chef;

namespace BackendExam.Tests.Chef;

public class ThreadTests
{
    private BackendExamApplication _application;

    [SetUp]
    public void Setup()
    {
        _application = new BackendExamApplication();
    }

    [Test]
    public async Task Starts()
    {
        _application.ChefManagerSettings.StartChefs = true;
        _application.ChefManagerSettings.NumberOfChefs = 1;

        _application.CreateDefaultClient();
        await Task.Delay(200); // wait a bit for thread to start

        var service = _application.Services.GetService<ChefManager>();

        Assert.That(service?.Chefs, Has.Count.EqualTo(1));
        var chefData = service?.Chefs.First();
        Assert.That(chefData, Is.Not.Null);
        Assert.That(chefData.Value.running, Is.EqualTo(true));
    }

    [Test]
    public async Task StartsMany()
    {
        _application.ChefManagerSettings.StartChefs = true;
        _application.ChefManagerSettings.NumberOfChefs = 3;

        _application.CreateDefaultClient();
        await Task.Delay(200); // wait a bit for thread to start

        var service = _application.Services.GetService<ChefManager>();

        Assert.That(service?.Chefs, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            foreach (var chefData in service?.Chefs)
            {
                Assert.That(chefData.running, Is.EqualTo(true));
            }
        });
    }
}
