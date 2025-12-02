Action<TextReader>[] problemSolutions =
[
    Day01.Run, Day02.Run
];

foreach ((int fromIndex, int toIndex) in ProblemIndices())
{
    if (fromIndex == toIndex)
    {
        problemSolutions[fromIndex](LocateInput(fromIndex));
        continue;
    }

    for (int i = fromIndex; i <= toIndex; i++)
    {
        Console.WriteLine($"Day {i + 1}:");
        Console.WriteLine();

        problemSolutions[i](LocateInput(i));
        
        if (i < toIndex) Console.WriteLine(new string('-', 80));
    }
}

TextReader LocateInput(int problemIndex) =>
    Directory.GetFiles(Directory.GetCurrentDirectory(), $"Day{problemIndex + 1:D2}.txt", SearchOption.AllDirectories) switch
    {
        [var file, ..] => new StreamReader(file),
        _ => Console.In
    };

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