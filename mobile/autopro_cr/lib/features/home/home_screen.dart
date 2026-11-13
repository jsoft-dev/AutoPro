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
  final ScrollController _scrollController = ScrollController();

  List<JobListItem> _jobs = [];
  bool _loading = true;
  bool _loadingMore = false;
  String? _error;
  JobStatus? _filter;
  int _page = 1;
  int _totalCount = 0;
  int _loadGeneration = 0;

  bool get _hasMore => _jobs.length < _totalCount;

  @override
  void initState() {
    super.initState();
    _scrollController.addListener(_onScroll);
    _loadFirstPage();
  }

  @override
  void dispose() {
    _scrollController.removeListener(_onScroll);
    _scrollController.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (!_scrollController.hasClients || _loading || _loadingMore || !_hasMore) {
      return;
    }
    final position = _scrollController.position;
    if (position.pixels >= position.maxScrollExtent - 120) {
      _loadNextPage();
    }
  }

  Future<void> _loadFirstPage() async {
    final generation = ++_loadGeneration;
    setState(() {
      _loading = true;
      _loadingMore = false;
      _error = null;
      _page = 1;
    });

    try {
      final bounds = _api.utcTodayBounds();
      final page = await _api.getJobs(
        from: bounds.$1,
        to: bounds.$2,
        status: _filter?.apiValue,
        page: 1,
      );
      if (!mounted || generation != _loadGeneration) return;
      setState(() {
        _jobs = page.items;
        _totalCount = page.totalCount;
        _page = page.page;
        _loading = false;
      });
    } catch (e) {
      if (!mounted || generation != _loadGeneration) return;
      setState(() {
        _error = userFacingError(e);
        _loading = false;
        _jobs = [];
        _totalCount = 0;
      });
    }
  }

  Future<void> _loadNextPage() async {
    if (_loading || _loadingMore || !_hasMore) return;

    final generation = _loadGeneration;
    final nextPage = _page + 1;

    setState(() => _loadingMore = true);

    try {
      final bounds = _api.utcTodayBounds();
      final page = await _api.getJobs(
        from: bounds.$1,
        to: bounds.$2,
        status: _filter?.apiValue,
        page: nextPage,
      );
      if (!mounted || generation != _loadGeneration) return;
      setState(() {
        _jobs = [..._jobs, ...page.items];
        _totalCount = page.totalCount;
        _page = page.page;
        _loadingMore = false;
      });
    } catch (e) {
      if (!mounted || generation != _loadGeneration) return;
      setState(() => _loadingMore = false);
    }
  }

  void _setFilter(JobStatus? status) {
    if (_filter == status) return;
    setState(() => _filter = status);
    _loadFirstPage();
  }

  String _chipLabel(String base, JobStatus? matches) {
    if (_filter == matches && !_loading) {
      return '$base ($_totalCount)';
    }
    return base;
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
    await _loadFirstPage();
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
    await _loadFirstPage();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

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
            onPressed: _loading ? null : _loadFirstPage,
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
                  label: _chipLabel('All', null),
                  selected: _filter == null,
                  onTap: () => _setFilter(null),
                ),
                _FilterChip(
                  label: _chipLabel('Active', JobStatus.onGoing),
                  selected: _filter == JobStatus.onGoing,
                  onTap: () => _setFilter(JobStatus.onGoing),
                ),
                _FilterChip(
                  label: _chipLabel('On hold', JobStatus.onHold),
                  selected: _filter == JobStatus.onHold,
                  onTap: () => _setFilter(JobStatus.onHold),
                ),
                _FilterChip(
                  label: _chipLabel('Done', JobStatus.completed),
                  selected: _filter == JobStatus.completed,
                  onTap: () => _setFilter(JobStatus.completed),
                ),
              ],
            ),
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: _loadFirstPage,
              child: _buildList(theme, scheme),
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

  Widget _buildList(ThemeData theme, ColorScheme scheme) {
    if (_loading && _jobs.isEmpty) {
      return ListView(
        controller: _scrollController,
        physics: const AlwaysScrollableScrollPhysics(),
        children: const [
          SizedBox(height: 120),
          Center(child: CircularProgressIndicator()),
        ],
      );
    }

    if (_error != null && _jobs.isEmpty) {
      return ListView(
        controller: _scrollController,
        physics: const AlwaysScrollableScrollPhysics(),
        children: [
          const SizedBox(height: 24),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: InlineError(message: _error!),
          ),
        ],
      );
    }

    if (_jobs.isEmpty) {
      return ListView(
        controller: _scrollController,
        physics: const AlwaysScrollableScrollPhysics(),
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

    final itemCount = _jobs.length + ((_hasMore || _loadingMore) ? 1 : 0);

    return ListView.separated(
      controller: _scrollController,
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(16, 4, 16, 88),
      itemCount: itemCount,
      separatorBuilder: (context, index) {
        if (index >= _jobs.length - 1) return const SizedBox.shrink();
        return const SizedBox(height: 10);
      },
      itemBuilder: (context, i) {
        if (i >= _jobs.length) {
          return const Padding(
            padding: EdgeInsets.symmetric(vertical: 16),
            child: Center(
              child: SizedBox(
                width: 24,
                height: 24,
                child: CircularProgressIndicator(strokeWidth: 2),
              ),
            ),
          );
        }

        final job = _jobs[i];
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
