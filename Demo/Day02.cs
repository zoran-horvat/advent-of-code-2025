using System.Diagnostics;

static class Day02
{
    public static void Run(TextReader reader)
    {
        var ranges = reader.ReadRanges().ToList();

        Stopwatch sw = Stopwatch.StartNew();

        var (halfCutSum, allCutsSum) = ranges.SelectMany(range => range.EnumerateInvalidIds()).Sum();
        sw.Stop();

        Console.WriteLine($"Sum of all invalid IDs (half-split): {halfCutSum}");
        Console.WriteLine($"Sum of all invalid IDs (any split):  {allCutsSum}");
        Console.WriteLine($"Elapsed time: {sw.Elapsed}");
   }

   private static (ulong halfCutSum, ulong allCutsSum) Sum(this IEnumerable<(ulong number, bool isHalfSplit)> invalidIds) =>
        invalidIds.Aggregate((halfCutSum: 0UL, allCutsSum: 0UL), (acc, t) => (
            t.isHalfSplit ? acc.halfCutSum + t.number : acc.halfCutSum,
            acc.allCutsSum + t.number));

    private static IEnumerable<(ulong number, bool isHalfSplit)> EnumerateInvalidIds(this Range range)
    {
        List<(bool isHalfSplit, IEnumerator<ulong> enumerator)> candidates = new();
        for (int groupSize = 1; groupSize <= range.DigitsCount / 2; groupSize++)
        {
            if (range.DigitsCount % groupSize != 0) continue;
            var enumerator = range.EnumerateInvalidIds(groupSize).GetEnumerator();
            if (enumerator.MoveNext()) candidates.Add((range.DigitsCount % 2 == 0 && groupSize == range.DigitsCount / 2, enumerator));
        }

        while (candidates.Count > 0)
        {
            ulong minNumber = candidates.Min(c => c.enumerator.Current);
            bool isHalfSplit = candidates.Any(c => c.isHalfSplit && c.enumerator.Current == minNumber);
            yield return (minNumber, isHalfSplit);

            candidates = candidates
                .Where(c => c.enumerator.Current != minNumber || c.enumerator.MoveNext())
                .ToList();
        }
    }

    private static IEnumerable<ulong> EnumerateInvalidIds(this Range range, int groupSize)
    {
        if (range.DigitsCount % groupSize != 0) yield break;

        ulong divisor = groupSize.GetDivisor();
        var segments = range.ToSegmentsFromLowest(divisor).Reverse().ToList();

        var mostSignificant = segments[0];

        if (segments.IsValidSeed(mostSignificant.From, mostSignificant)) yield return mostSignificant.From.ToId(segments.Count, divisor);

        for (ulong seed = mostSignificant.From + 1; seed < mostSignificant.To; seed++)
        {
            yield return seed.ToId(segments.Count, divisor);
        }

        if (mostSignificant.To > mostSignificant.From && segments.IsValidSeed(mostSignificant.To, mostSignificant)) yield return mostSignificant.To.ToId(segments.Count, divisor);
    }

    private static bool IsValidSeed(this IEnumerable<Segment> segments, ulong seed, Segment mostSignificant)
    {
        if (seed == mostSignificant.From && segments.SkipWhile(s => s.From == mostSignificant.From).Take(1).Any(s => s.From > mostSignificant.From)) return false;
        if (seed == mostSignificant.To && segments.SkipWhile(s => s.To == mostSignificant.To).Take(1).Any(s => s.To < mostSignificant.To)) return false;
        return true;
    }

    private static ulong ToId(this ulong segmentValue, int segmentsCount, ulong multiplier) =>
        Enumerable.Repeat(segmentValue, segmentsCount).Aggregate(0UL, (acc, val) => acc * multiplier + val);

    private static IEnumerable<Segment> ToSegmentsFromLowest(this Range range, ulong divisor)
    {
        ulong from = range.From;
        ulong to = range.To;

        while (from > 0 || to > 0)
        {
            yield return new Segment(from % divisor, to % divisor);
            from /= divisor;
            to /= divisor;
        }
    }

    private static ulong GetDivisor(this int digitsCount) =>
        (ulong)Math.Pow(10, digitsCount);

    private static IEnumerable<Range> ReadRanges(this TextReader reader) => 
        reader.ReadLines()
            .SelectMany(line => line.Split(','))
            .Where(pair => !string.IsNullOrWhiteSpace(pair))
            .Select(pair => pair.Split('-'))
            .Select(pair => (from: ulong.Parse(pair[0]), to: ulong.Parse(pair[1])))
            .SelectMany(ToRanges);

    private static IEnumerable<Range> ToRanges(this (ulong from, ulong to) bounds)
    {
        int fromDigits = bounds.from.CountDigits();
        int toDigits = bounds.to.CountDigits();
        for (int digits = fromDigits; digits <= toDigits; digits++)
        {
            ulong newTo = Math.Min(bounds.to, (ulong)Math.Pow(10, digits) - 1);
            yield return new Range(bounds.from, newTo, digits);
            bounds.from = newTo + 1;
            if (bounds.from > bounds.to) yield break;
        }
    }

    private static int CountDigits(this ulong number) =>
        number == 0 ? 1 : (int)Math.Floor(Math.Log10(number)) + 1;

    record NumbersRange(ulong From, ulong To);
    record Split(ulong Number, int PartsCount);

    record Segment(ulong From, ulong To);
    record Range(ulong From, ulong To, int DigitsCount);
}