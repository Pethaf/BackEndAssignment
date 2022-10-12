using ExamContext.LocalData;
using ExamContext.TestData;

namespace ExamContext;

public class UserRepository
{
    public List<User> Users { get; }

    public UserRepository(IUserRepositoryTestData? userData = null)
    {
        userData ??= LocalUserRepositoryTestData.Create();
        Users = userData.Users;
    }
}