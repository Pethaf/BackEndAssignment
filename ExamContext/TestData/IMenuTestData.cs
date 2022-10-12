using System.Collections.Immutable;

namespace ExamContext.TestData;

public interface IMenuTestData
{
    ImmutableList<MenuItem> Data { get; }
}
