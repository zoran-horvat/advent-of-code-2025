static class Day02
{
    public static void Run(TextReader reader)
    {
        var ranges = reader.ReadRanges().ToList();
        var repeatingSplits = ranges.SelectMany(GetNumbers).SelectMany(GetSplit).ToList();

        ulong sumInvalidHalf = repeatingSplits.Where(split => split.PartsCount == 2).Sum();
        ulong sumInvalidAny = repeatingSplits.Sum();

        Console.WriteLine($"Sum of all invalid IDs (half-split): {sumInvalidHalf}");
        Console.WriteLine($"Sum of all invalid IDs (any split):  {sumInvalidAny}");
    }

    private static ulong Sum(this IEnumerable<Split> splits) =>
        splits.Aggregate(0UL, (acc, val) => acc + val.Number);
    
    private static IEnumerable<Split> GetSplit(this ulong number)
    {
        int digitsCount = (int)Math.Ceiling(Math.Log10(number));

        ulong splitPower = (ulong)Math.Pow(10, digitsCount / 2 + 1);
        for (int groupLength = digitsCount / 2; groupLength > 0; groupLength--)
        {
            splitPower /= 10;
            if (digitsCount % groupLength != 0) continue;
            if (!number.AllPartsEqual(splitPower)) continue;
            yield return new Split(number, digitsCount / groupLength);
            break;
        }

    }

    private static bool AllPartsEqual(this ulong number, ulong splitPower)
    {
        ulong firstPart = number % splitPower;
        while (number > 0)
        {
            if ((number % splitPower) != firstPart) return false;
            number /= splitPower;
        }
        return true;
    }

    private static IEnumerable<ulong> GetNumbers(Range range)
    {
        for (ulong num = range.From; num <= range.To; num++) yield return num;
    }

    private static IEnumerable<Range> ReadRanges(this TextReader reader) => 
        reader.ReadLines()
            .SelectMany(line => line.Split(','))
            .Where(pair => !string.IsNullOrWhiteSpace(pair))
            .Select(pair => pair.Split('-'))
            .Select(ends => new Range(ulong.Parse(ends[0]), ulong.Parse(ends[1])));

    record Split(ulong Number, int PartsCount);
    record Range(ulong From, ulong To);
}