import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../core/api_client.dart';
import '../../core/auth_session.dart';
import '../../widgets/inline_error.dart';

class TakeJobScreen extends StatefulWidget {
  const TakeJobScreen({
    super.key,
    required this.session,
    required this.vehicle,
  });

  final AuthSession session;
  final Map<String, dynamic> vehicle;

  @override
  State<TakeJobScreen> createState() => _TakeJobScreenState();
}

class _TakeJobScreenState extends State<TakeJobScreen> {
  final _odoController = TextEditingController();
  final _notesController = TextEditingController();
  bool _loading = false;
  String? _error;
  Map<String, dynamic>? _resolvedVehicle;

  late final ApiClient _api = ApiClient(widget.session);

  @override
  void initState() {
    super.initState();
    _resolveVehicle();
  }

  Future<void> _resolveVehicle() async {
    setState(() => _loading = true);
    try {
      final token = widget.vehicle['qrToken'] as String?;
      final vehicleId = widget.vehicle['vehicleId'];
      if (token != null) {
        final v = await _api.getVehicleByQr(token);
        setState(() => _resolvedVehicle = v);
      } else if (vehicleId != null) {
        setState(() => _resolvedVehicle = widget.vehicle);
      } else {
        setState(() => _resolvedVehicle = widget.vehicle);
      }
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _takeJob() async {
    final v = _resolvedVehicle;
    if (v == null) return;

    int? odo;
    final odoText = _odoController.text.trim();
    if (odoText.isNotEmpty) odo = int.tryParse(odoText);

    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final created = await _api.takeJob(
        vehicleId: v['vehicleId'] as String,
        odometer: odo,
        notes: _notesController.text.trim().isEmpty ? null : _notesController.text.trim(),
      );
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Job created — status On Going')),
      );
      Navigator.of(context).pop(created['id'] as String?);
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _odoController.dispose();
    _notesController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final v = _resolvedVehicle;
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    return Scaffold(
      appBar: AppBar(title: const Text('Take job')),
      body: _loading && v == null
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  if (v != null) ...[
                    Card(
                      child: Padding(
                        padding: const EdgeInsets.symmetric(vertical: 8),
                        child: Column(
                          children: [
                            ListTile(
                              leading: CircleAvatar(
                                backgroundColor: scheme.primaryContainer,
                                child: Icon(
                                  Icons.directions_car,
                                  color: scheme.onPrimaryContainer,
                                ),
                              ),
                              title: Text(
                                v['plateNumber']?.toString() ?? '',
                                style: theme.textTheme.titleLarge,
                              ),
                              subtitle: const Text('Registered vehicle'),
                            ),
                            const Divider(height: 1, indent: 16, endIndent: 16),
                            ListTile(
                              leading: const Icon(Icons.person_outline),
                              title: Text(v['customerName']?.toString() ?? ''),
                            ),
                            ListTile(
                              leading: const Icon(Icons.phone_outlined),
                              title: Text(v['customerPhone']?.toString() ?? ''),
                            ),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 20),
                  ],
                  Text(
                    'Job details',
                    style: theme.textTheme.titleSmall?.copyWith(
                      color: scheme.onSurfaceVariant,
                    ),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: _odoController,
                    keyboardType: TextInputType.number,
                    inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                    decoration: const InputDecoration(
                      labelText: 'Odometer (optional)',
                      prefixIcon: Icon(Icons.speed_outlined),
                      helperText: 'Current mileage reading',
                    ),
                  ),
                  const SizedBox(height: 16),
                  TextField(
                    controller: _notesController,
                    maxLines: 3,
                    decoration: const InputDecoration(
                      labelText: 'Notes',
                      prefixIcon: Icon(Icons.notes_outlined),
                      alignLabelWithHint: true,
                    ),
                  ),
                  if (_error != null) ...[
                    const SizedBox(height: 16),
                    InlineError(message: _error!),
                  ],
                  const SizedBox(height: 24),
                  FilledButton.icon(
                    onPressed: _loading ? null : _takeJob,
                    icon: _loading
                        ? SizedBox(
                            height: 18,
                            width: 18,
                            child: CircularProgressIndicator(
                              strokeWidth: 2,
                              color: scheme.onPrimary,
                            ),
                          )
                        : const Icon(Icons.play_arrow_rounded),
                    label: const Text('Start job'),
                  ),
                ],
              ),
            ),
    );
  }
}
