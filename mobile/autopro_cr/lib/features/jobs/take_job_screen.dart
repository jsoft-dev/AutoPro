import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../core/api_client.dart';
import '../../core/auth_session.dart';
import '../../models/job_models.dart';
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
  final _formKey = GlobalKey<FormState>();
  final _odoController = TextEditingController();
  final _notesController = TextEditingController();
  final _otherJobTypeController = TextEditingController();
  final _otherServiceChargeController = TextEditingController();
  bool _loading = false;
  String? _error;
  Map<String, dynamic>? _resolvedVehicle;
  int? _lastRecordedOdometerKm;
  ServiceJobType _jobType = ServiceJobType.wash;

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
        setState(() {
          _resolvedVehicle = v;
          _lastRecordedOdometerKm = v['lastRecordedOdometerKm'] as int?;
        });
      } else if (vehicleId != null) {
        setState(() {
          _resolvedVehicle = widget.vehicle;
          _lastRecordedOdometerKm = widget.vehicle['lastRecordedOdometerKm'] as int?;
        });
      } else {
        setState(() {
          _resolvedVehicle = widget.vehicle;
          _lastRecordedOdometerKm = widget.vehicle['lastRecordedOdometerKm'] as int?;
        });
      }
    } catch (e) {
      setState(() => _error = userFacingError(e));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _takeJob() async {
    final v = _resolvedVehicle;
    if (v == null) return;
    if (!_formKey.currentState!.validate()) return;

    int? odo;
    final odoText = _odoController.text.trim();
    if (odoText.isNotEmpty) odo = int.tryParse(odoText);
    final otherText = _otherJobTypeController.text.trim();
    final jobTypeOther =
        _jobType == ServiceJobType.other ? otherText : null;

    double? customCharge;
    if (_jobType == ServiceJobType.other) {
      final chargeText = _otherServiceChargeController.text.trim();
      if (chargeText.isEmpty) {
        setState(() => _error = 'Enter a service charge for this custom job type.');
        return;
      }
      customCharge = double.tryParse(chargeText);
      if (customCharge == null || customCharge < 0) {
        setState(() => _error = 'Enter a valid service charge (0 or more).');
        return;
      }
    }

    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final created = await _api.takeJob(
        vehicleId: v['vehicleId'] as String,
        jobType: _jobType.apiValue,
        jobTypeOtherDescription: jobTypeOther,
        customServiceChargeLkr: customCharge,
        odometer: odo,
        notes: _notesController.text.trim().isEmpty ? null : _notesController.text.trim(),
      );
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Job created — status On Going')),
      );
      Navigator.of(context).pop(created['id'] as String?);
    } catch (e) {
      setState(() => _error = userFacingError(e));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  void dispose() {
    _odoController.dispose();
    _notesController.dispose();
    _otherJobTypeController.dispose();
    _otherServiceChargeController.dispose();
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
          : Form(
              key: _formKey,
              child: SingleChildScrollView(
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
                                    Icons.verified_outlined,
                                    color: scheme.onPrimaryContainer,
                                  ),
                                ),
                                title: Text(
                                  'Vehicle authenticated',
                                  style: theme.textTheme.titleMedium?.copyWith(
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                                subtitle: const Text(
                                  'Customer and vehicle matched from QR or registration number',
                                ),
                              ),
                              const Divider(height: 1, indent: 16, endIndent: 16),
                              ListTile(
                                leading: CircleAvatar(
                                  backgroundColor: scheme.secondaryContainer,
                                  child: Icon(
                                    Icons.directions_car,
                                    color: scheme.onSecondaryContainer,
                                  ),
                                ),
                                title: Text(
                                  v['plateNumber']?.toString() ?? '',
                                  style: theme.textTheme.titleLarge,
                                ),
                              ),
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
                    DropdownButtonFormField<ServiceJobType>(
                      value: _jobType,
                      decoration: const InputDecoration(
                        labelText: 'Job type',
                        prefixIcon: Icon(Icons.handyman_outlined),
                      ),
                      items: ServiceJobType.values
                          .map(
                            (t) => DropdownMenuItem(
                              value: t,
                              child: Text(t.label),
                            ),
                          )
                          .toList(),
                      onChanged: _loading
                          ? null
                          : (value) {
                              if (value == null) return;
                              setState(() {
                                _jobType = value;
                                if (value != ServiceJobType.other) {
                                  _otherJobTypeController.clear();
                                  _otherServiceChargeController.clear();
                                }
                              });
                            },
                    ),
                    if (_jobType == ServiceJobType.other) ...[
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _otherJobTypeController,
                        textCapitalization: TextCapitalization.sentences,
                        decoration: const InputDecoration(
                          labelText: 'Describe job type',
                          prefixIcon: Icon(Icons.edit_outlined),
                          helperText: 'Required when Other is selected',
                        ),
                        validator: (value) {
                          if (_jobType != ServiceJobType.other) return null;
                          if (value == null || value.trim().isEmpty) {
                            return 'Enter the job type';
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _otherServiceChargeController,
                        keyboardType: const TextInputType.numberWithOptions(
                          decimal: true,
                        ),
                        decoration: const InputDecoration(
                          labelText: 'Service charge (LKR)',
                          prefixIcon: Icon(Icons.payments_outlined),
                          helperText: 'Quoted price for this custom job',
                        ),
                        validator: (value) {
                          if (_jobType != ServiceJobType.other) return null;
                          if (value == null || value.trim().isEmpty) {
                            return 'Enter the service charge';
                          }
                          final parsed = double.tryParse(value.trim());
                          if (parsed == null || parsed < 0) {
                            return 'Enter a valid amount';
                          }
                          return null;
                        },
                      ),
                    ],
                    const SizedBox(height: 16),
                    TextFormField(
                      controller: _odoController,
                      keyboardType: TextInputType.number,
                      inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                      decoration: InputDecoration(
                        labelText: 'Odometer (optional)',
                        prefixIcon: const Icon(Icons.speed_outlined),
                        helperText: _lastRecordedOdometerKm != null
                            ? 'Last recorded: $_lastRecordedOdometerKm km — cannot enter less'
                            : 'Current mileage reading',
                      ),
                      validator: (value) {
                        final text = value?.trim() ?? '';
                        if (text.isEmpty) return null;
                        final parsed = int.tryParse(text);
                        if (parsed == null) return 'Enter a valid number';
                        return validateOdometerAgainstLast(
                          parsed,
                          _lastRecordedOdometerKm,
                        );
                      },
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
            ),
    );
  }
}
