import 'package:flutter/material.dart';

import '../models/job_models.dart';

class JobStatusChip extends StatelessWidget {
  const JobStatusChip({super.key, required this.status});

  final JobStatus status;

  static String labelOf(JobStatus status) {
    switch (status) {
      case JobStatus.onGoing:
        return 'On going';
      case JobStatus.onHold:
        return 'On hold';
      case JobStatus.cancelled:
        return 'Cancelled';
      case JobStatus.completed:
        return 'Completed';
      case JobStatus.closed:
        return 'Closed';
    }
  }

  static String labelOfDynamic(dynamic value) => labelOf(JobStatus.parse(value));

  JobStatus get _parsed => status;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final (bg, fg) = _colors(scheme, _parsed);
    return Chip(
      label: Text(labelOf(_parsed)),
      labelStyle: TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: fg),
      backgroundColor: bg,
      side: BorderSide.none,
      visualDensity: VisualDensity.compact,
      materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
      padding: const EdgeInsets.symmetric(horizontal: 4),
    );
  }

  (Color, Color) _colors(ColorScheme scheme, JobStatus raw) {
    switch (raw) {
      case JobStatus.onGoing:
        return (scheme.primaryContainer, scheme.onPrimaryContainer);
      case JobStatus.onHold:
        return (scheme.tertiaryContainer, scheme.onTertiaryContainer);
      case JobStatus.cancelled:
        return (scheme.errorContainer, scheme.onErrorContainer);
      case JobStatus.completed:
        return (scheme.secondaryContainer, scheme.onSecondaryContainer);
      case JobStatus.closed:
        return (scheme.surfaceContainerHighest, scheme.onSurfaceVariant);
    }
  }
}
