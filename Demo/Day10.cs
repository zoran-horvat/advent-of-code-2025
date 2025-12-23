using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

static class Day10
{
    public static void Run(TextReader reader)
    {
        var machines = reader.ReadMachines().ToList();

        var minButtonPressesIndicators = machines.Sum(SwitchIndicators);
        var minButtonPressesJoltages = machines.Sum(GetMinimumPresses);

        Console.WriteLine($"Minimum button presses for indicators: {minButtonPressesIndicators}");
        Console.WriteLine($"Minimum button presses for joltages:   {minButtonPressesJoltages}");
    }

    private static int GetMinimumPresses(this Machine machine)
    {
        var system = machine.ToEquations().Reduce();
        int buttonsCount = system.Equations[0].ButtonWeight.Length;

        var currentMin = system.ButtonMaximums.Sum() + 1;
        var min = system.GetMinimumPresses(buttonsCount - 1, new int[buttonsCount], 0, ref currentMin);
        return min;
    }

    private static int GetMinimumPresses(this ReducedEquations system, int buttonIndex, int[] presses, int currentPresses, ref int currentMin)
    {
        if (system.ButtonReductions[buttonIndex].Length == 0) return system.GetFreeVariableMinimumPresses(buttonIndex, presses, currentPresses, ref currentMin);

        var actual = system.ButtonReductions[buttonIndex].Resolve(presses);
        if (actual is null) return currentMin;

        currentPresses += actual.Value;
        if (buttonIndex == 0) return Math.Min(currentMin, currentPresses);

        presses[buttonIndex] = actual.Value;
        var newMin = system.GetMinimumPresses(buttonIndex - 1, presses, currentPresses, ref currentMin);
        presses[buttonIndex] = 0;
        return newMin;
    }

    private static int GetFreeVariableMinimumPresses(this ReducedEquations system, int buttonIndex, int[] presses, int currentPresses, ref int currentMin)
    {
        if (buttonIndex == 0) return currentMin;

        var max = Math.Min(currentMin - currentPresses - 1, system.ButtonMaximums[buttonIndex]);
        if (max < 0) return currentMin;

        for (int i = 0; i <= Math.Min(max, currentMin - currentPresses - 1); i++)
        {
            presses[buttonIndex] = i;
            var newPresses = currentPresses + i;
            var newMin = system.GetMinimumPresses(buttonIndex - 1, presses, newPresses, ref currentMin);
            presses[buttonIndex] = 0;
            
            currentMin = Math.Min(currentMin, newMin);
        }

        return currentMin;
    }

    private static int? Resolve(this Reduction[] buttonReductions, int[] presses)
    {
        int? first = buttonReductions[0].Resolve(presses);
        if (first is null) return null;
        if (buttonReductions.Length == 1) return first;

        for (int i = 1; i < buttonReductions.Length; i++)
        {
            int? next = buttonReductions[i].Resolve(presses);
            if (next is null || next != first) return null;
        }

        return first;
    }

    private static int? Resolve(this Reduction buttonReduction, int[] presses)
    {
        int remainingPresses = buttonReduction.Target;
        foreach (var (index, factor) in buttonReduction.ButtonEffects) remainingPresses -= factor * presses[index];
        if (remainingPresses % buttonReduction.ButtonWeight != 0) return null;
        var actual = remainingPresses / buttonReduction.ButtonWeight;
        if (actual < 0) return null;
        return actual;
    }

    private static ReducedEquations Reduce(this EquationSystem system)
    {
        var equations = system.Equations.Reduce().ToArray();
        var buttonReductions = equations.ToButtonReductions(equations[0].ButtonWeight.Length);
        var buttonMaximums = equations.GetTightButtonMaximums();
        return new ReducedEquations(equations, buttonReductions, buttonMaximums);
    }

    private static int[] GetTightButtonMaximums(this Equation[] system)
    {
        int?[] maximums = new int?[system[0].ButtonWeight.Length];
        bool changed = true;

        while (changed)
        {
            changed = false;

            for (int button = 0; button < maximums.Length; button++)
            {
                foreach (var eq in system)
                {
                    if (eq.ButtonWeight[button] == 0) continue;
                    int? target = eq.Target;
                    for (int otherButton = 0; otherButton < maximums.Length; otherButton++)
                    {
                        if (otherButton == button) continue;
                        if (eq.ButtonWeight[otherButton] == 0) continue;
                        if (Math.Sign(eq.ButtonWeight[otherButton]) == Math.Sign(eq.ButtonWeight[button])) continue;

                        var otherMaximum = maximums[otherButton];
                        if (otherMaximum is null)
                        {
                            target = null;
                            break;
                        }
                        target -= eq.ButtonWeight[otherButton] * otherMaximum.Value;
                    }

                    if (target is null) continue;
                    int candidate = Math.Max(target.Value / eq.ButtonWeight[button], 0);
                    if (maximums[button] is null || candidate < maximums[button])
                    {
                        maximums[button] = candidate;
                        changed = true;
                    }
                }
            }
        }

        return maximums.Select(m => m ?? int.MaxValue).ToArray();
    }

    private static IEnumerable<Equation> Reduce(this Equation[] system)
    {
        int reductionsCount = Math.Min(system.Length - 1, system[0].ButtonWeight.Length - 1);

        for (int row = 0; row < reductionsCount; row++)
        {
            int pivot = row;
            while (pivot < system.Length && system[pivot].ButtonWeight[row] == 0) pivot += 1;
            if (pivot >= system.Length) continue;

            if (pivot != row) (system[pivot], system[row]) = (system[row], system[pivot]);

            for (int targetRow = row + 1; targetRow < system.Length; targetRow++)
            {
                if (system[targetRow].ButtonWeight[row] == 0) continue;

                system[targetRow] = system[targetRow].Multiply(system[row].ButtonWeight[row])
                    .Subtract(system[row].Multiply(system[targetRow].ButtonWeight[row]));
            }
        }

        return system.MakeSquare();
    }

    private static Reduction[][] ToButtonReductions(this IEnumerable<Equation> equations, int buttonsCount)
    {
        var map = equations.Select(ToReduction)
            .OfType<Reduction>()
            .GroupBy(reduction => reduction.ButtonIndex)
            .ToDictionary(group => group.Key, group => group.ToArray()!);

        return Enumerable.Range(0, buttonsCount)
            .Select(buttonIndex => map.GetValueOrDefault(buttonIndex, Array.Empty<Reduction>()))
            .ToArray();
    }

    private static Reduction? ToReduction(this Equation equation)
    {
        int buttonIndex = 0;
        while (buttonIndex < equation.ButtonWeight.Length && equation.ButtonWeight[buttonIndex] == 0) buttonIndex++;
        if (buttonIndex >= equation.ButtonWeight.Length) return null;

        var buttonWeight = equation.ButtonWeight[buttonIndex];
        var target = equation.Target;
        var buttonEffects = new List<(int buttonIndex, int factor)>();

        for (int i = buttonIndex + 1; i < equation.ButtonWeight.Length; i++)
        {
            if (equation.ButtonWeight[i] == 0) continue;
            buttonEffects.Add((i, equation.ButtonWeight[i]));
        }

        return new Reduction(buttonIndex, buttonWeight, target, buttonEffects.ToArray());
    }

    private static IEnumerable<Equation> MakeSquare(this IEnumerable<Equation> equations)
    {
        var list = equations.ToList();
        list.AddRange(Enumerable.Repeat(
            new Equation(new int[list[0].ButtonWeight.Length], 0),
            Math.Max(0, list[0].ButtonWeight.Length - list.Count)));
        return list;
    }

    private static Equation Subtract(this Equation minuend, Equation subtrahend) =>
        new(minuend.ButtonWeight.Zip(subtrahend.ButtonWeight).Select(pair => pair.First - pair.Second).ToArray(),
            minuend.Target - subtrahend.Target);

    private static Equation Multiply(this Equation equation, int factor) =>
        new(equation.ButtonWeight.Select(w => w * factor).ToArray(), equation.Target * factor);

    private static EquationSystem ToEquations(this Machine machine) =>
        new EquationSystem(machine.Joltages.Values
            .Select((joltage, index) => machine.Buttons.ToEquation(index, joltage))
            .ToArray());

    private static Equation ToEquation(this Button[] buttons, int index, int target)
    {
        int[] factors = new int[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].ToggleIndices.Contains(index)) factors[i] = 1;
        }

        return new Equation(factors, target);
    }

    private static int SwitchIndicators(this Machine machine)
    {
        var maxIndicators = new Indicators(machine.Buttons.Aggregate(0, (acc, button) => acc | button.Toggles));
        
        var infinite = int.MaxValue;
        var minCounts = Enumerable.Repeat(infinite, maxIndicators.Bits + 1).ToArray();
        minCounts[0] = 0;

        var reached = new HashSet<Indicators> { new Indicators(0) };

        foreach (var button in machine.Buttons)
        {
            var newReached = new HashSet<Indicators>(reached);
            foreach (var indicators in reached)
            {
                var newIndicators = indicators.Apply(button);
                if (minCounts[newIndicators.Bits] > minCounts[indicators.Bits] + 1)
                {
                    minCounts[newIndicators.Bits] = minCounts[indicators.Bits] + 1;
                    newReached.Add(newIndicators);
                }
            }
            reached = newReached;
        }

        if (minCounts[machine.Indicators.Bits] < infinite) return minCounts[machine.Indicators.Bits];

        throw new InvalidDataException("Cannot reach target indicators with available buttons.");
    }

    private static Indicators Apply(this Indicators indicators, Button button) =>
        new Indicators(indicators.Bits ^ button.Toggles);

    private static IEnumerable<Machine> ReadMachines(this TextReader reader) =>
        reader.ReadLines().Select(ParseMachine);

    private static Machine ParseMachine(string line) =>
        new Machine(line.ParseIndicators(), line.ParseButtons().ToArray(), line.ParseJoltages());

    private static Joltages ParseJoltages(this string line) =>
        Regex.Match(line, @"\{(?<numbers>\d+(,\d+)*)\}") is { Success: true } match &&
            match.Groups["numbers"] is { Success: true } group
            ? new Joltages(group.Value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray())
            : throw new InvalidDataException($"Invalid joltages line: {line}");

    private static IEnumerable<Button> ParseButtons(this string line) =>
        Regex.Matches(line, @"\((?<numbers>\d+(,\d+)*)\)")
            .Where(match => match.Success && match.Groups["numbers"].Success)
            .Select(match => match.Groups["numbers"].Value)
            .Select(ParseButton);

    private static Button ParseButton(this string toggles) =>
        toggles.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse)
            .ToArray()
            .ToButton();

    private static Button ToButton(this int[]toggleIndices) =>
        new Button(toggleIndices.Aggregate(0, (acc, val) => acc | (1 << val)), toggleIndices);

    private static Indicators ParseIndicators(this string line) =>
        Regex.Match(line, @"\[(?<indicators>[\.#]+)\]") is { Success: true } match &&
            match.Groups["indicators"] is { Success: true } group
            ? group.Value.ParseBits()
            : throw new InvalidDataException($"Invalid indicators line: {line}");

    private static Indicators ParseBits(this string remainingBits) =>
        new Indicators(remainingBits.Select((b, i) => b == '#' ? 1 << i : 0).Sum());

    class JoltageComparer : IEqualityComparer<Joltages>
    {
        public bool Equals(Joltages? x, Joltages? y) =>
            x is null ? y is null
            : y is null ? false
            : x.Values.SequenceEqual(y.Values);

        public int GetHashCode([DisallowNull] Joltages obj) =>
            obj.Values.Aggregate(0, (acc, val) => HashCode.Combine(acc, val));
    }

    record ReducedEquations(Equation[] Equations, Reduction[][] ButtonReductions, int[] ButtonMaximums);

    record Reduction(int ButtonIndex, int ButtonWeight, int Target, (int buttonIndex, int factor)[] ButtonEffects);

    record EquationSystem(Equation[] Equations);
    record Equation(int[] ButtonWeight, int Target);

    record Machine(Indicators Indicators, Button[] Buttons, Joltages Joltages);

    record Joltages(int[] Values);

    record Button(int Toggles, int[] ToggleIndices);
    record struct Indicators(int Bits);
}