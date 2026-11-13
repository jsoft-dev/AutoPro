import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../../core/api_client.dart';
import '../../../models/job_models.dart';

Future<JobDetail?> showEditJobDetailsSheet({
  required BuildContext context,
  required ApiClient api,
  required JobDetail job,
}) {
  return showModalBottomSheet<JobDetail>(
    context: context,
    isScrollControlled: true,
    useSafeArea: true,
    builder: (ctx) => _EditJobDetailsSheet(api: api, job: job),
  );
}

class _EditJobDetailsSheet extends StatefulWidget {
  const _EditJobDetailsSheet({required this.api, required this.job});

  final ApiClient api;
  final JobDetail job;

  @override
  State<_EditJobDetailsSheet> createState() => _EditJobDetailsSheetState();
}

class _EditJobDetailsSheetState extends State<_EditJobDetailsSheet> {
  final _formKey = GlobalKey<FormState>();
  late ServiceJobType _jobType;
  late final TextEditingController _otherTypeController;
  late final TextEditingController _otherChargeController;
  late final TextEditingController _odoController;
  bool _saving = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _jobType = widget.job.jobType;
    _otherTypeController = TextEditingController(
      text: widget.job.jobTypeOtherDescription ?? '',
    );
    _otherChargeController = TextEditingController(
      text: widget.job.customServiceChargeLkr?.toString() ??
          (widget.job.jobType == ServiceJobType.other
              ? widget.job.jobTypeServiceChargeLkr.toString()
              : ''),
    );
    _odoController = TextEditingController(
      text: widget.job.odometerReading?.toString() ?? '',
    );
  }

  @override
  void dispose() {
    _otherTypeController.dispose();
    _otherChargeController.dispose();
    _odoController.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;

    final odoText = _odoController.text.trim();
    final odometer = odoText.isEmpty ? null : int.tryParse(odoText);

    setState(() {
      _saving = true;
      _error = null;
    });

    double? customCharge;
    if (_jobType == ServiceJobType.other) {
      final chargeText = _otherChargeController.text.trim();
      if (chargeText.isEmpty) {
        setState(() => _error = 'Enter a service charge for this custom job type.');
        return;
      }
      customCharge = double.tryParse(chargeText);
      if (customCharge == null || customCharge < 0) {
        setState(() => _error = 'Enter a valid service charge.');
        return;
      }
    }

    try {
      final json = await widget.api.updateJobDetails(
        widget.job.id,
        jobType: _jobType.apiValue,
        jobTypeOtherDescription: _jobType == ServiceJobType.other
            ? _otherTypeController.text.trim()
            : null,
        customServiceChargeLkr: customCharge,
        odometerReading: odometer,
      );
      if (!mounted) return;
      Navigator.pop(context, JobDetail.fromJson(json));
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _saving = false;
        _error = userFacingError(e);
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final bottom = MediaQuery.viewInsetsOf(context).bottom;

    return Padding(
      padding: EdgeInsets.fromLTRB(20, 16, 20, 16 + bottom),
      child: Form(
        key: _formKey,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'Edit job details',
              style: Theme.of(context).textTheme.titleLarge,
            ),
            const SizedBox(height: 4),
            Text(
              'Changes apply to billing when you complete the job.',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: Theme.of(context).colorScheme.onSurfaceVariant,
                  ),
            ),
            const SizedBox(height: 20),
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
              onChanged: _saving
                  ? null
                  : (value) {
                      if (value == null) return;
                      setState(() {
                        _jobType = value;
                        if (value != ServiceJobType.other) {
                          _otherTypeController.clear();
                          _otherChargeController.clear();
                        }
                      });
                    },
            ),
            if (_jobType == ServiceJobType.other) ...[
              const SizedBox(height: 16),
              TextFormField(
                controller: _otherTypeController,
                enabled: !_saving,
                textCapitalization: TextCapitalization.sentences,
                decoration: const InputDecoration(
                  labelText: 'Describe job type',
                  prefixIcon: Icon(Icons.edit_outlined),
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
                controller: _otherChargeController,
                enabled: !_saving,
                keyboardType: const TextInputType.numberWithOptions(decimal: true),
                decoration: const InputDecoration(
                  labelText: 'Service charge (LKR)',
                  prefixIcon: Icon(Icons.payments_outlined),
                ),
                validator: (value) {
                  if (_jobType != ServiceJobType.other) return null;
                  if (value == null || value.trim().isEmpty) {
                    return 'Enter the service charge';
                  }
                  final parsed = double.tryParse(value.trim());
                  if (parsed == null || parsed < 0) return 'Enter a valid amount';
                  return null;
                },
              ),
            ],
            const SizedBox(height: 16),
            TextFormField(
              controller: _odoController,
              enabled: !_saving,
              keyboardType: TextInputType.number,
              inputFormatters: [FilteringTextInputFormatter.digitsOnly],
              decoration: InputDecoration(
                labelText: 'Odometer (km)',
                prefixIcon: const Icon(Icons.speed_outlined),
                helperText: widget.job.vehicleLastRecordedOdometerKm != null
                    ? 'Last recorded: ${widget.job.vehicleLastRecordedOdometerKm} km — cannot enter less'
                    : 'Leave blank if not recorded',
              ),
              validator: (value) {
                final text = value?.trim() ?? '';
                if (text.isEmpty) return null;
                final parsed = int.tryParse(text);
                if (parsed == null) return 'Enter a valid number';
                return validateOdometerAgainstLast(
                  parsed,
                  widget.job.vehicleLastRecordedOdometerKm,
                );
              },
            ),
            if (_error != null) ...[
              const SizedBox(height: 12),
              Text(
                _error!,
                style: TextStyle(color: Theme.of(context).colorScheme.error),
              ),
            ],
            const SizedBox(height: 20),
            FilledButton(
              onPressed: _saving ? null : _save,
              child: Text(_saving ? 'Saving…' : 'Save changes'),
            ),
            TextButton(
              onPressed: _saving ? null : () => Navigator.pop(context),
              child: const Text('Cancel'),
            ),
          ],
        ),
      ),
    );
  }
}
