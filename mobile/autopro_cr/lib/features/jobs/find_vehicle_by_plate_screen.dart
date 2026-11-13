import 'package:flutter/material.dart';

import '../../core/api_client.dart';
import '../../core/auth_session.dart';
import '../../core/plate_number.dart';
import '../../widgets/inline_error.dart';
import '../../widgets/plate_number_input.dart';
import '../registration/register_vehicle_screen.dart';

class FindVehicleByPlateScreen extends StatefulWidget {
  const FindVehicleByPlateScreen({super.key, required this.session});

  final AuthSession session;

  @override
  State<FindVehicleByPlateScreen> createState() => _FindVehicleByPlateScreenState();
}

class _FindVehicleByPlateScreenState extends State<FindVehicleByPlateScreen> {
  final _platePrefixController = TextEditingController();
  final _plateSuffixController = TextEditingController();
  late final ApiClient _api = ApiClient(widget.session);
  bool _loading = false;
  String? _error;
  bool _notFound = false;
  Map<String, dynamic>? _vehicle;

  @override
  void dispose() {
    _platePrefixController.dispose();
    _plateSuffixController.dispose();
    super.dispose();
  }

  String get _formattedPlate => PlateNumberParts(
        prefix: _platePrefixController.text.trim(),
        suffix: _plateSuffixController.text.trim(),
      ).formatted;

  Future<void> _search() async {
    final message = PlateNumberParts.validationMessage(
      _platePrefixController.text,
      _plateSuffixController.text,
    );
    if (message != null) {
      setState(() => _error = message);
      return;
    }

    setState(() {
      _loading = true;
      _error = null;
      _notFound = false;
      _vehicle = null;
    });
    try {
      final v = await _api.getVehicleByPlateParts(
        platePrefix: _platePrefixController.text.trim(),
        plateSuffix: _plateSuffixController.text.trim(),
      );
      setState(() => _vehicle = v);
    } catch (e) {
      final notFound = e is ApiException && e.statusCode == 404;
      setState(() {
        _notFound = notFound;
        _error = notFound
            ? 'No vehicle with plate “$_formattedPlate” is registered yet.'
            : userFacingError(e);
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
            PlateNumberInput(
              prefixController: _platePrefixController,
              suffixController: _plateSuffixController,
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
                    final result = await nav.push<Map<String, dynamic>>(
                      MaterialPageRoute(
                        builder: (_) => RegisterVehicleScreen(
                          session: widget.session,
                          initialPlate: _formattedPlate,
                        ),
                      ),
                    );
                    if (result != null && context.mounted) {
                      nav.pop(result);
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
                          v['plateNumber']?.toString() ?? _formattedPlate,
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
