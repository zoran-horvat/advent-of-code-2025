using System.Runtime.ExceptionServices;

static class ClosestSpatialPairsSelector
{
    public static IEnumerable<((int x, int y, int z) first, (int x, int y, int z) second)> GetClosestPairs(this IEnumerable<(int x, int y, int z)> points)
    {
        var rootBox = points.Select(triplet => new Point(triplet.x, triplet.y, triplet.z)).ToList().ToBox();
        PriorityQueue<(Box First, Box Second), long> queue = new();

        queue.Enqueue((rootBox, rootBox), 0);

        while (queue.TryDequeue(out var boxPair, out var distance))
        {
            if (boxPair.First.Points.Count == 1 && boxPair.Second.Points.Count == 1)
            {
                var (x1, y1, z1) = boxPair.First.Points[0];
                var (x2, y2, z2) = boxPair.Second.Points[0];
                if (boxPair.First.Points[0].CompareTo(boxPair.Second.Points[0]) < 0) yield return ((x1, y1, z1), (x2, y2, z2));
                continue;
            }

            foreach (var pair in boxPair.Split()) queue.Enqueue(pair, pair.first.DistanceFrom(pair.second));
        }
    }

    private static IEnumerable<(Box first, Box second)> Split(this (Box first, Box second) pair)
    {
        if (pair.first == pair.second)
        {
            var split = pair.first.Split();
            yield return (split.first, split.first);
            yield return (split.first, split.second);
            yield return (split.second, split.first);
            yield return (split.second, split.second);
        }
        else if (pair.first.Points.Count >= pair.second.Points.Count)
        {
            var split = pair.first.Split();
            yield return (split.first, pair.second);
            yield return (split.second, pair.second);
        }
        else
        {
            var split = pair.second.Split();
            yield return (pair.first, split.first);
            yield return (pair.first, split.second);
        }
    }

    private static int CompareTo(this Point a, Point b) =>
        a.X != b.X ? a.X.CompareTo(b.X)
        : a.Y != b.Y ? a.Y.CompareTo(b.Y)
        : a.Z.CompareTo(b.Z);

    private static (Box first, Box second) Split(this Box box)
    {
        int xRange = box.MaxX - box.MinX;
        int yRange = box.MaxY - box.MinY;
        int zRange = box.MaxZ - box.MinZ;

        int splitAxis = 
            xRange >= yRange && xRange >= zRange ? 0
            : yRange >= xRange && yRange >= zRange ? 1 
            : 2;

        int selectAxis(Point point, int axis) => axis switch
        {
            0 => point.X,
            1 => point.Y,
            _ => point.Z
        };

        IComparer<Point> comparer = Comparer<Point>.Create((a, b) => selectAxis(a, splitAxis).CompareTo(selectAxis(b, splitAxis)));
        box.Points.Sort(comparer);

        List<Point> left = box.Points[..(box.Points.Count / 2)];
        List<Point> right = box.Points[(box.Points.Count / 2)..];

        return (left.ToBox(), right.ToBox());
    }

    private static Box ToBox(this List<Point> points)
    {
        var extremes = points.Select(point => (min: point, max: point))
            .Aggregate((acc, point) => 
            (
                min: new(Math.Min(acc.min.X, point.min.X), Math.Min(acc.min.Y, point.min.Y), Math.Min(acc.min.Z, point.min.Z)),
                max: new(Math.Max(acc.max.X, point.max.X), Math.Max(acc.max.Y, point.max.Y), Math.Max(acc.max.Z, point.max.Z))
            ));

        return new Box(points, extremes.min.X, extremes.min.Y, extremes.min.Z, extremes.max.X, extremes.max.Y, extremes.max.Z);
    }

    private static long DistanceFrom(this Box A, Box B)
    {
        if (A == B) return 0;

        long dx = Math.Max(0, Math.Max(A.MinX - B.MaxX, B.MinX - A.MaxX));
        long dy = Math.Max(0, Math.Max(A.MinY - B.MaxY, B.MinY - A.MaxY));
        long dz = Math.Max(0, Math.Max(A.MinZ - B.MaxZ, B.MinZ - A.MaxZ));

        return dx * dx + dy * dy + dz * dz;
    }

    private record Box(List<Point> Points, int MinX, int MinY, int MinZ, int MaxX, int MaxY, int MaxZ);
    
    private record Point(int X, int Y, int Z);    
}