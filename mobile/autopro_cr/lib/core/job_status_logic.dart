import '../models/job_models.dart';

class JobStatusLogic {
  const JobStatusLogic._();

  static const _transitions = <(JobStatus, JobStatus)>{
    (JobStatus.onGoing, JobStatus.onHold),
    (JobStatus.onHold, JobStatus.onGoing),
    (JobStatus.onGoing, JobStatus.cancelled),
    (JobStatus.onHold, JobStatus.cancelled),
    (JobStatus.onGoing, JobStatus.completed),
    (JobStatus.onHold, JobStatus.completed),
  };

  static List<JobStatus> allowedNextStatuses(JobStatus current) {
    return _transitions
        .where((t) => t.$1 == current)
        .map((t) => t.$2)
        .toList();
  }

  static bool canModifyPurchases(JobStatus status) =>
      status == JobStatus.onGoing || status == JobStatus.onHold;

  static bool canModifyJobDetails(JobStatus status) => canModifyPurchases(status);

  static bool isTerminalForCr(JobStatus status) =>
      status == JobStatus.cancelled ||
      status == JobStatus.completed ||
      status == JobStatus.closed;

  static String actionLabel(JobStatus target) {
    switch (target) {
      case JobStatus.onHold:
        return 'Put on hold';
      case JobStatus.onGoing:
        return 'Resume';
      case JobStatus.cancelled:
        return 'Cancel job';
      case JobStatus.completed:
        return 'Mark completed';
      case JobStatus.closed:
        return 'Close';
    }
  }
}
