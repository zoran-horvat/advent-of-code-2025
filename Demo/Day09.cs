static class Day09
{
    public static void Run(TextReader reader)
    {
        var points = reader.ReadPoints().ToList();

        points.Draw(40);

        // 4582310446 too high
        // 2976014041 - not
        // 137489982 - not
        // 1448987856 - not
        var maxArea = points.GetMaxArea();
        var maxInternalArea = points.GetMaxInternalArea();

        Console.WriteLine($"Largest rectangle area:          {maxArea}");
        Console.WriteLine($"Largest internal rectangle area: {maxInternalArea}");
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

        bool IsInside(Point point) => discriminators.Where(d => d.Affects(point)).LastOrDefault() is EnterAt;

        Console.WriteLine($"({minX},{minY})");
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                var point = new Point(x, y);
                if (set.Contains(point) && IsInside(point)) Console.Write('O');
                else if (set.Contains(point)) Console.Write('#');
                else if (IsInside(point)) Console.Write('X');
                else Console.Write('.');
            }
            Console.WriteLine();
        }
    }

    private static string ToLabel(this Discriminator discriminator) => discriminator switch
    {
        EnterAt enterAt => $"EnterAt Y={enterAt.Line.Y} X=[{enterAt.Line.FromX}..{enterAt.Line.ToX}]",
        ExitBelow exitBelow => $"ExitBelow Y={exitBelow.Line.Y} X=[{exitBelow.Line.FromX}..{exitBelow.Line.ToX}]",
        _ => "Unknown discriminator"
    };

    private static long GetMaxInternalArea(this List<Point> points)
    {
        var discriminators = points.GetDiscriminatorsFromTop();

        long maxArea = 0;
        var stripes = new List<Stripe>();

        foreach (var discriminator in discriminators)
        {
            maxArea = stripes.GetMaxArea(discriminator, maxArea);
            stripes = discriminator.ToNewStripes(stripes)
                .Concat(stripes.SelectMany(stripe => stripe.CloseStripe(discriminator)))
                .Distinct()
                .ToList();
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
        if (discriminator.Line.FromX < stripe.Top.FromX && discriminator.Line.ToX > stripe.Top.ToX) yield break;

        if (discriminator.Line.ToX < stripe.Top.FromX) yield return stripe;
        else if (discriminator.Line.FromX > stripe.Top.ToX) yield return stripe;
        else if (discriminator.Line.FromX > stripe.Top.FromX && discriminator.Line.FromX > stripe.Pivot.X) yield return stripe with { Top = stripe.Top with { ToX = discriminator.Line.FromX - 1 } };
        else if (discriminator.Line.ToX < stripe.Top.ToX && discriminator.Line.ToX < stripe.Pivot.X) yield return stripe with { Top = stripe.Top with { FromX = discriminator.Line.ToX + 1 } };
    }

    private static IEnumerable<Stripe> ToNewStripes(this Discriminator discriminator, IEnumerable<Stripe> existingStripes) => discriminator switch
    {
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
        Right top = discriminator.Line;
        foreach (var stripeTop in existingStripes.Select(s => s.Top).Where(t => t.Y > top.Y))
        {
            if (stripeTop.FromX < top.FromX && stripeTop.ToX >= top.FromX - 1) top = top with { FromX = top.FromX };
            if (stripeTop.FromX <= top.ToX + 1 && stripeTop.ToX > top.ToX) top = top with { ToX = stripeTop.ToX };
        }

        foreach (var point in discriminator.Points)
        {
            if (point.X > top.FromX) yield return new Stripe(point, top with { ToX = point.X });
            if (point.X < top.ToX) yield return new Stripe(point, top with { FromX = point.X });
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