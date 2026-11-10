using AutoPro.Domain.Enums;

namespace AutoPro.Application.Contracts.Jobs;

public record UpdateJobStatusRequest(JobStatus Status, string? Note);
