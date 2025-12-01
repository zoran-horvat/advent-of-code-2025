static class Day01
{
    public static void Run(TextReader reader)
    {
        var zeros = reader.ReadMoves().Apply().ToList();

        int zeroEndings = zeros.Count(position => position.after == 0);
        int zeroPasses = zeros.Sum(position => position.CountZeroPasses());

        Console.WriteLine($"     Number of zero positions: {zeroEndings}");
        Console.WriteLine($"Number of passes through zero: {zeroPasses}");
    }
    
    private static int CountZeroPasses(this (int before, int move, int after) step) =>
        Math.Abs(step.move) / 100 +                                             // Full circle
        (step.before > 0 && step.before + (step.move % 100) <= 0 ? 1 : 0) +     // Crossed zero going down
        (step.before > 0 && step.before + (step.move % 100) >= 100 ? 1 : 0);    // Crossed zero going up

    private static IEnumerable<(int before, int move, int after)> Apply(this IEnumerable<int> moves)
    {
        int position = 50;
        foreach (var move in moves)
        {
            int before = position;
            position = position.Apply(move);
            yield return (before, move, position);
        }
    }

    private static int Apply(this int position, int move) =>
        ((position + move) % 100 + 100) % 100;

    private static IEnumerable<int> ReadMoves(this TextReader reader) =>
        reader.ReadLines().Select(ParseMove);

    private static int ParseMove(string move) =>
        move[0] == 'L' ? -int.Parse(move[1..]) : int.Parse(move[1..]);
}