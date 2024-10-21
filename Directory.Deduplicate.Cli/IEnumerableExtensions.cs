namespace Directory.Deduplicate.Cli;

public static class IEnumerableExtensions
{
    public static IEnumerable<T> If<T>(
        this IEnumerable<T> source,
        bool condition,
        Func<IEnumerable<T>, IEnumerable<T>> action)
        => condition
        ? action(source)
        : source;
}
