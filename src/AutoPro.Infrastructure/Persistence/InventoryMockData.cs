namespace AutoPro.Infrastructure.Persistence;

public static class InventoryMockData
{
    public const string PartNumberPrefix = "MOCK-";
    public const int DefaultMockCount = 250;
    public const int RandomSeed = 2026_03_13;

    private static readonly string[] PartTypes =
    [
        "Oil filter", "Air filter", "Cabin filter", "Fuel filter", "Spark plug",
        "Brake pad set", "Brake rotor", "Brake caliper", "Brake hose", "Brake fluid",
        "Wiper blade", "Headlamp bulb", "Fog lamp", "Tail lamp assembly", "Turn signal lamp",
        "Serpentine belt", "Timing belt kit", "Tensioner pulley", "Idler pulley", "Drive belt",
        "Radiator", "Radiator cap", "Thermostat", "Coolant hose", "Water pump",
        "Alternator", "Starter motor", "Battery", "Voltage regulator", "Fusible link",
        "Shock absorber", "Strut assembly", "Coil spring", "Control arm", "Ball joint",
        "Tie rod end", "Stabilizer link", "Wheel bearing", "Hub assembly", "CV axle",
        "Engine mount", "Transmission mount", "Exhaust gasket", "Muffler", "Catalytic converter",
        "Oxygen sensor", "MAF sensor", "MAP sensor", "Crankshaft sensor", "Camshaft sensor",
        "Ignition coil", "Fuel pump", "Fuel injector", "Throttle body", "EGR valve",
        "PCV valve", "Valve cover gasket", "Head gasket set", "Oil pan gasket", "Oil drain plug",
        "Wheel nut", "Lug bolt", "Wheel stud", "Tire valve stem", "TPMS sensor",
        "Power steering fluid", "Transmission fluid", "Gear oil", "Grease cartridge", "Thread locker",
        "Shop towel roll", "Degreaser", "Brake cleaner", "Contact cleaner", "Dielectric grease",
    ];

    private static readonly string[] FitmentHints =
    [
        "universal", "compact sedan", "SUV", "pickup", "hatchback", "van",
        "1.5L", "1.8L", "2.0L", "2.4L", "3.5L V6", "diesel",
        "front", "rear", "left", "right", "pair",
    ];

    public readonly record struct MockInventoryRow(
        Guid Id,
        string PartNumber,
        string Name,
        string? Notes,
        decimal QuantityOnHand,
        decimal UnitPrice,
        bool IsActive,
        DateTime CreatedAtUtc);

    public static IEnumerable<MockInventoryRow> GenerateRows(int count, DateTime? createdAtUtc = null)
    {
        if (count <= 0)
            yield break;

        var random = new Random(RandomSeed);
        var createdAt = createdAtUtc ?? new DateTime(2026, 3, 13, 0, 0, 0, DateTimeKind.Utc);

        for (var index = 1; index <= count; index++)
        {
            var partNumber = $"{PartNumberPrefix}{index:D5}";
            var partType = PartTypes[(index - 1) % PartTypes.Length];
            var hint = FitmentHints[random.Next(FitmentHints.Length)];
            var name = $"{partType} ({hint})";
            if (name.Length > 200)
                name = name[..200];

            var quantity = random.Next(0, 401);
            var unitPrice = Math.Round((decimal)(random.NextDouble() * 480 + 4.99), 2);

            yield return new MockInventoryRow(
                DeterministicId(index),
                partNumber,
                name,
                index % 7 == 0 ? "Demo stock — mock seed" : null,
                quantity,
                unitPrice,
                index % 23 != 0,
                createdAt);
        }
    }

  /// <summary>Stable IDs so migrations can insert and roll back predictably.</summary>
    public static Guid DeterministicId(int index) =>
        Guid.Parse($"22222222-2222-4000-8000-{index:X12}");

    public static string SqlLiteral(string? value)
    {
        if (value is null)
            return "NULL";
        return $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
    }
}
