import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../../models/inventory_models.dart';
import '../../../models/job_models.dart';

typedef PurchaseSubmit = Future<void> Function({
  required PurchaseSource source,
  required String description,
  required double quantity,
  required double unitPrice,
  String? vendorName,
  String? inventoryItemId,
});

typedef InventorySearch = Future<List<InventorySearchItem>> Function(String query);

class AddPurchaseSheet extends StatefulWidget {
  const AddPurchaseSheet({
    super.key,
    required this.onSubmit,
    required this.onSearchInventory,
  });

  final PurchaseSubmit onSubmit;
  final InventorySearch onSearchInventory;

  @override
  State<AddPurchaseSheet> createState() => _AddPurchaseSheetState();
}

class _AddPurchaseSheetState extends State<AddPurchaseSheet> {
  PurchaseSource _source = PurchaseSource.inventory;
  final _description = TextEditingController();
  final _quantity = TextEditingController(text: '1');
  final _unitPrice = TextEditingController();
  final _vendor = TextEditingController();
  final _inventoryQuery = TextEditingController();

  InventorySearchItem? _selectedPart;
  List<InventorySearchItem> _searchResults = [];
  bool _searching = false;
  Timer? _searchDebounce;

  bool _loading = false;
  String? _error;

  @override
  void dispose() {
    _searchDebounce?.cancel();
    _description.dispose();
    _quantity.dispose();
    _unitPrice.dispose();
    _vendor.dispose();
    _inventoryQuery.dispose();
    super.dispose();
  }

  void _onInventoryQueryChanged(String value) {
    _searchDebounce?.cancel();
    if (value.trim().length < 1) {
      setState(() {
        _searchResults = [];
        _searching = false;
      });
      return;
    }
    _searchDebounce = Timer(const Duration(milliseconds: 350), () => _runSearch(value));
  }

  Future<void> _runSearch(String query) async {
    setState(() {
      _searching = true;
      _error = null;
    });
    try {
      final results = await widget.onSearchInventory(query);
      if (!mounted) return;
      setState(() {
        _searchResults = results;
        _searching = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _searching = false;
        _searchResults = [];
        _error = e.toString();
      });
    }
  }

  void _selectPart(InventorySearchItem part) {
    setState(() {
      _selectedPart = part;
      _description.text = part.name;
      _unitPrice.text = part.unitPrice > 0 ? part.unitPrice.toStringAsFixed(2) : '';
      _searchResults = [];
      _inventoryQuery.text = part.label;
    });
  }

  void _clearSelectedPart() {
    setState(() {
      _selectedPart = null;
      _inventoryQuery.clear();
      _description.clear();
      _unitPrice.clear();
    });
  }

  Future<void> _submit() async {
    final description = _description.text.trim();
    final qty = double.tryParse(_quantity.text.trim()) ?? 0;
    final price = double.tryParse(_unitPrice.text.trim()) ?? 0;
    final vendor = _vendor.text.trim();

    if (_source == PurchaseSource.inventory && _selectedPart == null) {
      setState(() => _error = 'Search and select a part from shop inventory.');
      return;
    }
    if (_source == PurchaseSource.inventory &&
        _selectedPart != null &&
        qty > _selectedPart!.quantityOnHand) {
      setState(() => _error = 'Only ${_selectedPart!.quantityOnHand} on hand for this part.');
      return;
    }
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
        inventoryItemId: _source == PurchaseSource.inventory ? _selectedPart!.id : null,
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
      child: SingleChildScrollView(
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
              onSelectionChanged: (s) => setState(() {
                _source = s.first;
                _error = null;
              }),
            ),
            const SizedBox(height: 12),
            if (_source == PurchaseSource.inventory) ...[
              TextField(
                controller: _inventoryQuery,
                onChanged: _onInventoryQueryChanged,
                onTap: () {
                  if (_selectedPart == null && _searchResults.isEmpty && !_searching) {
                    _runSearch(_inventoryQuery.text);
                  }
                },
                decoration: InputDecoration(
                  labelText: 'Search parts',
                  hintText: 'Part number or name',
                  prefixIcon: const Icon(Icons.search),
                  suffixIcon: _selectedPart != null
                      ? IconButton(
                          onPressed: _clearSelectedPart,
                          icon: const Icon(Icons.close),
                          tooltip: 'Clear',
                        )
                      : _searching
                          ? const Padding(
                              padding: EdgeInsets.all(12),
                              child: SizedBox(
                                width: 18,
                                height: 18,
                                child: CircularProgressIndicator(strokeWidth: 2),
                              ),
                            )
                          : null,
                ),
              ),
              if (_selectedPart != null)
                Padding(
                  padding: const EdgeInsets.only(top: 8),
                  child: Text(
                    '${_selectedPart!.partNumber} · ${_selectedPart!.quantityOnHand} on hand',
                    style: theme.textTheme.bodySmall?.copyWith(color: scheme.primary),
                  ),
                ),
              if (_searchResults.isNotEmpty)
                ConstrainedBox(
                  constraints: const BoxConstraints(maxHeight: 160),
                  child: Material(
                    elevation: 1,
                    borderRadius: BorderRadius.circular(12),
                    child: ListView.separated(
                      shrinkWrap: true,
                      itemCount: _searchResults.length,
                      separatorBuilder: (_, __) => const Divider(height: 1),
                      itemBuilder: (context, i) {
                        final part = _searchResults[i];
                        return ListTile(
                          dense: true,
                          title: Text(part.name),
                          subtitle: Text(
                            '${part.partNumber} · ${part.quantityOnHand} on hand · Rs ${part.unitPrice.toStringAsFixed(2)}',
                          ),
                          onTap: () => _selectPart(part),
                        );
                      },
                    ),
                  ),
                ),
            ] else
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
              readOnly: _source == PurchaseSource.inventory && _selectedPart != null,
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
      ),
    );
  }
}
