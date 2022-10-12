using ExamContext.TestData;
using System.Collections.Concurrent;

namespace ExamContext.Chef;

public class ChefManager : IDisposable
{
    private readonly IChefFactory? _factory;
    private readonly IChefManagerSettings _settings;
    public ConcurrentBag<(string name, IChef chef, bool running)> Chefs { get; private set; }

    public ChefManager(IChefManagerSettings settings, IChefFactory? factory = null)
    {
        _factory = factory;
        _settings = settings;
        Chefs = new();
    }

    public void Dispose()
    {
        Console.WriteLine("Dispose was called"); // TODO
    }

    internal void StartChefs()
    {
        if (!_settings.StartChefs)
        {
            return;
        }

        if (_factory is null)
        {
            Console.WriteLine("No chef factory available!"); // TODO better error message
        }
        else
        {
            for (int i = 0; i < _settings.NumberOfChefs; i++)
            {
                IChef chef = _factory.CreateChef();
                var name = chef.Name;
                if (Chefs.Any(chef => chef.name == name))
                {
                    throw new Exception($"More than one chef has the name {name}. Chef names should be unique");
                }
                Chefs.Add((name, chef, true));
                var thread = new Thread(new ParameterizedThreadStart(RunChef))
                {
                    Name = name
                };
                thread.Start(chef);
            }
        }
    }

    private void RunChef(object? chefObject)
    {
        try
        {
            if (chefObject == null)
            {
                throw new Exception("Chef instance was null when starting thread");
            }
            IChef chef = (IChef)chefObject;
            chef.Run();
            Console.WriteLine($"The chef [{Thread.CurrentThread.Name}] running on thread {Environment.CurrentManagedThreadId} exited");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"The chef [{Thread.CurrentThread.Name}] running on thread {Environment.CurrentManagedThreadId} has crashed with error [{ex.GetType()}] message: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            var chef = Chefs.First(tuple => tuple.name == Thread.CurrentThread.Name);
            chef.running = false;
        }
    }
}
