static class Day10
{
    public static void Run(TextReader reader)
    {
        var machines = reader.ReadMachines().ToList();

        var indicatorPresses = machines.Sum(SwitchIndicators);

        var sum = 0;
        foreach (var machine in machines)
        {
            Console.WriteLine($"Solving machine {machine.Joltages.ToLabel()} using {machine.Buttons.ToLabel()}");
            var presses = machine.ReachJoltages();
            sum += presses;
            Console.WriteLine($"  -> {presses} presses");
        }
        var joltagePresses = sum;

        Console.WriteLine($"Minimum button presses: {indicatorPresses}");
        Console.WriteLine($"Minimum joltage button presses: {joltagePresses}");
    }

    private const int Infinite = int.MaxValue;

    private static int ReachJoltages(this Machine machine)
    {
        var result = machine.Joltages.ReachJoltages(machine.Buttons);
        if (result < Infinite) return result;
        throw new InvalidOperationException("Cannot reach target joltages with available buttons.");
    }

    private static ulong iterations = 0;

    private static int ReachJoltages(this Joltages joltages, IEnumerable<Button> buttons)
    {
        if (joltages.Values.Max() == 0) return 0;

        var usefulButtons = buttons
            .Where(button => button.ToggleIndices.All(index => joltages.Values[index] > 0))
            .ToList();

        var positiveJoltages = joltages.Values
            .Select((value, index) => (value, index))
            .Where(pair => pair.value > 0)
            .Select(pair => pair.index)
            .ToList();

        if (!positiveJoltages.All(index => usefulButtons.Any(button => button.ToggleIndices.Contains(index)))) return Infinite;

        var bestButton = usefulButtons.MaxBy(button => button.ToggleIndices.Length);
        if (bestButton == null) return Infinite;

        int maxPresses = bestButton.ToggleIndices.Min(index => joltages.Values[index]);

        for (int presses = maxPresses; presses > 0; presses--)
        {
            if (++iterations % 1_000_000 == 0) Console.WriteLine($"  Iterations: {iterations:#,##0}");
            // Console.WriteLine($"Trying button {bestButton.ToLabel()} x{presses} on {joltages.ToLabel()} using {usefulButtons.ToLabel()}");
            var newJoltages = joltages.Apply(bestButton, presses);
            var result = newJoltages.ReachJoltages(usefulButtons.Except(new[] { bestButton }));
            if (result < Infinite)
            {
                // Console.WriteLine($"Pressed button {bestButton.ToLabel()}x{presses} to reach {newJoltages.ToLabel()}");
                // Console.WriteLine($"Solved {joltages.ToLabel()} in {result}+{presses}={result + presses} presses using {usefulButtons.ToLabel()}");
                return result + presses;
            }
        }

        return Infinite;
    }

    private static string ToLabel(this Button button) =>
        $"[{string.Join(",", button.ToggleIndices)}]";

    private static string ToLabel(this IEnumerable<Button> buttons) =>
        string.Join(", ", buttons.Select(ToLabel));

    private static string ToLabel(this Joltages joltages) =>
        string.Join(",", joltages.Values);

    private static IEnumerable<int> GetMaxButtonPresses(this Joltages joltages, Button[] buttons) =>
        buttons.Select(button => button.ToggleIndices.Max(index => joltages.Values[index]));

    private static IEnumerable<int> GetButtonsCountPerJoltage(this Machine machine) =>
        machine.Joltages.Values.Select((_, index) => machine.Buttons.Count(button => button.ToggleIndices.Contains(index)));

    private static int SwitchIndicators(this Machine machine)
    {
        var maxIndicators = new Indicators(machine.Buttons.Aggregate(0, (acc, button) => acc | button.Toggles));
        
        var minCounts = Enumerable.Repeat(Infinite, maxIndicators.Bits + 1).ToArray();
        minCounts[0] = 0;

        var reached = new HashSet<Indicators> { new Indicators(0) };

        foreach (var button in machine.Buttons)
        {
            var newReached = new HashSet<Indicators>(reached);
            foreach (var indicators in reached)
            {
                var newIndicators = indicators.Apply(button);
                if (minCounts[newIndicators.Bits] > minCounts[indicators.Bits].Add(1))
                {
                    minCounts[newIndicators.Bits] = minCounts[indicators.Bits].Add(1);
                    newReached.Add(newIndicators);
                }
            }
            reached = newReached;
        }

        if (minCounts[machine.Indicators.Bits] < Infinite) return minCounts[machine.Indicators.Bits];

        throw new InvalidDataException("Cannot reach target indicators with available buttons.");
    }

    private static int Add(this int a, int b) =>
        a == Infinite ? Infinite
        : b == Infinite ? Infinite
        : a + b;

    private static Joltages Apply(this Joltages joltages, Button button, int presses)
    {
        var newValues = new int[joltages.Values.Length];
        Array.Copy(joltages.Values, newValues, joltages.Values.Length);
        foreach (var index in button.ToggleIndices) newValues[index] -= presses;
        return new Joltages(newValues);
    }

    private static Indicators Apply(this Indicators indicators, Button button) =>
        new Indicators(indicators.Bits ^ button.Toggles);

    public record Machine(Indicators Indicators, Button[] Buttons, Joltages Joltages);

    public record Joltages(int[] Values);

    public record Button(int Toggles, int[] ToggleIndices);
    public record struct Indicators(int Bits);
}

file static class Day10Parsing
{
    public static IEnumerable<Day10.Machine> ReadMachines(this TextReader reader) =>
        reader.ReadLines().Select(ParseMachine);

    private static Day10.Machine ParseMachine(string line) =>
        new Day10.Machine(line.ParseIndicators(), line.ParseButtons().ToArray(), line.ParseJoltages());

    private static Day10.Joltages ParseJoltages(this string line) =>
        System.Text.RegularExpressions.Regex.Match(line, @"\{(?<numbers>\d+(,\d+)*)\}") is { Success: true } match &&
            match.Groups["numbers"] is { Success: true } group
            ? new Day10.Joltages(group.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray())
            : throw new InvalidDataException($"Invalid joltages line: {line}");

    private static IEnumerable<Day10.Button> ParseButtons(this string line) =>
        System.Text.RegularExpressions.Regex.Matches(line, @"\((?<numbers>\d+(,\d+)*)\)")
            .Where(match => match.Success && match.Groups["numbers"].Success)
            .Select(match => match.Groups["numbers"].Value)
            .Select(ParseButton);

    private static Day10.Button ParseButton(this string toggles) =>
        toggles.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse)
            .ToArray()
            .ToButton();

    private static Day10.Button ToButton(this int[]toggleIndices) =>
        new Day10.Button(toggleIndices.Aggregate(0, (acc, val) => acc | (1 << val)), toggleIndices);

    private static Day10.Indicators ParseIndicators(this string line) =>
        System.Text.RegularExpressions.Regex.Match(line, @"\[(?<indicators>[\.#]+)\]") is { Success: true } match &&
            match.Groups["indicators"] is { Success: true } group
            ? group.Value.ParseBits()
            : throw new InvalidDataException($"Invalid indicators line: {line}");

    private static Day10.Indicators ParseBits(this string remainingBits) =>
        new Day10.Indicators(remainingBits.Select((b, i) => b == '#' ? 1 << i : 0).Sum());    
}