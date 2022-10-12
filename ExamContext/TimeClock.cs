using System.Collections.Immutable;
using ExamContext.LocalData;
using ExamContext.TestData;

namespace ExamContext;

public class TimeClock
{
    private User? _currentUser;
    private readonly object _occupantLock = new();

    private readonly List<User> _users;

    public TimeClock(ITimeClockTestData? testData = null)
    {
        testData ??= LocalTimeClockTestData.Create();
        _users =  testData.Users;
    }
    public void Enter(User user)
    {
        lock (_occupantLock)
        {
            if (_currentUser is not null)
            {
                throw new ExamException($"USER IS PUNCHING IN ALREADY");
            }
            _currentUser = user;
        }
        Thread.Sleep(200);
        _users.Add(user);

        lock (_occupantLock)
        {
            _currentUser = null;
        }
    }

    public void Leave(User user)
    {
        lock (_occupantLock)
        {
            if (_currentUser is not null)
            {
                throw new ExamException($"USER IS PUNCHING OUT ALREADY");
            }
            _currentUser = user;
        }
        Thread.Sleep(200);
        var existingUser = _users.Find((existingUser) => user.Id == existingUser.Id);
        if (existingUser != null) _users.Remove(existingUser);


        lock (_occupantLock)
        {
            _currentUser = null;
        }
    }

    public User? GetUser(User user)
    {
        return _users.Find(userInList => userInList.Id == user.Id) ?? null;
    }

    public IEnumerable<User> GetUsers()
    {
        return _users.ToImmutableList();
    }
}