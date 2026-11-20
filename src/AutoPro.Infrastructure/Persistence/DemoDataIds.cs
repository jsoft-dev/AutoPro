namespace AutoPro.Infrastructure.Persistence;

/// <summary>Stable GUIDs for demo seed data so re-runs and docs can reference fixed records.</summary>
public static class DemoDataIds
{
    public static readonly Guid DemoCrUserId = Guid.Parse("11111111-1111-1111-1111-111111111001");
    public static readonly Guid DemoAoUserId = Guid.Parse("11111111-1111-1111-1111-111111111002");

    /// <summary>First demo customer — presence indicates the business demo pack was applied.</summary>
    public static readonly Guid MarkerCustomerId = EntityId(1);

    public static Guid EntityId(int index) =>
        Guid.Parse($"33333333-3333-4000-8000-{index:X12}");
}