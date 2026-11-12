import 'dart:async';

import 'package:flutter/material.dart';

import '../../core/api_client.dart';
import '../../core/auth_session.dart';
import '../../core/job_status_logic.dart';
import '../../models/job_models.dart';
import '../../widgets/inline_error.dart';
import '../../widgets/job_status_chip.dart';
import 'widgets/add_purchase_sheet.dart';

class JobDetailScreen extends StatefulWidget {
  const JobDetailScreen({
    super.key,
    required this.session,
    required this.jobId,
  });

  final AuthSession session;
  final String jobId;

  @override
  State<JobDetailScreen> createState() => _JobDetailScreenState();
}

class _JobDetailScreenState extends State<JobDetailScreen> {
  late final ApiClient _api = ApiClient(widget.session);
  JobDetail? _job;
  bool _loading = true;
  String? _error;
  bool _statusBusy = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _syncJobFromServer() async {
    try {
      final json = await _api.getJob(widget.jobId);
      if (!mounted) return;
      setState(() => _job = JobDetail.fromJson(json));
    } catch (_) {
      // Keep last local state; user already saw success snackbar.
    }
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final json = await _api.getJob(widget.jobId);
      setState(() => _job = JobDetail.fromJson(json));
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _changeStatus(JobStatus target) async {
    final job = _job;
    if (job == null) return;

    final needsConfirm =
        target == JobStatus.cancelled || target == JobStatus.completed;
    String? note;

    if (needsConfirm) {
      final confirmed = await showDialog<bool>(
        context: context,
        builder: (ctx) {
          final noteCtrl = TextEditingController();
          return AlertDialog(
            title: Text(
              target == JobStatus.cancelled
                  ? 'Cancel this job?'
                  : 'Complete this job?',
            ),
            content: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  target == JobStatus.cancelled
                      ? 'This job will be marked cancelled. The WebApp can still see it in records.'
                      : 'Work is finished. Payment and closing happen later on the WebApp.',
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: noteCtrl,
                  decoration: const InputDecoration(
                    labelText: 'Note (optional)',
                    border: OutlineInputBorder(),
                  ),
                  maxLines: 2,
                ),
              ],
            ),
            actions: [
              TextButton(
                  onPressed: () => Navigator.pop(ctx, false),
                  child: const Text('Back')),
              FilledButton(
                onPressed: () {
                  note = noteCtrl.text.trim().isEmpty
                      ? null
                      : noteCtrl.text.trim();
                  Navigator.pop(ctx, true);
                },
                child: Text(
                    target == JobStatus.cancelled ? 'Cancel job' : 'Complete'),
              ),
            ],
          );
        },
      );
      if (confirmed != true) return;
    }

    setState(() => _statusBusy = true);
    try {
      final json = await _api.updateJobStatus(
        widget.jobId,
        status: target.apiValue,
        note: note,
      );
      if (!mounted) return;
      var detail = JobDetail.fromJson(json);
      if (detail.status != target) {
        detail = detail.copyWith(status: target);
      }
      setState(() => _job = detail);
      unawaited(_syncJobFromServer());
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
            content:
                Text('Status updated to ${JobStatusChip.labelOf(target)}')),
      );
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(e.toString())),
        );
      }
    } finally {
      if (mounted) setState(() => _statusBusy = false);
    }
  }

  Future<void> _openAddPurchase() async {
    final job = _job;
    if (job == null || !JobStatusLogic.canModifyPurchases(job.status)) return;

    final added = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      showDragHandle: false,
      builder: (ctx) => AddPurchaseSheet(
        onSearchInventory: (q) => _api.searchInventory(q),
        onSubmit: ({
          required source,
          required description,
          required quantity,
          required unitPrice,
          vendorName,
          inventoryItemId,
        }) async {
          final json = await _api.addJobPurchase(
            widget.jobId,
            source: source.apiValue,
            inventoryItemId: inventoryItemId,
            description: description,
            quantity: quantity,
            unitPrice: unitPrice,
            vendorName: vendorName,
          );
          setState(() => _job = JobDetail.fromJson(json));
        },
      ),
    );

    if (added == true && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Purchase added')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final job = _job;

    return Scaffold(
      body: _loading && job == null
          ? const Center(child: CircularProgressIndicator())
          : _error != null && job == null
              ? Center(
                  child: Padding(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        InlineError(message: _error!),
                        const SizedBox(height: 16),
                        FilledButton(
                            onPressed: _load, child: const Text('Retry')),
                      ],
                    ),
                  ),
                )
              : RefreshIndicator(
                  onRefresh: _load,
                  child: CustomScrollView(
                    slivers: [
                      SliverAppBar.large(
                        key: ValueKey('appbar-${job!.status.name}'),
                        title: Text(job.jobNumber),
                        actions: [
                          Center(
                            child: Padding(
                              padding: const EdgeInsets.only(right: 12),
                              child: JobStatusChip(
                                key: ValueKey('status-${job.status.name}'),
                                status: job.status,
                              ),
                            ),
                          ),
                        ],
                      ),
                      SliverToBoxAdapter(
                        child: Padding(
                          padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.stretch,
                            children: [
                              _VehicleCard(job: job),
                              if (job.notes != null &&
                                  job.notes!.isNotEmpty) ...[
                                const SizedBox(height: 16),
                                const _SectionTitle(title: 'Notes'),
                                const SizedBox(height: 8),
                                Card(
                                  child: Padding(
                                    padding: const EdgeInsets.all(16),
                                    child: Text(job.notes!),
                                  ),
                                ),
                              ],
                              const SizedBox(height: 20),
                              const _SectionTitle(title: 'Status'),
                              const SizedBox(height: 8),
                              _StatusActions(
                                current: job.status,
                                busy: _statusBusy,
                                onTarget: _changeStatus,
                              ),
                              const SizedBox(height: 24),
                              Row(
                                children: [
                                  const Expanded(
                                      child: const _SectionTitle(
                                          title: 'Purchases')),
                                  Text(
                                    'Total Rs ${job.purchasesTotal.toStringAsFixed(2)}',
                                    style: theme.textTheme.titleSmall?.copyWith(
                                      color: scheme.primary,
                                      fontWeight: FontWeight.w600,
                                    ),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 8),
                              if (job.purchases.isEmpty)
                                Card(
                                  child: Padding(
                                    padding: const EdgeInsets.all(20),
                                    child: Text(
                                      'No purchases yet. Add parts from the shop or external vendors.',
                                      style:
                                          theme.textTheme.bodyMedium?.copyWith(
                                        color: scheme.onSurfaceVariant,
                                      ),
                                    ),
                                  ),
                                )
                              else
                                ...job.purchases
                                    .map((p) => _PurchaseTile(line: p)),
                              const SizedBox(height: 80),
                            ],
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
      floatingActionButton:
          job != null && JobStatusLogic.canModifyPurchases(job.status)
              ? FloatingActionButton.extended(
                  onPressed: _openAddPurchase,
                  icon: const Icon(Icons.add_shopping_cart),
                  label: const Text('Add purchase'),
                )
              : null,
    );
  }
}

class _SectionTitle extends StatelessWidget {
  const _SectionTitle({required this.title});

  final String title;

  @override
  Widget build(BuildContext context) {
    return Text(
      title,
      style: Theme.of(context).textTheme.titleSmall?.copyWith(
            fontWeight: FontWeight.w600,
          ),
    );
  }
}

class _VehicleCard extends StatelessWidget {
  const _VehicleCard({required this.job});

  final JobDetail job;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    return Card(
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 8),
        child: Column(
          children: [
            ListTile(
              leading: CircleAvatar(
                backgroundColor: scheme.primaryContainer,
                child: Icon(Icons.directions_car,
                    color: scheme.onPrimaryContainer),
              ),
              title: Text(job.plateNumber, style: theme.textTheme.titleLarge),
              subtitle: const Text('Vehicle'),
            ),
            const Divider(height: 1, indent: 16, endIndent: 16),
            ListTile(
              leading: const Icon(Icons.person_outline),
              title: Text(job.customerName),
            ),
            if (job.odometerReading != null)
              ListTile(
                leading: const Icon(Icons.speed_outlined),
                title: Text('${job.odometerReading} km'),
                subtitle: const Text('Odometer at check-in'),
              ),
            ListTile(
              leading: const Icon(Icons.handyman_outlined),
              title: Text(job.jobTypeLabel),
              subtitle: const Text('Job type'),
            ),
          ],
        ),
      ),
    );
  }
}

class _StatusActions extends StatelessWidget {
  const _StatusActions({
    required this.current,
    required this.busy,
    required this.onTarget,
  });

  final JobStatus current;
  final bool busy;
  final ValueChanged<JobStatus> onTarget;

  static int _actionOrder(JobStatus target) {
    return switch (target) {
      JobStatus.completed => 0,
      JobStatus.onHold || JobStatus.onGoing => 1,
      JobStatus.cancelled => 2,
      JobStatus.closed => 3,
    };
  }

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final targets = [...JobStatusLogic.allowedNextStatuses(current)]
      ..sort((a, b) => _actionOrder(a).compareTo(_actionOrder(b)));

    if (targets.isEmpty) {
      return Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Text(
            'No further status changes available for you on this job.',
            style: Theme.of(context).textTheme.bodyMedium,
          ),
        ),
      );
    }

    return Card(
      clipBehavior: Clip.antiAlias,
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            for (var i = 0; i < targets.length; i++) ...[
              if (i > 0) const SizedBox(height: 10),
              _StatusActionButton(
                target: targets[i],
                busy: busy,
                onPressed: () => onTarget(targets[i]),
              ),
            ],
            if (busy) ...[
              const SizedBox(height: 12),
              LinearProgressIndicator(
                minHeight: 2,
                borderRadius: BorderRadius.circular(1),
                color: scheme.primary,
                backgroundColor: scheme.surfaceContainerHighest,
              ),
            ],
          ],
        ),
      ),
    );
  }
}

enum _StatusActionVariant { primary, neutral, destructive }

class _StatusActionButton extends StatelessWidget {
  const _StatusActionButton({
    required this.target,
    required this.busy,
    required this.onPressed,
  });

  final JobStatus target;
  final bool busy;
  final VoidCallback onPressed;

  _StatusActionVariant get _variant => switch (target) {
        JobStatus.completed => _StatusActionVariant.primary,
        JobStatus.cancelled => _StatusActionVariant.destructive,
        _ => _StatusActionVariant.neutral,
      };

  IconData get _icon => switch (target) {
        JobStatus.onHold => Icons.pause_circle_filled,
        JobStatus.onGoing => Icons.play_circle_filled,
        JobStatus.cancelled => Icons.cancel_outlined,
        JobStatus.completed => Icons.task_alt_rounded,
        JobStatus.closed => Icons.lock_outline,
      };

  static const _radius = BorderRadius.all(Radius.circular(12));
  static const _height = 52.0;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final label = JobStatusLogic.actionLabel(target);
    final icon = Icon(_icon, size: 22);

    const shape = RoundedRectangleBorder(borderRadius: _radius);
    const minSize = Size(0, _height);
    const padding = EdgeInsets.symmetric(horizontal: 20, vertical: 12);
    final textStyle = theme.textTheme.titleSmall?.copyWith(
      fontWeight: FontWeight.w600,
      letterSpacing: 0.1,
    );

    return switch (_variant) {
      _StatusActionVariant.primary => FilledButton.icon(
          onPressed: busy ? null : onPressed,
          icon: icon,
          label: Text(label),
          style: FilledButton.styleFrom(
            minimumSize: minSize,
            padding: padding,
            shape: shape,
            textStyle: textStyle,
            elevation: 0,
          ),
        ),
      _StatusActionVariant.neutral => FilledButton.icon(
          onPressed: busy ? null : onPressed,
          icon: icon,
          label: Text(label),
          style: FilledButton.styleFrom(
            minimumSize: minSize,
            padding: padding,
            shape: shape,
            textStyle: textStyle,
            elevation: 0,
            backgroundColor: scheme.secondaryContainer,
            foregroundColor: scheme.onSecondaryContainer,
          ),
        ),
      _StatusActionVariant.destructive => OutlinedButton.icon(
          onPressed: busy ? null : onPressed,
          icon: icon,
          label: Text(label),
          style: OutlinedButton.styleFrom(
            minimumSize: minSize,
            padding: padding,
            shape: shape,
            textStyle: textStyle,
            foregroundColor: scheme.error,
            side: BorderSide(color: scheme.error.withValues(alpha: 0.35)),
          ),
        ),
    };
  }
}

class _PurchaseTile extends StatelessWidget {
  const _PurchaseTile({required this.line});

  final JobPurchaseLine line;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final isShop = line.source == PurchaseSource.inventory;

    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        leading: CircleAvatar(
          backgroundColor:
              isShop ? scheme.secondaryContainer : scheme.tertiaryContainer,
          child: Icon(
            isShop ? Icons.inventory_2_outlined : Icons.storefront_outlined,
            color: isShop
                ? scheme.onSecondaryContainer
                : scheme.onTertiaryContainer,
          ),
        ),
        title: Text(line.description),
        subtitle: Text(
          isShop
              ? [
                  if (line.inventoryPartNumber != null) line.inventoryPartNumber,
                  'Shop inventory',
                  'qty ${line.quantity}',
                ].join(' · ')
              : '${line.vendorName ?? 'Vendor'} · qty ${line.quantity}',
        ),
        trailing: Text(
          'Rs ${line.lineTotal.toStringAsFixed(2)}',
          style:
              theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w600),
        ),
      ),
    );
  }
}
