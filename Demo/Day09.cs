static class Day09
{
    public static void Run(TextReader reader)
    {
        var points = reader.ReadPoints().ToList();
        var index = points.ToClockwiseSegments().ToIndex();

        var maxAreaAny = points.GetMaxArea();
        var maxAreaInside = points.GetMaxArea(index);

        points.Draw(index, 40);

        // 4582310446 too high
        Console.WriteLine($"Largest area of any rectangle:      {maxAreaAny}");
        Console.WriteLine($"Largest area of an inner rectangle: {maxAreaInside}");
    }

    private static void Draw(this IEnumerable<Point> points, Index index, int maxSize)
    {
        var set = points.ToHashSet();
        int minX = 0;
        int maxX = set.Max(p => p.X) + 2;
        int minY = 0;
        int maxY = set.Max(p => p.Y) + 2;

        if (maxX - minX > maxSize || maxY - minY > maxSize) return;


        var segments = points.ToList().ToClockwiseSegments().ToList();

        Console.WriteLine(string.Join(", ", segments.Where(seg => seg.From.Y == seg.To.Y).Select(d => $"({d.From.X}-{d.To.X}, {d.From.Y}) {Math.Sign(d.To.X - d.From.X)}")));
        var discriminators = segments.GetHorizontalDiscriminators().ToList();
        Console.WriteLine(string.Join(", ", discriminators.Select(d => $"({d.From.X}-{d.To.X}, {d.From.Y}) {Math.Sign(d.To.X - d.From.X)}")));

        List<Point> realPoints = [segments[0].From, ..segments.Select(seg => seg.To)];
        Console.WriteLine(string.Join(" -> ", realPoints.Select(point => $"({point.X}, {point.Y})")));
        Console.WriteLine($"Area: {points.ToList().ToClockwiseSegments().GetArea()}");

        index.ShapesByX
            .SelectMany(kv => kv.Value.Select(shape => (x: kv.Key, shape)))
            .OrderBy(pair => pair.x)
            .ThenByDescending(pair => pair.shape.MaxY)
            .Select(pair => $"(x={pair.x}, y <= {pair.shape.MaxY} {pair.shape.Change})")
            .ToList()
            .ForEach(Console.WriteLine);

        if (maxX - minX > maxSize || maxY - minY > maxSize) return;

        Console.WriteLine($"({minX},{minY})");
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                var point = new Point(x, y);
                if (set.Contains(point) && index.Contains(point)) Console.Write('O');
                else if (set.Contains(point)) Console.Write('#');
                else if (index.Contains(point)) Console.Write('X');
                else Console.Write('.');
            }
            Console.WriteLine();
        }
    }

    private static long GetMaxArea(this List<Point> points) =>
        points.GetAllPairs().Select(GetArea).Max();

    private static long GetMaxArea(this List<Point> points, Index index)
    {
        long maxArea = 0;

        foreach (var pair in points.GetAllPairs().Select(GetOtherCorners))
        {
            var newArea = pair.GetArea();
            if (newArea <= maxArea) continue;
            if (index.Contains(pair)) maxArea = newArea;
        }

        return maxArea;
    }

    private static (Point a, Point b) GetOtherCorners(this (Point a, Point b) pair) =>
        (new Point(pair.a.X, pair.b.Y), new Point(pair.b.X, pair.a.Y));

    private static bool Contains(this Index index, (Point a, Point b) pair) =>
        index.Contains(pair.a) && index.Contains(pair.b);

    private static bool Contains(this Index index, Point point) =>
        index.ShapesByX.TryGetValue(point.X, out var shapeSegments) &&
        shapeSegments.Where(shape => shape.MaxY >= point.Y).Sum(shape => shape.Change) > 0;

    private static Index ToIndex(this IEnumerable<Segment> segments) =>
        new Index(segments.GetHorizontalDiscriminators()
            .SelectMany(ToShapeSegments)
            .GroupBy(item => item.x)
            .ToDictionary(g => g.Key, g => g.Select(item => item.shape).ToList()));

    private static IEnumerable<(int x, ShapeSegment shape)> ToShapeSegments(this Segment segment) =>
        Enumerable.Range(Math.Min(segment.To.X, segment.From.X), Math.Abs(segment.To.X - segment.From.X) + 1)
            .Select(x => (x, segment.ToShapeSegment()));

    private static ShapeSegment ToShapeSegment(this Segment segment) =>
        segment.To.X > segment.From.X ? new ShapeSegment(segment.From.Y, 1)
        : new ShapeSegment(segment.To.Y - 1, -1);

    private static IEnumerable<Segment> GetHorizontalDiscriminators(this IEnumerable<Segment> segments)
    {
        var horizontal = segments.Where(seg => seg.From.Y == seg.To.Y).ToList();

        for (int i = 0; i < horizontal.Count; i++)
        {
            var current = horizontal[i];
            var next = horizontal[(i + 1) % horizontal.Count];

            if (current.To.X != next.From.X) continue;

            var currentStep = Math.Sign(current.To.X - current.From.X);
            var nextStep = Math.Sign(next.To.X - next.From.X);

            var currentChange = 0;
            var nextChange = 0;

            var currentY = current.From.Y;
            var nextY = next.From.Y;

            if (currentStep > 0 && nextStep > 0 && currentY > nextY) nextChange = currentStep;
            else if (currentStep > 0 && nextStep > 0) currentChange = -currentStep;
            else if (currentStep > 0 && nextStep < 0 && currentY <= nextY) (currentChange, nextChange) = ( -currentStep, -currentStep);
            else if (currentStep < 0 && nextStep < 0 && currentY > next.To.Y) currentChange = -currentStep;
            else if (currentStep < 0 && nextStep < 0) nextChange = currentStep;
            else if (currentStep < 0 && nextStep > 0 && currentY > nextY) (currentChange, nextChange) = ( -currentStep, -currentStep);

            horizontal[i] = new Segment(current.From, new Point(current.To.X + currentChange, current.To.Y));
            horizontal[(i + 1) % horizontal.Count] = new Segment(new Point(next.From.X + nextChange, next.From.Y), next.To);
        }

        return horizontal;
    }

    private static long GetArea(this IEnumerable<Segment> segments) =>
        segments.Sum(segment => (segment.To.X - segment.From.X) * (segment.To.Y + segment.From.Y) / 2L);

    private static IEnumerable<Segment> ToClockwiseSegments(this List<Point> points)
    {
        var segments = points.ToSegments().ToList();
        if (segments.GetArea() >= 0) return segments;
        return ((IEnumerable<Point>)points).Reverse().ToList().ToSegments();
    }

    private static IEnumerable<Segment> ToSegments(this List<Point> points) =>
        points.Zip(points[1..].Concat([points[0]]), (from, to) => new Segment(from, to));

    private static IEnumerable<(Point a, Point b)> GetAllPairs(this List<Point> points) =>
        from i in Enumerable.Range(0, points.Count - 1)
        from j in Enumerable.Range(i + 1, points.Count - i - 1)
        select (points[i], points[j]);

    private static long GetArea(this (Point a, Point b) pair) =>
        ((long)Math.Abs(pair.b.X - pair.a.X) + 1) * (Math.Abs(pair.b.Y - pair.a.Y) + 1);

    private static IEnumerable<Point> ReadPoints(this TextReader reader) =>
        reader.ReadLines()
            .Select(line => line.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(parts => new Point(int.Parse(parts[0]), int.Parse(parts[1])));

    record Index(Dictionary<int, List<ShapeSegment>> ShapesByX);
    record struct ShapeSegment(int MaxY, int Change);
    record Segment(Point From, Point To);
    record Point(int X, int Y);
}