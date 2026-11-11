using AutoPro.Domain.Enums;

namespace AutoPro.Domain.Services;

public static class JobStatusRules
{
  private static readonly HashSet<(JobStatus From, JobStatus To)> CustomerRepTransitions =
  [
    (JobStatus.OnGoing, JobStatus.OnHold),
    (JobStatus.OnHold, JobStatus.OnGoing),
    (JobStatus.OnGoing, JobStatus.Cancelled),
    (JobStatus.OnHold, JobStatus.Cancelled),
    (JobStatus.OnGoing, JobStatus.Completed),
    (JobStatus.OnHold, JobStatus.Completed),
  ];

  public static bool CanCustomerRepresentativeChange(JobStatus from, JobStatus to) =>
    CustomerRepTransitions.Contains((from, to));

  public static bool IsTerminalForCustomerRep(JobStatus status) =>
    status is JobStatus.Cancelled or JobStatus.Completed or JobStatus.Closed;

  public static bool CanModifyPurchases(JobStatus status) =>
    status is JobStatus.OnGoing or JobStatus.OnHold;

  public static bool CanAdministrativeOfficerClose(JobStatus status) =>
    status == JobStatus.Completed;
}
