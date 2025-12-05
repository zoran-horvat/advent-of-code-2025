using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

Action<TextReader>[] problemSolutions =
[
    Day01.Run, Day02.Run, Day03.Run
];

foreach ((int fromIndex, int toIndex) in ProblemIndices())
{
    TimeSpan totalTime = TimeSpan.Zero;

    for (int i = fromIndex; i <= toIndex; i++)
    {
        if (fromIndex != toIndex)
        {
            Console.WriteLine($"Day {i + 1}:");
            Console.WriteLine();
        }

        var inputs = LocateInputs(i).ToList();

        foreach (var (label, reader) in inputs)
        {
            if (inputs.Count > 1) Console.WriteLine($"--- Input: {label} ---");
            var stopwatch = Stopwatch.StartNew();
            problemSolutions[i](reader);
            stopwatch.Stop();
            totalTime += stopwatch.Elapsed;
            Console.WriteLine($"Done in: {stopwatch.Elapsed}");
            Console.WriteLine();
        }
        
        if (fromIndex != toIndex && i < toIndex) Console.WriteLine(new string('-', 80));
    }

    Console.WriteLine($"Total execution time: {totalTime}");
}

IEnumerable<(string label, TextReader reader)> LocateInputs(int problemIndex)
{
    var assembly = typeof(Common).Assembly;
    string resourcePrefix = $"{assembly.GetName().Name}.Inputs.";
    string dayPrefix = $"Day{problemIndex + 1:D2}.";

    var resources = assembly
        .GetManifestResourceNames()
        .Where(name =>
            name.StartsWith(resourcePrefix + dayPrefix, StringComparison.OrdinalIgnoreCase) &&
            name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        .OrderBy(name => ExtractInputLabel(name, string.Empty), StringComparer.OrdinalIgnoreCase)
        .ToArray();

    if (resources.Length == 0)
    {
        yield return (label: "Standard Input", reader: Console.In);
        yield break;
    }

    foreach (var resource in resources)
    {
        var stream = assembly.GetManifestResourceStream(resource);
        var reader = stream is null ? Console.In : new StreamReader(LoadInMemory(stream));

        yield return (ExtractInputLabel(resource, "default"), reader);
    }
}

Stream LoadInMemory(Stream input)
{
    var memoryStream = new MemoryStream();
    input.CopyTo(memoryStream);
    memoryStream.Position = 0;
    return memoryStream;
}

string ExtractInputLabel(string fileName, string defaultLabel)
{
    var match = Regex.Match(fileName, @"Day\d{2}(\.(?<label>.+))?\.txt$");
    if (match.Success && match.Groups["label"].Value is { Length: > 0 } label) return label;
    return defaultLabel;
}

IEnumerable<(int from, int to)> ProblemIndices()
{
    string prompt = $"{Environment.NewLine}Enter the day number [1-{problemSolutions.Length}] (A = all, ENTER = quit): ";
    Console.Write(prompt);
    while (true)
    {
        string input = Console.ReadLine() ?? string.Empty;

        if (string.IsNullOrEmpty(input)) yield break;
        else if (input == "A" || input == "a") yield return (0, problemSolutions.Length - 1);
        else if (int.TryParse(input, out int number) && number >= 1 && number <= problemSolutions.Length) yield return (number - 1, number - 1);

        Console.Write(prompt);
    }
}