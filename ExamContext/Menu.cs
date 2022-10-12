using ExamContext.LocalData;
using System.Collections.Immutable;
using ExamContext.TestData;

namespace ExamContext;

public class Menu
{
    public ImmutableList<MenuItem> MenuItems { get; private set; }


    public Menu(IMenuTestData? testData = null)
    {
        testData ??= LocalMenuData.Create();
        MenuItems = testData.Data;
    }
}
