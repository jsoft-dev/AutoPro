import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../core/api_client.dart';
import '../../core/auth_session.dart';
import '../../widgets/inline_error.dart';
import '../registration/register_vehicle_screen.dart';

class FindVehicleByPlateScreen extends StatefulWidget {
  const FindVehicleByPlateScreen({super.key, required this.session});

  final AuthSession session;

  @override
  State<FindVehicleByPlateScreen> createState() => _FindVehicleByPlateScreenState();
}

class _FindVehicleByPlateScreenState extends State<FindVehicleByPlateScreen> {
  final _plateController = TextEditingController();
  late final ApiClient _api = ApiClient(widget.session);
  bool _loading = false;
  String? _error;
  bool _notFound = false;
  Map<String, dynamic>? _vehicle;

  @override
  void dispose() {
    _plateController.dispose();
    super.dispose();
  }

  Future<void> _search() async {
    final plate = _plateController.text.trim();
    if (plate.isEmpty) {
      setState(() => _error = 'Enter the vehicle registration number.');
      return;
    }

    setState(() {
      _loading = true;
      _error = null;
      _notFound = false;
      _vehicle = null;
    });
    try {
      final v = await _api.getVehicleByPlate(plate);
      setState(() => _vehicle = v);
    } catch (e) {
      final notFound = e is ApiException && e.statusCode == 404;
      setState(() {
        _notFound = notFound;
        _error = notFound ? 'No vehicle with plate “$plate” is registered yet.' : e.toString();
      });
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  void _continueWithVehicle() {
    final v = _vehicle;
    if (v == null) return;
    Navigator.of(context).pop(v);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final v = _vehicle;

    return Scaffold(
      appBar: AppBar(title: const Text('Find by plate')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'Use when the vehicle QR is missing or damaged.',
              style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant),
            ),
            const SizedBox(height: 20),
            TextField(
              controller: _plateController,
              textCapitalization: TextCapitalization.characters,
              inputFormatters: [
                FilteringTextInputFormatter.allow(RegExp(r'[A-Za-z0-9\- ]')),
              ],
              decoration: const InputDecoration(
                labelText: 'Registration number',
                hintText: 'e.g. CAB-1234',
                prefixIcon: Icon(Icons.pin_outlined),
              ),
              textInputAction: TextInputAction.search,
              onSubmitted: (_) => _search(),
            ),
            const SizedBox(height: 12),
            FilledButton.icon(
              onPressed: _loading ? null : _search,
              icon: _loading
                  ? const SizedBox(
                      height: 18,
                      width: 18,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.search),
              label: const Text('Look up vehicle'),
            ),
            if (_error != null) ...[
              const SizedBox(height: 16),
              InlineError(message: _error!),
              if (_notFound) ...[
                const SizedBox(height: 12),
                FilledButton.tonalIcon(
                  onPressed: () async {
                    final nav = Navigator.of(context);
                    final v = await nav.push<Map<String, dynamic>>(
                      MaterialPageRoute(
                        builder: (_) => RegisterVehicleScreen(
                          session: widget.session,
                          initialPlate: _plateController.text.trim(),
                        ),
                      ),
                    );
                    if (v != null && context.mounted) {
                      nav.pop(v);
                    }
                  },
                  icon: const Icon(Icons.person_add_alt_1_outlined),
                  label: const Text('Register this vehicle'),
                ),
              ],
            ],
            if (v != null) ...[
              const SizedBox(height: 24),
              Card(
                child: Padding(
                  padding: const EdgeInsets.symmetric(vertical: 8),
                  child: Column(
                    children: [
                      ListTile(
                        leading: CircleAvatar(
                          backgroundColor: scheme.primaryContainer,
                          child: Icon(Icons.directions_car, color: scheme.onPrimaryContainer),
                        ),
                        title: Text(
                          v['plateNumber']?.toString() ?? '',
                          style: theme.textTheme.titleLarge,
                        ),
                        subtitle: Text(_makeModel(v)),
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
              const SizedBox(height: 16),
              FilledButton(
                onPressed: _continueWithVehicle,
                child: const Text('Continue to take job'),
              ),
            ],
          ],
        ),
      ),
    );
  }

  String _makeModel(Map<String, dynamic> v) {
    final parts = <String>[
      if (v['make'] != null) v['make'].toString(),
      if (v['model'] != null) v['model'].toString(),
      if (v['year'] != null) v['year'].toString(),
    ];
    if (parts.isEmpty) return 'Registered vehicle';
    return parts.join(' ');
  }
}
