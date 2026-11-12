import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../core/api_client.dart';
import '../../core/auth_session.dart';
import '../../core/plate_number.dart';
import '../../widgets/inline_error.dart';
import '../../widgets/plate_number_input.dart';
import 'widgets/vehicle_qr_display.dart';

class RegisterVehicleScreen extends StatefulWidget {
  const RegisterVehicleScreen({
    super.key,
    required this.session,
    this.initialPlate,
  });

  final AuthSession session;
  final String? initialPlate;

  @override
  State<RegisterVehicleScreen> createState() => _RegisterVehicleScreenState();
}

class _RegisterVehicleScreenState extends State<RegisterVehicleScreen> {
  late final ApiClient _api = ApiClient(widget.session);

  int _step = 0;
  bool _loading = false;
  String? _error;

  final _searchController = TextEditingController();
  final _fullNameController = TextEditingController();
  final _phoneController = TextEditingController();
  final _emailController = TextEditingController();

  final _platePrefixController = TextEditingController();
  final _plateSuffixController = TextEditingController();
  final _vinController = TextEditingController();
  final _makeController = TextEditingController();
  final _modelController = TextEditingController();
  final _yearController = TextEditingController();

  Timer? _searchDebounce;
  List<Map<String, dynamic>> _searchResults = [];
  Map<String, dynamic>? _selectedCustomer;

  String? _qrPayload;
  Map<String, dynamic>? _registeredVehicle;

  @override
  void initState() {
    super.initState();
    final initial = PlateNumberParts.tryParse(widget.initialPlate);
    if (initial != null) {
      _platePrefixController.text = initial.prefix;
      _plateSuffixController.text = initial.suffix;
    }
  }

  @override
  void dispose() {
    _searchDebounce?.cancel();
    _searchController.dispose();
    _fullNameController.dispose();
    _phoneController.dispose();
    _emailController.dispose();
    _platePrefixController.dispose();
    _plateSuffixController.dispose();
    _vinController.dispose();
    _makeController.dispose();
    _modelController.dispose();
    _yearController.dispose();
    super.dispose();
  }

  void _onSearchChanged(String value) {
    _searchDebounce?.cancel();
    if (value.trim().length < 2) {
      setState(() => _searchResults = []);
      return;
    }
    _searchDebounce = Timer(const Duration(milliseconds: 350), () => _runSearch(value));
  }

  Future<void> _runSearch(String q) async {
    try {
      final results = await _api.searchCustomers(q);
      if (!mounted) return;
      setState(() {
        _searchResults =
            results.map((e) => (e as Map<String, dynamic>).cast<String, dynamic>()).toList();
      });
    } catch (_) {
      if (mounted) setState(() => _searchResults = []);
    }
  }

  void _selectCustomer(Map<String, dynamic> customer) {
    setState(() {
      _selectedCustomer = customer;
      _fullNameController.text = customer['fullName']?.toString() ?? '';
      _phoneController.text = customer['phone']?.toString() ?? '';
      _searchResults = [];
      _searchController.clear();
    });
  }

  void _clearCustomer() {
    setState(() {
      _selectedCustomer = null;
      _fullNameController.clear();
      _phoneController.clear();
      _emailController.clear();
    });
  }

  bool _validateCustomerStep() {
    if (_selectedCustomer != null) return true;
    if (_fullNameController.text.trim().isEmpty || _phoneController.text.trim().isEmpty) {
      setState(() => _error = 'Enter customer name and phone, or pick an existing customer.');
      return false;
    }
    return true;
  }

  bool _validateVehicleStep() {
    final message = PlateNumberParts.validationMessage(
      _platePrefixController.text,
      _plateSuffixController.text,
    );
    if (message != null) {
      setState(() => _error = message);
      return false;
    }
    return true;
  }

  String get _formattedPlate => PlateNumberParts(
        prefix: _platePrefixController.text.trim(),
        suffix: _plateSuffixController.text.trim(),
      ).formatted;

  Future<void> _submitRegistration() async {
    if (!_validateVehicleStep()) return;

    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      String customerId;
      String customerName;
      String customerPhone;

      if (_selectedCustomer != null) {
        customerId = _selectedCustomer!['id'] as String;
        customerName = _selectedCustomer!['fullName'] as String;
        customerPhone = _selectedCustomer!['phone'] as String;
      } else {
        final created = await _api.createCustomer(
          fullName: _fullNameController.text.trim(),
          phone: _phoneController.text.trim(),
          email: _emailController.text.trim().isEmpty ? null : _emailController.text.trim(),
        );
        customerId = created['id'] as String;
        customerName = created['fullName'] as String;
        customerPhone = created['phone'] as String;
      }

      final yearText = _yearController.text.trim();
      final year = yearText.isEmpty ? null : int.tryParse(yearText);

      final platePrefix = _platePrefixController.text.trim();
      final plateSuffix = _plateSuffixController.text.trim();

      final vehicle = await _api.createVehicle(
        customerId: customerId,
        platePrefix: platePrefix,
        plateSuffix: plateSuffix,
        vin: _vinController.text.trim().isEmpty ? null : _vinController.text.trim(),
        make: _makeController.text.trim().isEmpty ? null : _makeController.text.trim(),
        model: _modelController.text.trim().isEmpty ? null : _modelController.text.trim(),
        year: year,
      );

      final lookup = await _api.getVehicleByPlateParts(
        platePrefix: platePrefix,
        plateSuffix: plateSuffix,
      );

      setState(() {
        _qrPayload = vehicle['qrPayload'] as String?;
        _registeredVehicle = lookup;
        _registeredVehicle!['customerName'] = customerName;
        _registeredVehicle!['customerPhone'] = customerPhone;
        _step = 2;
      });
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  void _takeJob() {
    final v = _registeredVehicle;
    if (v == null) return;
    Navigator.of(context).pop(v);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Register vehicle'),
      ),
      body: Stepper(
        type: StepperType.vertical,
        currentStep: _step,
        onStepContinue: () {
          if (_loading) return;
          setState(() => _error = null);
          if (_step == 0) {
            if (_validateCustomerStep()) setState(() => _step = 1);
          } else if (_step == 1) {
            _submitRegistration();
          } else if (_step == 2) {
            _takeJob();
          }
        },
        onStepCancel: _step == 0
            ? null
            : () {
                if (_step == 2) {
                  Navigator.of(context).pop(_registeredVehicle);
                } else {
                  setState(() => _step = _step - 1);
                }
              },
        controlsBuilder: (context, details) {
          if (_step == 2) {
            return Padding(
              padding: const EdgeInsets.only(top: 16),
              child: Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: details.onStepCancel,
                      child: const Text('Done'),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: FilledButton(
                      onPressed: details.onStepContinue,
                      child: const Text('Take job'),
                    ),
                  ),
                ],
              ),
            );
          }
          return Padding(
            padding: const EdgeInsets.only(top: 16),
            child: Row(
              children: [
                if (_step > 0)
                  TextButton(
                    onPressed: details.onStepCancel,
                    child: const Text('Back'),
                  ),
                const Spacer(),
                FilledButton(
                  onPressed: details.onStepContinue,
                  child: _loading
                      ? const SizedBox(
                          height: 20,
                          width: 20,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : Text(_step == 1 ? 'Register' : 'Continue'),
                ),
              ],
            ),
          );
        },
        steps: [
          Step(
            title: const Text('Customer'),
            subtitle: const Text('Existing or new owner'),
            isActive: _step >= 0,
            state: _step > 0 ? StepState.complete : StepState.indexed,
            content: _CustomerStep(
              selected: _selectedCustomer,
              searchController: _searchController,
              fullNameController: _fullNameController,
              phoneController: _phoneController,
              emailController: _emailController,
              searchResults: _searchResults,
              onSearchChanged: _onSearchChanged,
              onSelect: _selectCustomer,
              onClear: _clearCustomer,
            ),
          ),
          Step(
            title: const Text('Vehicle'),
            subtitle: const Text('Plate and optional details'),
            isActive: _step >= 1,
            state: _step > 1 ? StepState.complete : (_step == 1 ? StepState.indexed : StepState.disabled),
            content: _VehicleStep(
              platePrefixController: _platePrefixController,
              plateSuffixController: _plateSuffixController,
              vinController: _vinController,
              makeController: _makeController,
              modelController: _modelController,
              yearController: _yearController,
            ),
          ),
          Step(
            title: const Text('QR code'),
            subtitle: const Text('Print or share for next visit'),
            isActive: _step >= 2,
            state: _step == 2 ? StepState.indexed : StepState.disabled,
            content: _qrPayload == null
                ? const SizedBox.shrink()
                : VehicleQrDisplay(
                    qrPayload: _qrPayload!,
                    plateNumber: _formattedPlate,
                  ),
          ),
        ],
      ),
      bottomNavigationBar: _error == null
          ? null
          : SafeArea(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: InlineError(message: _error!),
              ),
            ),
    );
  }
}

class _CustomerStep extends StatelessWidget {
  const _CustomerStep({
    required this.selected,
    required this.searchController,
    required this.fullNameController,
    required this.phoneController,
    required this.emailController,
    required this.searchResults,
    required this.onSearchChanged,
    required this.onSelect,
    required this.onClear,
  });

  final Map<String, dynamic>? selected;
  final TextEditingController searchController;
  final TextEditingController fullNameController;
  final TextEditingController phoneController;
  final TextEditingController emailController;
  final List<Map<String, dynamic>> searchResults;
  final ValueChanged<String> onSearchChanged;
  final ValueChanged<Map<String, dynamic>> onSelect;
  final VoidCallback onClear;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    if (selected != null) {
      return Card(
        child: ListTile(
          leading: const Icon(Icons.person),
          title: Text(selected!['fullName']?.toString() ?? ''),
          subtitle: Text(selected!['phone']?.toString() ?? ''),
          trailing: IconButton(
            onPressed: onClear,
            icon: const Icon(Icons.close),
            tooltip: 'Choose different customer',
          ),
        ),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        TextField(
          controller: searchController,
          decoration: const InputDecoration(
            labelText: 'Search existing customer',
            hintText: 'Name or phone',
            prefixIcon: Icon(Icons.search),
          ),
          onChanged: onSearchChanged,
        ),
        if (searchResults.isNotEmpty) ...[
          const SizedBox(height: 8),
          Material(
            elevation: 1,
            borderRadius: BorderRadius.circular(12),
            color: scheme.surfaceContainerLow,
            child: Column(
              children: searchResults
                  .map(
                    (c) => ListTile(
                      dense: true,
                      title: Text(c['fullName']?.toString() ?? ''),
                      subtitle: Text(c['phone']?.toString() ?? ''),
                      onTap: () => onSelect(c),
                    ),
                  )
                  .toList(),
            ),
          ),
        ],
        const SizedBox(height: 20),
        Text(
          'Or register new customer',
          style: theme.textTheme.titleSmall?.copyWith(color: scheme.onSurfaceVariant),
        ),
        const SizedBox(height: 12),
        TextField(
          controller: fullNameController,
          decoration: const InputDecoration(
            labelText: 'Full name',
            prefixIcon: Icon(Icons.person_outline),
          ),
          textCapitalization: TextCapitalization.words,
        ),
        const SizedBox(height: 12),
        TextField(
          controller: phoneController,
          decoration: const InputDecoration(
            labelText: 'Phone',
            prefixIcon: Icon(Icons.phone_outlined),
          ),
          keyboardType: TextInputType.phone,
        ),
        const SizedBox(height: 12),
        TextField(
          controller: emailController,
          decoration: const InputDecoration(
            labelText: 'Email (optional)',
            prefixIcon: Icon(Icons.email_outlined),
          ),
          keyboardType: TextInputType.emailAddress,
        ),
      ],
    );
  }
}

class _VehicleStep extends StatelessWidget {
  const _VehicleStep({
    required this.platePrefixController,
    required this.plateSuffixController,
    required this.vinController,
    required this.makeController,
    required this.modelController,
    required this.yearController,
  });

  final TextEditingController platePrefixController;
  final TextEditingController plateSuffixController;
  final TextEditingController vinController;
  final TextEditingController makeController;
  final TextEditingController modelController;
  final TextEditingController yearController;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        PlateNumberInput(
          prefixController: platePrefixController,
          suffixController: plateSuffixController,
        ),
        const SizedBox(height: 12),
        TextField(
          controller: vinController,
          decoration: const InputDecoration(
            labelText: 'VIN (optional)',
            prefixIcon: Icon(Icons.numbers),
          ),
        ),
        const SizedBox(height: 12),
        Row(
          children: [
            Expanded(
              child: TextField(
                controller: makeController,
                decoration: const InputDecoration(labelText: 'Make'),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: TextField(
                controller: modelController,
                decoration: const InputDecoration(labelText: 'Model'),
              ),
            ),
          ],
        ),
        const SizedBox(height: 12),
        TextField(
          controller: yearController,
          keyboardType: TextInputType.number,
          inputFormatters: [FilteringTextInputFormatter.digitsOnly],
          decoration: const InputDecoration(
            labelText: 'Year (optional)',
            prefixIcon: Icon(Icons.calendar_today_outlined),
          ),
        ),
      ],
    );
  }
}
