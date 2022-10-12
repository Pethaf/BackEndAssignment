namespace BackendExam.Tests.StackExtensions;

internal static class StackHelpers
{
    public static Stack<T> ToStack<T>(this IEnumerable<T> theList)
    {
        return new Stack<T>(theList);
    }
}
