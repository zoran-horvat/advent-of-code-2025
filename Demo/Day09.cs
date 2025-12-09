static class Day09
{
    public static void Run(TextReader reader)
    {
        var points = reader.ReadPoints().ToList();

        var maxArea = points.GetMaxArea();

        Console.WriteLine($"Largest area between any two points: {maxArea}");
    }

    private static long GetMaxArea(this List<Point> points)
    {
        var sorted = points.OrderBy(p => p.Y).ToArray();
        int maxY = sorted[^1].Y;
        int xRange = sorted.Max(p => p.X) - sorted.Min(p => p.X) + 1;

        long maxArea = 0;
        for (int i = 0; i < sorted.Length - 1; i++)
        {
            int minY = (int)(maxArea / xRange) + sorted[i].Y;
            for (int j = sorted.Length - 1; j > i; j--)
            {
                if (sorted[j].Y < minY) break;

                long area = (long)(sorted[j].X - sorted[i].X + 1) * (sorted[j].Y - sorted[i].Y + 1);
                if (area > maxArea) maxArea = area;
                minY = (int)(maxArea / xRange) + sorted[i].Y;
            }
        }

        return maxArea;
    }

    private static long GetMaxAreaBruteForce(this List<Point> points) =>
        points.GetAllPairs().Select(GetArea).Max();

    private static IEnumerable<(Point a, Point b)> GetAllPairs(this List<Point> points) =>
        from i in Enumerable.Range(0, points.Count - 1)
        from j in Enumerable.Range(i + 1, points.Count - i - 1)
        select (points[i], points[j]);

    private static long GetArea((Point a, Point b) pair) =>
        Math.Abs((long)(pair.a.X - pair.b.X + 1) * (pair.a.Y - pair.b.Y + 1));

    private static IEnumerable<Point> ReadPoints(this TextReader reader) =>
        reader.ReadLines()
            .Select(line => line.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(parts => new Point(int.Parse(parts[0]), int.Parse(parts[1])));

    record Point(int X, int Y);
}