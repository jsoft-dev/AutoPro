namespace AutoPro.Application.Contracts.Vehicles;

public record VehicleCreatedResponse(
    Guid VehicleId,
    string QrToken,
    string QrPayload);
