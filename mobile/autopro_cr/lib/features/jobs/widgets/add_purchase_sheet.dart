import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../../models/job_models.dart';

typedef PurchaseSubmit = Future<void> Function({
  required PurchaseSource source,
  required String description,
  required double quantity,
  required double unitPrice,
  String? vendorName,
});

class AddPurchaseSheet extends StatefulWidget {
  const AddPurchaseSheet({super.key, required this.onSubmit});

  final PurchaseSubmit onSubmit;

  @override
  State<AddPurchaseSheet> createState() => _AddPurchaseSheetState();
}

class _AddPurchaseSheetState extends State<AddPurchaseSheet> {
  PurchaseSource _source = PurchaseSource.inventory;
  final _description = TextEditingController();
  final _quantity = TextEditingController(text: '1');
  final _unitPrice = TextEditingController();
  final _vendor = TextEditingController();
  bool _loading = false;
  String? _error;

  @override
  void dispose() {
    _description.dispose();
    _quantity.dispose();
    _unitPrice.dispose();
    _vendor.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final description = _description.text.trim();
    final qty = double.tryParse(_quantity.text.trim()) ?? 0;
    final price = double.tryParse(_unitPrice.text.trim()) ?? 0;
    final vendor = _vendor.text.trim();

    if (description.isEmpty) {
      setState(() => _error = 'Enter a description for this line.');
      return;
    }
    if (qty <= 0) {
      setState(() => _error = 'Quantity must be greater than zero.');
      return;
    }
    if (price < 0) {
      setState(() => _error = 'Unit price cannot be negative.');
      return;
    }
    if (_source == PurchaseSource.externalVendor && vendor.isEmpty) {
      setState(() => _error = 'Vendor name is required for external purchases.');
      return;
    }

    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      await widget.onSubmit(
        source: _source,
        description: description,
        quantity: qty,
        unitPrice: price,
        vendorName: _source == PurchaseSource.externalVendor ? vendor : null,
      );
      if (mounted) Navigator.pop(context, true);
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final bottom = MediaQuery.viewInsetsOf(context).bottom;

    return Padding(
      padding: EdgeInsets.fromLTRB(20, 16, 20, 16 + bottom),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Center(
            child: Container(
              width: 36,
              height: 4,
              decoration: BoxDecoration(
                color: scheme.outlineVariant,
                borderRadius: BorderRadius.circular(2),
              ),
            ),
          ),
          const SizedBox(height: 16),
          Text('Add purchase', style: theme.textTheme.titleLarge),
          const SizedBox(height: 4),
          Text(
            'Shop parts from inventory or record items bought outside.',
            style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant),
          ),
          const SizedBox(height: 16),
          SegmentedButton<PurchaseSource>(
            segments: const [
              ButtonSegment(
                value: PurchaseSource.inventory,
                label: Text('Shop'),
                icon: Icon(Icons.inventory_2_outlined),
              ),
              ButtonSegment(
                value: PurchaseSource.externalVendor,
                label: Text('Vendor'),
                icon: Icon(Icons.storefront_outlined),
              ),
            ],
            selected: {_source},
            onSelectionChanged: (s) => setState(() => _source = s.first),
          ),
          const SizedBox(height: 12),
          if (_source == PurchaseSource.inventory)
            Material(
              color: scheme.surfaceContainerHighest,
              borderRadius: BorderRadius.circular(12),
              child: Padding(
                padding: const EdgeInsets.all(12),
                child: Row(
                  children: [
                    Icon(Icons.upcoming_outlined, color: scheme.primary, size: 20),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        'Inventory catalog search will link here in a later release. '
                        'Enter the part description manually for now.',
                        style: theme.textTheme.bodySmall,
                      ),
                    ),
                  ],
                ),
              ),
            )
          else
            TextField(
              controller: _vendor,
              decoration: const InputDecoration(
                labelText: 'Vendor name',
                prefixIcon: Icon(Icons.storefront_outlined),
              ),
              textInputAction: TextInputAction.next,
            ),
          const SizedBox(height: 12),
          TextField(
            controller: _description,
            decoration: InputDecoration(
              labelText: _source == PurchaseSource.inventory ? 'Part / service' : 'Item description',
              prefixIcon: const Icon(Icons.description_outlined),
            ),
            textInputAction: TextInputAction.next,
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: TextField(
                  controller: _quantity,
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                  inputFormatters: [
                    FilteringTextInputFormatter.allow(RegExp(r'^\d*\.?\d{0,3}')),
                  ],
                  decoration: const InputDecoration(
                    labelText: 'Qty',
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: TextField(
                  controller: _unitPrice,
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                  inputFormatters: [
                    FilteringTextInputFormatter.allow(RegExp(r'^\d*\.?\d{0,2}')),
                  ],
                  decoration: const InputDecoration(
                    labelText: 'Unit price',
                    prefixText: 'Rs ',
                  ),
                ),
              ),
            ],
          ),
          if (_error != null) ...[
            const SizedBox(height: 12),
            Text(_error!, style: TextStyle(color: scheme.error, fontSize: 13)),
          ],
          const SizedBox(height: 20),
          FilledButton(
            onPressed: _loading ? null : _submit,
            child: _loading
                ? const SizedBox(
                    height: 22,
                    width: 22,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : const Text('Add to job'),
          ),
        ],
      ),
    );
  }
}
