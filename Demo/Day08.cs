static class Day08
{
    public static void Run(TextReader reader)
    {
        int stepsCount = int.Parse(reader.ReadLine()!);
        var network = reader.ReadPoints().ToList();

        var (largestCircuits, lastRay) = network.SimulateMerging(stepsCount);

        Console.WriteLine($"Largest circuits: {largestCircuits.Multiply()}");
        Console.WriteLine($"Wall distance:    {(long)lastRay.From.X * lastRay.To.X}");
    }

    private static (int[] largestCircuits, Ray lastRay) SimulateMerging(this IEnumerable<Point> points, int reportAfterRaysCount)
    {
        var list = points.ToList();
        var rayCandidates = list.GetRaysInPreferenceOrderBruteForce();
        Dictionary<Point, HashSet<Point>> adjacentPoints = points.ToDictionary(p => p, p => new HashSet<Point>() { p });

        int[] largestCircuits = Array.Empty<int>();

        foreach (var ray in rayCandidates)
        {
            if (reportAfterRaysCount-- == 0)
            {
                var x = adjacentPoints.Values.Distinct().Select(pointsSet => pointsSet.Count).OrderDescending().Take(3).ToArray();
                largestCircuits = adjacentPoints.Values
                    .Distinct()
                    .Select(pointsSet => pointsSet.Count)
                    .OrderDescending()
                    .Take(3)
                    .ToArray();
            }

            var circuitA = adjacentPoints[ray.From];
            var circuitB = adjacentPoints[ray.To];

            if (circuitA == circuitB) continue;

            foreach (var pointB in circuitB)
            {
                adjacentPoints[pointB] = circuitA;
                circuitA.Add(pointB);
            }

            if (circuitA.Count == list.Count) return (largestCircuits, ray);
        }

        throw new ArgumentException("All points did not merge into a single circuit.");
    }

    private static IEnumerable<Ray> GetRaysInPreferenceOrder(this List<Point> points) =>
        ClosestSpatialPairsSelector
            .GetClosestPairs(points.Select(p => (p.X, p.Y, p.Z)))
            .Select(pair => new Ray(
                new Point(pair.first.x, pair.first.y, pair.first.z),
                new Point(pair.second.x, pair.second.y, pair.second.z)));

    private static IEnumerable<Ray> GetRaysInPreferenceOrderBruteForce(this List<Point> points) =>
        from i in Enumerable.Range(0, points.Count - 1)
        from j in Enumerable.Range(i + 1, points.Count - i - 1)
        let ray = new Ray(points[i], points[j])
        orderby ray.From.SquareDistanceFrom(ray.To)
        select ray;

    private static long Multiply(this IEnumerable<int> numbers) =>
        numbers.Aggregate(1L, (acc, n) => acc * n);

    private static long SquareDistanceFrom(this Point a, Point b) =>
        (long)(a.X - b.X) * (a.X - b.X) +
        (long)(a.Y - b.Y) * (a.Y - b.Y) +
        (long)(a.Z - b.Z) * (a.Z - b.Z);

    private static IEnumerable<Point> ReadPoints(this TextReader reader) =>
        reader.ReadLines()
            .Select(line => line.Split(',').Select(int.Parse).ToArray())
            .Select(coords => new Point(coords[0], coords[1], coords[2]));

    record Ray(Point From, Point To);
    record Point(int X, int Y, int Z);
}