static class Day11
{
    public static void Run(TextReader reader)
    {
        var connections = reader.ParseConnections().IndexOrigins();

        var totalSimplePaths = connections.CountPaths("you", "out");

        var totalComplexPaths = 
            connections.CountComplexPaths(["svr", "dac", "fft", "out"]) +
            connections.CountComplexPaths(["svr", "fft", "dac", "out"]);

        Console.WriteLine($"Total number of simple paths:  {totalSimplePaths}");
        Console.WriteLine($"Total number of complex paths: {totalComplexPaths}");
    }

    private static long CountComplexPaths(this Dictionary<string, string[]> origins, string[] points) =>
        points.Skip(1)
            .Zip(points, (to, from) => origins.CountPaths(from, to))
            .Aggregate(1L, (acc, count) => acc * count);

    private static long CountPaths(this Dictionary<string, string[]> origins, string from, string to) =>
        origins.CountPaths(to, new Dictionary<string, long>() { { from, 1 }});

    private static long CountPaths(this Dictionary<string, string[]> origins, string to, Dictionary<string, long> knownCounts) =>
        knownCounts.TryGetValue(to, out var knownCount) ? knownCount
        : knownCounts[to] = origins.FullCountPaths(to, knownCounts);

    private static long FullCountPaths(this Dictionary<string, string[]> origins, string to, Dictionary<string, long> knownCounts) =>
        origins.GetValueOrDefault(to, Array.Empty<string>())
            .Sum(origin => origins.CountPaths(origin, knownCounts));

    private static Dictionary<string, string[]> IndexOrigins(this IEnumerable<Connection> connections) =>
        connections
            .GroupBy(c => c.To)
            .ToDictionary(
                g => g.Key,
                g => g.Select(c => c.From).ToArray()
            );

    private static IEnumerable<Connection> ParseConnections(this TextReader reader) =>
        reader.ReadLines().SelectMany(ParseConnections);
    
    private static IEnumerable<Connection> ParseConnections(this string line) => line.Split(":") switch
    {
        [var from, var tos] => from.ParseConnections(tos),
        _ => Array.Empty<Connection>()
    };

    private static IEnumerable<Connection> ParseConnections(this string from, string tos) =>
        tos.Split(" ", StringSplitOptions.RemoveEmptyEntries).Select(to => new Connection(from.Trim(), to.Trim()));

    record Connection(string From, string To);
}