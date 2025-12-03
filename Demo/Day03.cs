static class Day03
{
    public static void Run(TextReader reader)
    {
        var batteryRanks = reader.ReadBatteryBanks().ToList();

        var totalJoltage2 = batteryRanks.Sum(GetTwoBatteryRating);
        var totalJoltage12 = batteryRanks.Sum(GetTwelveBatteryRating);

        Console.WriteLine($"Total joltage (2 batteries):  {totalJoltage2}");
        Console.WriteLine($"Total joltage (12 batteries): {totalJoltage12}");
    }

    private static long GetTwoBatteryRating(this BatteryBank bank) =>
        bank.SelectBatteries(2).Join();
    
    private static long GetTwelveBatteryRating(this BatteryBank bank) =>
        bank.SelectBatteries(12).Join();

    private static long Join(this IEnumerable<Battery> batteries) =>
        batteries.OrderBy(b => b.Index).Aggregate(0L, (acc, b) => acc * 10 + b.Joltage);

    private static IEnumerable<Battery> SelectBatteries(this BatteryBank bank, int remainingSlots)
    {
        var firstAvailableBattery = 0;

        while (remainingSlots > 0)
        {
            var candidates = bank.Batteries
                .Where(b => b.Index >= firstAvailableBattery)
                .Where(b => b.Index <= bank.Batteries.Length - remainingSlots);
            var joltage = candidates.Max(b => b.Joltage);
            var selectedBattery = candidates.Where(b => b.Joltage == joltage).MinBy(b => b.Index)!;

            yield return selectedBattery;

            firstAvailableBattery = selectedBattery.Index + 1;
            remainingSlots--;
        }
    }

    private static IEnumerable<BatteryBank> ReadBatteryBanks(this TextReader reader) =>
        reader.ReadLines().Select(ToBatteryBank);

    private static BatteryBank ToBatteryBank(this string line) =>
        new BatteryBank(line.Select((c, i) => c.ToBattery(i, line.Length)).ToArray());
    
    private static Battery ToBattery(this char c, int index, int bankSize) =>
        new Battery(int.Parse(c.ToString()), index, bankSize);

    record BatteryBank(Battery[] Batteries);
    
    record Battery(int Joltage, int Index, int BankSize);
}