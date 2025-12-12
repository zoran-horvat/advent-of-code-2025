static class Day09
{
    public static void Run(TextReader reader)
    {
        var points = reader.ReadPoints().ToList();

        points.Draw(40);

        var maxArea = points.GetMaxArea();

        System.Diagnostics.Stopwatch swBruteForce = System.Diagnostics.Stopwatch.StartNew();
        var maxInternalAreaBruteForce = points.GetMaxInternalAreaBruteForce();
        swBruteForce.Stop();

        System.Diagnostics.Stopwatch swOptimized = System.Diagnostics.Stopwatch.StartNew();
        var maxInternalArea = points.GetMaxInternalArea();
        swOptimized.Stop();


        Console.WriteLine($"Largest rectangle area:                        {maxArea}");
        Console.WriteLine($"Largest internal rectangle area (brute force): {maxInternalAreaBruteForce} ({swBruteForce.Elapsed})");
        Console.WriteLine($"Largest internal rectangle area:               {maxInternalArea} ({swOptimized.Elapsed})");
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

    private static IEnumerable<(Point a, Point b)> GetAllPairs(this List<Point> points) =>
        from i in Enumerable.Range(0, points.Count - 1)
        let a = points[i]
        from b in points[(i + 1)..]
        select (a, b);

    private static long GetArea((Point a, Point b) pair) =>
        (long)(Math.Abs(pair.a.X - pair.b.X) + 1) * (Math.Abs(pair.a.Y - pair.b.Y) + 1);

    private static void Draw(this IEnumerable<Point> points, int maxSize)
    {
        var set = points.ToHashSet();
        int minX = 0;
        int maxX = set.Max(p => p.X) + 2;
        int minY = 0;
        int maxY = set.Max(p => p.Y) + 2;

        if (maxX - minX > maxSize || maxY - minY > maxSize) return;

        if (maxX - minX > maxSize || maxY - minY > maxSize) return;
        
        var discriminators = points.GetDiscriminatorsFromTop().ToList();

        bool isInside(Point point) => discriminators.Where(d => d.Affects(point)).LastOrDefault() is EnterAt;

        Console.WriteLine($"({minX},{minY})");
        for (int y = minY; y <= maxY; y++)
        {
            Console.Write($"y={y,3} ");
            for (int x = minX; x <= maxX; x++)
            {
                var point = new Point(x, y);
                if (set.Contains(point) && isInside(point)) Console.Write('O');
                else if (set.Contains(point)) Console.Write('#');
                else if (isInside(point)) Console.Write('X');
                else Console.Write('.');
            }
            Console.WriteLine();
        }

        Console.Write("      ");
        for (int x = minX; x <= maxX; x++) Console.Write((x / 10) % 10);
        Console.WriteLine();
        Console.Write("      ");
        for (int x = minX; x <= maxX; x++) Console.Write(x % 10);
        Console.WriteLine();
        Console.WriteLine("-------------------------------");
    }

    private static string ToLabel(this Discriminator discriminator) => discriminator switch
    {
        EnterAt enterAt => $"EnterAt Y={enterAt.Line.Y} X=[{enterAt.Line.FromX}..{enterAt.Line.ToX}]",
        ExitBelow exitBelow => $"ExitBelow Y={exitBelow.Line.Y} X=[{exitBelow.Line.FromX}..{exitBelow.Line.ToX}]",
        _ => "Unknown discriminator"
    };

    private static long GetMaxInternalAreaBruteForce(this List<Point> points)
    {
        var discriminatorsFromTop = points.GetDiscriminatorsFromTop().ToList();

        bool isInside(Point point) => discriminatorsFromTop.Where(d => d.Affects(point)).LastOrDefault() is EnterAt;

        var candidates = points.GetAllPairs().OrderByDescending(GetArea);

        foreach (var (a, b) in candidates)
        {
            var fromX = Math.Min(a.X, b.X);
            var toX = Math.Max(a.X, b.X);
            var fromY = Math.Min(a.Y, b.Y);
            var toY = Math.Max(a.Y, b.Y);

            var containsExitLines = discriminatorsFromTop.OfType<ExitBelow>()
                .Where(d => d.Line.Y - 1 <= toY && d.Line.Y - 1 >= fromY)
                .Where(d => !(d.Line.ToX < fromX || d.Line.FromX > toX))
                .Any();

            if (containsExitLines) continue;

            var topPoints = discriminatorsFromTop
                .SelectMany(d => d.GetPoints())
                .SelectMany(p => new int[] { p.X - 1, p.X, p.X + 1 })
                .Concat([fromX, fromX + 1, toX, toX - 1])
                .Distinct()
                .Where(x => x >= fromX && x <= toX)
                .Select(x => new Point(x, toY));

            if (!topPoints.All(isInside)) continue;

            return GetArea((a, b));
        }

        return 0;
    }

    private static long GetMaxInternalArea(this List<Point> points)
    {
        var discriminators = points.GetDiscriminatorsFromTop();

        long maxArea = 0;
        var stripes = new List<Stripe>();

        int xRange = points.Max(p => p.X) - points.Min(p => p.X) + 1;
        int yRange = points.Max(p => p.Y) - points.Min(p => p.Y) + 1;
        bool report = xRange <= 40 && yRange <= 40;

        foreach (var discriminator in discriminators)
        {
            maxArea = stripes.GetMaxArea(discriminator, maxArea);
            stripes = discriminator.ToNewStripes(stripes)
                .Concat(stripes.SelectMany(stripe => stripe.CloseStripe(discriminator)))
                .Distinct()
                .ToList();

            var y = discriminator switch
            {
                EnterAt enterAt => enterAt.Line.Y,
                ExitBelow exitBelow => exitBelow.Line.Y - 1,
                _ => throw new ArgumentException("Unknown discriminator type.")
            };
            var printableStripes = stripes.OrderBy(s => s.Pivot.X).ThenByDescending(s => s.Pivot.Y).ThenBy(s => s.Top.FromX).ToList();
            if (report) Console.WriteLine($"y={y,-3} {discriminator.ToLabel(),-30} | Stripes: {string.Join(", ", printableStripes.Select(s => s.ToLabel()))}");
        }

        return maxArea;
    }

    private static string ToLabel(this Stripe stripe) =>
        $"({stripe.Pivot.X},{stripe.Pivot.Y}) [{stripe.Top.FromX}-{stripe.Top.ToX}]";

    private static IEnumerable<Stripe> CloseStripe(this Stripe stripe, Discriminator discriminator) =>
        discriminator is ExitBelow exitBelow ? stripe.CloseStripe(exitBelow)
        : [stripe];

    private static IEnumerable<Stripe> CloseStripe(this Stripe stripe, ExitBelow discriminator)
    {
        if (discriminator.Line.FromX <= stripe.Pivot.X && discriminator.Line.ToX >= stripe.Pivot.X)
        {
            Console.WriteLine($"    Removing stripe {stripe.ToLabel()} by discriminator {discriminator.ToLabel()}");
            yield break;        // Discriminator removes pivot
        }
        else if (discriminator.Line.ToX < stripe.Top.FromX) yield return stripe;
        else if (discriminator.Line.FromX > stripe.Top.ToX) yield return stripe;
        else if (discriminator.Line.FromX <= stripe.Top.ToX && discriminator.Line.FromX > stripe.Top.FromX)
        {
            var newStripe = stripe with { Top = stripe.Top with { ToX = discriminator.Line.FromX - 1 } };
            Console.WriteLine($"    Clipping stripe {stripe.ToLabel()} -> {newStripe.ToLabel()} by discriminator {discriminator.ToLabel()}");
            yield return newStripe;
        }
        else if (discriminator.Line.ToX >= stripe.Top.FromX && discriminator.Line.ToX < stripe.Top.ToX)
        {
            var newStripe = stripe with { Top = stripe.Top with { FromX = discriminator.Line.ToX + 1 } };
            Console.WriteLine($"    Clipping stripe {stripe.ToLabel()} -> {newStripe.ToLabel()} by discriminator {discriminator.ToLabel()}");
            yield return newStripe;
        }
    }

    private static IEnumerable<Stripe> ToNewStripes(this Discriminator discriminator, IEnumerable<Stripe> existingStripes) => discriminator switch
    {
        // (9,20) [9-23] -> Must also generate (9,20) [3-9]
        // ()
        EnterAt enterAt => enterAt.ToNewStripes(existingStripes),
        ExitBelow exitBelow => exitBelow.ToNewStripes(existingStripes),
        _ => Enumerable.Empty<Stripe>()
    };

    private static IEnumerable<Stripe> ToNewStripes(this ExitBelow discriminator, IEnumerable<Stripe> existingStripes)
    {
        var toLeft = discriminator.Points
            .Where(p => p.X < discriminator.Line.FromX)
            .SelectMany(p => existingStripes.Where(s => s.Contains(p)).Select(s => new Stripe(p, new Right(discriminator.Line.Y, s.Top.FromX, p.X))));

        var toRight = discriminator.Points
            .Where(p => p.X > discriminator.Line.ToX)
            .SelectMany(p => existingStripes.Where(s => s.Contains(p)).Select(s => new Stripe(p, new Right(discriminator.Line.Y, p.X, s.Top.ToX))));

        return toLeft.Concat(toRight);
    }

    private static IEnumerable<Stripe> ToNewStripes(this EnterAt discriminator, IEnumerable<Stripe> existingStripes)
    {
        Right top = new Right(discriminator.Line.Y, discriminator.Points.Min(p => p.X), discriminator.Points.Max(p => p.X));
        foreach (var stripeTop in existingStripes.Select(s => s.Top).Where(t => t.Y >= top.Y))
        {
            if (stripeTop.FromX < top.FromX && stripeTop.ToX >= top.FromX - 1) top = top with { FromX = stripeTop.FromX };
            if (stripeTop.FromX <= top.ToX + 1 && stripeTop.ToX > top.ToX) top = top with { ToX = stripeTop.ToX };
        }

        foreach (var point in discriminator.Points)
        {
            if (point.X > top.FromX && point.X <= top.ToX) yield return new Stripe(point, top with { ToX = point.X });
            if (point.X >= top.FromX && point.X <= top.ToX) yield return new Stripe(point, top with { FromX = point.X });
        }
    }

    private static long GetMaxArea(this IEnumerable<Stripe> stripes, Discriminator discriminator, long previousMaxArea) =>
        discriminator.GetPoints()
            .SelectMany(point => stripes.Where(stripe => stripe.Contains(point)).Select(stripe => GetArea((stripe.Pivot, point)))
            .Concat([previousMaxArea]))
            .Max();

    private static bool Contains(this Stripe stripe, Point point) =>
        stripe.Top.Y >= point.Y && stripe.Top.FromX <= point.X && stripe.Top.ToX >= point.X;

    private static IEnumerable<Discriminator> GetDiscriminatorsFromTop(this IEnumerable<Point> points) =>
        points.ToList()
            .ToClockwiseLines()
            .GetHorizontalDiscriminators()
            .OrderByDescending(discriminator => discriminator switch 
            {
                EnterAt enterAt => enterAt.Line.Y,
                ExitBelow enterBelow => enterBelow.Line.Y - 1,
                _ => throw new ArgumentException("Unknown discriminator type.")
            });

    private static IEnumerable<Discriminator> GetHorizontalDiscriminators(this IEnumerable<Line> segments)
    {
        var horizontal = segments.OfType<HorizontalLine>().ToList();
        var points = horizontal.Select(GetPoints).ToList();

        for (int i = 0; i < horizontal.Count; i++)
        {
            var prev = horizontal[(i + horizontal.Count - 1) % horizontal.Count];
            var current = horizontal[i];
            var next = horizontal[(i + 1) % horizontal.Count];
            var endpoints = current.GetPoints().ToArray();
            
            var discriminatorLine = (prev, current, next) switch
            {
                (HorizontalLine p, Right c, HorizontalLine n) when c.Y > p.Y && c.Y < n.Y => c.WithoutLastPoint(),
                (HorizontalLine p, Right c, HorizontalLine n) when c.Y > p.Y && c.Y > p.Y => c,
                (HorizontalLine p, Right c, HorizontalLine n) when c.Y < p.Y && c.Y < n.Y => c.WithoutFirstPoint().WithoutLastPoint(),
                (HorizontalLine p, Right c, HorizontalLine n) when c.Y < p.Y && c.Y > n.Y => c.WithoutFirstPoint(),
                (HorizontalLine p, Left c, HorizontalLine n) when c.Y > p.Y && c.Y < n.Y => c.WithoutFirstPoint(),
                (HorizontalLine p, Left c, HorizontalLine n) when c.Y > p.Y && c.Y > p.Y => c.WithoutFirstPoint().WithoutLastPoint(),
                (HorizontalLine p, Left c, HorizontalLine n) when c.Y < p.Y && c.Y < n.Y => c,
                (HorizontalLine p, Left c, HorizontalLine n) when c.Y < p.Y && c.Y > n.Y => c.WithoutLastPoint(),
                _ => throw new InvalidOperationException("Cannot determine discriminator type.")
            };

            yield return discriminatorLine.ToDiscriminator(endpoints);
        }
    }

    private static Discriminator ToDiscriminator(this Line line, Point[] points) => line switch
    {
        Right r => new EnterAt(r, points),
        Left l => new ExitBelow(l.Reverse(), points),
        _ => throw new ArgumentException("Only horizontal lines can be converted to discriminators.")
    };

    private static bool Affects(this Discriminator discriminator, Point point) => discriminator switch
    {
        EnterAt enterAt => point.Y <= enterAt.Line.Y && point.X >= enterAt.Line.FromX && point.X <= enterAt.Line.ToX,
        ExitBelow exitBelow => point.Y < exitBelow.Line.Y && point.X >= exitBelow.Line.FromX && point.X <= exitBelow.Line.ToX,
        _ => false
    };

    private static Right Reverse(this Left line) => new Right(line.Y, line.ToX, line.FromX);

    private static Line WithoutFirstPoint(this Line line) => line switch
    {
        Right r => new Right(r.Y, r.FromX + 1, r.ToX),
        Left l => new Left(l.Y, l.FromX - 1, l.ToX),
        Up u => new Up(u.X, u.FromY + 1, u.ToY),
        Down d => new Down(d.X, d.FromY - 1, d.ToY),
        _ => line
    };

    private static Line WithoutLastPoint(this Line line) => line switch
    {
        Right r => new Right(r.Y, r.FromX, r.ToX - 1),
        Left l => new Left(l.Y, l.FromX, l.ToX + 1),
        Up u => new Up(u.X, u.FromY, u.ToY - 1),
        Down d => new Down(d.X, d.FromY, d.ToY + 1),
        _ => line
    };

    private static Point[] GetPoints(this Discriminator discriminator) => discriminator switch
    {
        EnterAt enterAt => enterAt.Points,
        ExitBelow exitBelow => exitBelow.Points,
        _ => throw new ArgumentException("Unknown discriminator type.")
    };

    private static Point[] GetPoints(this Line line) => line switch
    {
        Right r => [new Point(r.FromX, r.Y), new Point(r.ToX, r.Y)],
        Left l => [new Point(l.FromX, l.Y), new Point(l.ToX, l.Y)],
        Up u => [new Point(u.X, u.FromY), new Point(u.X, u.ToY)],
        Down d => [new Point(d.X, d.FromY), new Point(d.X, d.ToY)],
        _ => throw new ArgumentException("Unknown line type.")
    };

    private static IEnumerable<Line> ToClockwiseLines(this List<Point> points)
    {
        var segments = points.ToLines().ToList();
        var topMostSegments = segments.OfType<HorizontalLine>().MaxBy(line => line.Y);

        if (topMostSegments is Right) return segments;

        return ((IEnumerable<Point>)points).Reverse().ToList().ToLines();
    }

    private static IEnumerable<Line> ToLines(this List<Point> points) =>
        points.Zip(points[1..].Concat([points[0]]), (from, to) => from.LineTo(to));

    private static Line LineTo(this Point from, Point to) =>
        from.Y == to.Y && from.X < to.X ? new Right(from.Y, from.X, to.X)
        : from.Y == to.Y ? new Left(from.Y, from.X, to.X)
        : from.Y < to.Y ? new Up(from.X, from.Y, to.Y)
        : new Down(from.X, from.Y, to.Y);

    private static IEnumerable<Point> ReadPoints(this TextReader reader) =>
        reader.ReadLines()
            .Select(line => line.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(parts => new Point(int.Parse(parts[0]), int.Parse(parts[1])));

    record Stripe(Point Pivot, Right Top);

    record Index(Dictionary<int, List<ShapeSegment>> ShapesByX);
    record ShapeSegment(int MaxY, int Change);

    abstract record Discriminator;
    record EnterAt(Right Line, Point[] Points) : Discriminator;
    record ExitBelow(Right Line, Point[] Points) : Discriminator;
    
    abstract record Line;
    abstract record HorizontalLine(int Y) : Line;
    record Right(int Y, int FromX, int ToX) : HorizontalLine(Y);
    record Left(int Y, int FromX, int ToX) : HorizontalLine(Y);
    record Up(int X, int FromY, int ToY) : Line;
    record Down(int X, int FromY, int ToY) : Line;

    record Point(int X, int Y);
}