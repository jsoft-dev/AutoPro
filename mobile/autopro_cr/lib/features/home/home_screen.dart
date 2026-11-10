import 'package:flutter/material.dart';

import '../../core/api_client.dart';
import '../../core/auth_session.dart';
import '../../models/job_models.dart';
import '../../widgets/inline_error.dart';
import '../auth/login_screen.dart';
import '../jobs/job_detail_screen.dart';
import '../jobs/widgets/job_card.dart';
import '../jobs/find_vehicle_by_plate_screen.dart';
import '../registration/register_vehicle_screen.dart';
import '../jobs/take_job_screen.dart';
import '../jobs/widgets/new_job_sheet.dart';
import '../scan/qr_scan_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key, required this.session});

  final AuthSession session;

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  late final ApiClient _api = ApiClient(widget.session);
  List<JobListItem> _jobs = [];
  bool _loading = true;
  String? _error;
  JobStatus? _filter;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final raw = await _api.getTodayJobs();
      final jobs = raw
          .map((e) => JobListItem.fromJson(e as Map<String, dynamic>))
          .toList();
      setState(() => _jobs = jobs);
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  List<JobListItem> get _visibleJobs {
    if (_filter == null) return _jobs;
    return _jobs.where((j) => j.status == _filter).toList();
  }

  int _countFor(JobStatus? status) {
    if (status == null) return _jobs.length;
    return _jobs.where((j) => j.status == status).length;
  }

  Future<void> _logout() async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Sign out?'),
        content: const Text('You will need to sign in again to access jobs.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancel'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Sign out'),
          ),
        ],
      ),
    );
    if (confirm != true) return;

    await widget.session.clear();
    if (!mounted) return;
    Navigator.of(context).pushReplacement(
      MaterialPageRoute(builder: (_) => LoginScreen(session: widget.session)),
    );
  }

  Future<void> _startNewJob() async {
    final method = await showModalBottomSheet<NewJobMethod>(
      context: context,
      showDragHandle: true,
      useSafeArea: true,
      builder: (_) => const NewJobSheet(),
    );
    if (method == null || !mounted) return;
    final navigator = Navigator.of(context);

    final Map<String, dynamic>? vehicle = switch (method) {
      NewJobMethod.scanQr => await navigator.push<Map<String, dynamic>>(
          MaterialPageRoute(builder: (_) => const QrScanScreen()),
        ),
      NewJobMethod.findByPlate => await navigator.push<Map<String, dynamic>>(
          MaterialPageRoute(
            builder: (_) => FindVehicleByPlateScreen(session: widget.session),
          ),
        ),
      NewJobMethod.registerVehicle => await navigator.push<Map<String, dynamic>>(
          MaterialPageRoute(
            builder: (_) => RegisterVehicleScreen(session: widget.session),
          ),
        ),
    };
    if (vehicle == null || !mounted) return;

    final result = await navigator.push<String?>(
      MaterialPageRoute(
        builder: (_) => TakeJobScreen(
          session: widget.session,
          vehicle: vehicle,
        ),
      ),
    );
    await _load();
    if (result != null && mounted) {
      await _openJob(result);
    }
  }

  Future<void> _openJob(String jobId) async {
    await Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => JobDetailScreen(session: widget.session, jobId: jobId),
      ),
    );
    await _load();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final visible = _visibleJobs;

    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Jobs', style: theme.textTheme.titleLarge),
            if (widget.session.displayName?.isNotEmpty == true)
              Text(
                widget.session.displayName!,
                style: theme.textTheme.labelMedium?.copyWith(
                  color: scheme.onSurfaceVariant,
                ),
              ),
          ],
        ),
        actions: [
          IconButton(
            onPressed: _loading ? null : _load,
            icon: const Icon(Icons.refresh),
            tooltip: 'Refresh',
          ),
          IconButton(
            onPressed: _logout,
            icon: const Icon(Icons.logout),
            tooltip: 'Sign out',
          ),
        ],
      ),
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          SizedBox(
            height: 48,
            child: ListView(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              children: [
                _FilterChip(
                  label: 'All (${_countFor(null)})',
                  selected: _filter == null,
                  onTap: () => setState(() => _filter = null),
                ),
                _FilterChip(
                  label: 'Active (${_countFor(JobStatus.onGoing)})',
                  selected: _filter == JobStatus.onGoing,
                  onTap: () => setState(() => _filter = JobStatus.onGoing),
                ),
                _FilterChip(
                  label: 'On hold (${_countFor(JobStatus.onHold)})',
                  selected: _filter == JobStatus.onHold,
                  onTap: () => setState(() => _filter = JobStatus.onHold),
                ),
                _FilterChip(
                  label: 'Done (${_countFor(JobStatus.completed)})',
                  selected: _filter == JobStatus.completed,
                  onTap: () => setState(() => _filter = JobStatus.completed),
                ),
              ],
            ),
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: _load,
              child: _buildList(theme, scheme, visible),
            ),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _startNewJob,
        icon: const Icon(Icons.add),
        label: const Text('New job'),
      ),
    );
  }

  Widget _buildList(ThemeData theme, ColorScheme scheme, List<JobListItem> visible) {
    if (_loading && _jobs.isEmpty) {
      return ListView(
        children: const [
          SizedBox(height: 120),
          Center(child: CircularProgressIndicator()),
        ],
      );
    }

    if (_error != null && _jobs.isEmpty) {
      return ListView(
        children: [
          const SizedBox(height: 24),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: InlineError(message: _error!),
          ),
        ],
      );
    }

    if (visible.isEmpty) {
      return ListView(
        children: [
          const SizedBox(height: 64),
          Icon(Icons.work_off_outlined, size: 56, color: scheme.outline),
          const SizedBox(height: 12),
          Text(
            _filter == null ? 'No jobs yet today' : 'No jobs in this filter',
            textAlign: TextAlign.center,
            style: theme.textTheme.titleMedium,
          ),
          const SizedBox(height: 8),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 32),
            child: Text(
              _filter == null
                  ? 'Tap New job — scan a QR or find the vehicle by plate.'
                  : 'Try another filter or start a new job.',
              textAlign: TextAlign.center,
              style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant),
            ),
          ),
        ],
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.fromLTRB(16, 4, 16, 88),
      itemCount: visible.length,
      separatorBuilder: (_, __) => const SizedBox(height: 10),
      itemBuilder: (context, i) {
        final job = visible[i];
        return JobCard(
          job: job,
          onTap: () => _openJob(job.id),
        );
      },
    );
  }
}

class _FilterChip extends StatelessWidget {
  const _FilterChip({
    required this.label,
    required this.selected,
    required this.onTap,
  });

  final String label;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(right: 8),
      child: FilterChip(
        label: Text(label),
        selected: selected,
        onSelected: (_) => onTap(),
        showCheckmark: false,
      ),
    );
  }
}
