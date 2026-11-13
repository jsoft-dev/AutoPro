namespace AutoPro.Domain.Services;

public static class VehicleOdometerRules
{
    public static string? ValidateNewReading(int? newReading, int? lastRecordedKm)
    {
        if (newReading is < 0)
            return "Odometer cannot be negative.";

        if (newReading is null || lastRecordedKm is null)
            return null;

        if (newReading.Value < lastRecordedKm.Value)
        {
            return $"Odometer cannot be less than the last recorded reading ({lastRecordedKm.Value:N0} km) for this vehicle.";
        }

        return null;
    }
}
