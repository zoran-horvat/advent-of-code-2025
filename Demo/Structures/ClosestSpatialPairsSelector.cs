static class ClosestSpatialPairsSelector
{
    public static IEnumerable<((int x, int y, int z) first, (int x, int y, int z) second)> GetClosestPairs(this IEnumerable<(int x, int y, int z)> points)
    {
        var rootBox = points.Select(triplet => new Point(triplet.x, triplet.y, triplet.z)).ToArray().ToBox();
        PriorityQueue<(Box First, Box Second), long> queue = new();

        queue.Enqueue((rootBox, rootBox), 0);

        while (queue.TryDequeue(out var boxPair, out var distance))
        {
            if (boxPair.First.Points.Length == 1 && boxPair.Second.Points.Length == 1)
            {
                var (x1, y1, z1) = boxPair.First.Points[0];
                var (x2, y2, z2) = boxPair.Second.Points[0];
                yield return ((x1, y1, z1), (x2, y2, z2));
                continue;
            }

            boxPair.SplitAndEnqueue(queue);
        }
    }

    private static void Enqueue(this PriorityQueue<(Box first, Box second), long> queue, (Box first, Box second) pair)
    {
        if (pair.first.Points.Length == 1 && pair.second.Points.Length == 1 && pair.first.Points[0] == pair.second.Points[0]) return;

        long distance = pair.first.DistanceFrom(pair.second);
        queue.Enqueue(pair, distance);
    }

    private static void SplitAndEnqueue(this (Box first, Box second) pair, PriorityQueue<(Box first, Box second), long> queue)
    {
        if (pair.first == pair.second)
        {
            var split = pair.first.Split();
            queue.Enqueue((split.first, split.first));
            queue.Enqueue((split.first, split.second));
            queue.Enqueue((split.second, split.second));
        }
        else if (pair.first.Points.Length >= pair.second.Points.Length)
        {
            var split = pair.first.Split();
            queue.Enqueue((split.first, pair.second));
            queue.Enqueue((split.second, pair.second));
        }
        else
        {
            var split = pair.second.Split();
            queue.Enqueue((pair.first, split.first));
            queue.Enqueue((pair.first, split.second));
        }
    }

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

        int[] values = new int[box.Points.Length];
        for (int i = 0; i < box.Points.Length; i++)
        {
            values[i] = selectAxis(box.Points[i], splitAxis);
        }
        Array.Sort(values);
        int medianValue = values[values.Length / 2];
        int leftCount = values.Length / 2;
        while (leftCount > 0 && values[leftCount - 1] == medianValue) leftCount--;

        Point[] left = new Point[leftCount];
        Point[] right = new Point[values.Length - leftCount];

        int leftIndex = 0;
        int rightIndex = 0;
        foreach (var point in box.Points)
        {
            if (selectAxis(point, splitAxis) < medianValue) left[leftIndex++] = point;
            else right[rightIndex++] = point;
        }

        return (left.ToBox(), right.ToBox());
    }

    private static Box ToBox(this Point[] points)
    {
        var minX = points[0].X;
        var minY = points[0].Y;
        var minZ = points[0].Z;
        var maxX = points[0].X;
        var maxY = points[0].Y;
        var maxZ = points[0].Z;

        for (int i = 1; i < points.Length; i++)
        {
            if (points[i].X < minX) minX = points[i].X;
            if (points[i].Y < minY) minY = points[i].Y;
            if (points[i].Z < minZ) minZ = points[i].Z;
            if (points[i].X > maxX) maxX = points[i].X;
            if (points[i].Y > maxY) maxY = points[i].Y;
            if (points[i].Z > maxZ) maxZ = points[i].Z;
        }

        return new Box(points, minX, minY, minZ, maxX, maxY, maxZ);
    }

    private static long DistanceFrom(this Box A, Box B)
    {
        if (A == B) return A.EstimateMinimumInnerDistance();

        long dx = Math.Max(0, Math.Max(A.MinX - B.MaxX, B.MinX - A.MaxX));
        long dy = Math.Max(0, Math.Max(A.MinY - B.MaxY, B.MinY - A.MaxY));
        long dz = Math.Max(0, Math.Max(A.MinZ - B.MaxZ, B.MinZ - A.MaxZ));

        return dx * dx + dy * dy + dz * dz;
    }

    private static long EstimateMinimumInnerDistance(this Box box) => box.Points switch
    {
        [var a, var b, var c] => Math.Min(a.DistanceFrom(b), Math.Min(b.DistanceFrom(c), c.DistanceFrom(a))),
        [var a, var b] => a.DistanceFrom(b),
        _ => 0
    };

    private static long DistanceFrom(this Point a, Point b) =>
        (long)(a.X - b.X) * (a.X - b.X) +
        (long)(a.Y - b.Y) * (a.Y - b.Y) +
        (long)(a.Z - b.Z) * (a.Z - b.Z);

    private record Box(Point[] Points, int MinX, int MinY, int MinZ, int MaxX, int MaxY, int MaxZ);
    
    private record struct Point(int X, int Y, int Z);
}