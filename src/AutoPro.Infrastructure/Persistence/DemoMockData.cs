using AutoPro.Domain.Enums;

namespace AutoPro.Infrastructure.Persistence;

public static class DemoMockData
{
    public const string JobNumberPrefix = "DEMO-2026-";
    public const string DemoPhonePrefix = "0772000";

    public readonly record struct CustomerRow(
        int Index,
        string FullName,
        string Phone,
        string? Email);

    public readonly record struct VehicleRow(
        int Index,
        int CustomerIndex,
        string PlatePrefix,
        string PlateSuffix,
        string QrToken,
        string? Make,
        string? Model,
        int? Year);

    public readonly record struct SupplierRow(
        int Index,
        string Name,
        string? ContactPerson,
        string? Phone);

    public readonly record struct JobRow(
        int Index,
        int? VehicleIndex,
        string JobNumber,
        JobType JobType,
        JobStatus Status,
        int? OdometerKm,
        string? JobTypeOtherDescription,
        decimal? CustomServiceChargeLkr,
        string? StoreCustomerName,
        string? Notes,
        int DaysAgo,
        bool WithInventoryPurchase);

    public static IReadOnlyList<CustomerRow> Customers { get; } =
    [
        new(1, "Nimal Perera", $"{DemoPhonePrefix}001", "nimal.perera@example.com"),
        new(2, "Sanduni Fernando", $"{DemoPhonePrefix}002", "sanduni.f@example.com"),
        new(3, "Kasun Silva", $"{DemoPhonePrefix}003", null),
        new(4, "Ayesha Jayawardena", $"{DemoPhonePrefix}004", "ayesha.j@example.com"),
        new(5, "Rohan Wickramasinghe", $"{DemoPhonePrefix}005", null),
    ];

    public static IReadOnlyList<VehicleRow> Vehicles { get; } =
    [
        new(1, 1, "CAP", "4521", "demoseedvehicleqr00000000000001", "Toyota", "Corolla", 2018),
        new(2, 1, "CAP", "8890", "demoseedvehicleqr00000000000002", "Toyota", "Vitz", 2016),
        new(3, 2, "KAB", "1123", "demoseedvehicleqr00000000000003", "Suzuki", "Wagon R", 2019),
        new(4, 3, "LD", "5544", "demoseedvehicleqr00000000000004", "Nissan", "Sunny", 2015),
        new(5, 3, "WP", "9012", "demoseedvehicleqr00000000000005", "Honda", "Fit", 2020),
        new(6, 4, "CAR", "3301", "demoseedvehicleqr00000000000006", "Mazda", "3", 2017),
        new(7, 5, "NE", "7788", "demoseedvehicleqr00000000000007", "Mitsubishi", "Lancer", 2014),
        new(8, 5, "NE", "7789", "demoseedvehicleqr00000000000008", "Hyundai", "Elantra", 2021),
    ];

    public static IReadOnlyList<SupplierRow> Suppliers { get; } =
    [
        new(1, "Lanka Auto Parts (Demo)", "Mr. Dias", "0112345001"),
        new(2, "Colombo Spares Hub (Demo)", "Ms. Perera", "0112345002"),
        new(3, "Island Oil & Filters (Demo)", null, "0773000001"),
    ];

    public static IReadOnlyList<JobRow> Jobs { get; } =
    [
        new(1, 1, $"{JobNumberPrefix}00001", JobType.Wash, JobStatus.OnGoing, 62_400, null, null, null, null, 0, false),
        new(2, 2, $"{JobNumberPrefix}00002", JobType.FullService, JobStatus.OnHold, 88_200, null, null, null, null, 1, true),
        new(3, 3, $"{JobNumberPrefix}00003", JobType.Repair, JobStatus.OnGoing, 41_500, null, null, null, null, 0, true),
        new(4, 4, $"{JobNumberPrefix}00004", JobType.Wash, JobStatus.Completed, 95_000, null, null, null, null, 3, false),
        new(5, 5, $"{JobNumberPrefix}00005", JobType.FullService, JobStatus.Closed, 72_300, null, null, null, null, 7, true),
        new(6, 6, $"{JobNumberPrefix}00006", JobType.Repair, JobStatus.Closed, 118_900, null, null, null, null, 14, true),
        new(7, 7, $"{JobNumberPrefix}00007", JobType.Wash, JobStatus.Cancelled, 210_000, null, null, null, "Customer rescheduled", 2, false),
        new(8, 8, $"{JobNumberPrefix}00008", JobType.Other, JobStatus.OnGoing, 15_600, "AC gas recharge", 4500m, null, null, 0, false),
        new(9, null, $"{JobNumberPrefix}00009", JobType.StoreSale, JobStatus.OnGoing, null, null, null, "Walk-in customer", "Counter sale — demo", 0, true),
        new(10, 1, $"{JobNumberPrefix}00010", JobType.Wash, JobStatus.Completed, 62_800, null, null, null, null, 5, false),
    ];

    public static DateTime AnchorUtc { get; } = new(2026, 3, 13, 8, 0, 0, DateTimeKind.Utc);

    public static DateTime CreatedAt(int daysAgo) => AnchorUtc.AddDays(-daysAgo);
}