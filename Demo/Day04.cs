static class Day04
{
    public static void Run(TextReader reader)
    {
        var map = reader.ReadMap();

        var (immediateRemovable, totalRemovable) = map.CountRemovableRolls();

        Console.WriteLine($"Optimized immediate: {immediateRemovable}");
        Console.WriteLine($"Optimized total:     {totalRemovable}");
    }

    private static (int immediate, int total) CountRemovableRolls(this Map map)
    {
        var neighborsCount = map.Rolls.ToDictionary(roll => roll, roll => map.Neighbors[roll].Count(map.Rolls.Contains));
        var pending = new Queue<Position>(neighborsCount.Where(kv => kv.Value < 4).Select(kv => kv.Key));
        var remaining = map.Rolls.Except(pending).ToHashSet();

        var immediate = pending.Count;
        var total = immediate;

        while (pending.TryDequeue(out var roll))
        {
            foreach (var neighbor in map.Neighbors[roll].Where(remaining.Contains))
            {
                if (--neighborsCount[neighbor] < 4)
                {
                    pending.Enqueue(neighbor);
                    remaining.Remove(neighbor);
                    total++;
                }
            }
        }

        return (immediate, total);
    }
    
    private static Map ReadMap(this TextReader reader)
    {
        string[] rows = reader.ReadLines().ToArray();
        
        var rolls = new HashSet<Position>();
        var neighbors = new Dictionary<Position, Position[]>();

        for (int row = 0; row < rows.Length; row++)
        {
            int prevPos = 0;
            while ((prevPos = rows[row].IndexOf('@', prevPos)) != -1)
            {
                rolls.Add(new Position(row, prevPos));
                prevPos++;
            }
        }

        foreach (var roll in rolls)
        {
            List<Position> validNeighbors = new();
            for (int neighborRow = Math.Max(0, roll.Row - 1); neighborRow <= Math.Min(rows.Length - 1, roll.Row + 1); neighborRow++)
            {
                for (int neighborColumn = Math.Max(0, roll.Column - 1); neighborColumn <= Math.Min(rows[neighborRow].Length - 1, roll.Column + 1); neighborColumn++)
                {
                    if (neighborRow == roll.Row && neighborColumn == roll.Column) continue;
                    var neighbor = new Position(neighborRow, neighborColumn);
                    if (rolls.Contains(neighbor)) validNeighbors.Add(neighbor);
                }
            }

            neighbors[roll] = validNeighbors.ToArray();
        }

        return new(rolls, neighbors);
    }

    record struct Position(int Row, int Column);
    record Map(HashSet<Position> Rolls, Dictionary<Position, Position[]> Neighbors);
}