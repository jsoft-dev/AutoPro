import 'dart:async';

import 'package:flutter/material.dart';

import '../../../core/api_client.dart';
import '../../../models/inventory_models.dart';

typedef InventorySearch = Future<InventorySearchPage> Function(
  String query, {
  int offset,
});

/// Inventory part picker that opens a dedicated sheet above the keyboard
/// (avoids [SearchAnchor] overlays hiding behind the keyboard in parent sheets).
class InventoryPartSearchField extends StatelessWidget {
  const InventoryPartSearchField({
    super.key,
    required this.onSearch,
    required this.value,
    required this.onChanged,
  });

  final InventorySearch onSearch;
  final InventorySearchItem? value;
  final ValueChanged<InventorySearchItem?> onChanged;

  Future<void> _openPicker(BuildContext context) async {
    FocusManager.instance.primaryFocus?.unfocus();
    await Future<void>.delayed(Duration.zero);

    if (!context.mounted) return;

    final picked = await showModalBottomSheet<InventorySearchItem>(
      context: context,
      useRootNavigator: true,
      isScrollControlled: true,
      showDragHandle: true,
      useSafeArea: true,
      builder: (ctx) => _InventoryPartPickerSheet(onSearch: onSearch),
    );

    if (picked != null) {
      onChanged(picked);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final selected = value;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Material(
          color: scheme.surfaceContainerHighest,
          borderRadius: BorderRadius.circular(28),
          child: InkWell(
            borderRadius: BorderRadius.circular(28),
            onTap: () => _openPicker(context),
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 4),
              child: Row(
                children: [
                  const SizedBox(width: 12),
                  Icon(Icons.inventory_2_outlined, color: scheme.onSurfaceVariant),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Text(
                      selected?.label ?? 'Search by part number or name',
                      style: theme.textTheme.bodyLarge?.copyWith(
                        color: selected != null ? scheme.onSurface : scheme.onSurfaceVariant,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  if (selected != null)
                    IconButton(
                      icon: const Icon(Icons.close),
                      tooltip: 'Clear',
                      onPressed: () => onChanged(null),
                    )
                  else
                    Padding(
                      padding: const EdgeInsets.only(right: 12),
                      child: Icon(Icons.keyboard_arrow_down, color: scheme.onSurfaceVariant),
                    ),
                ],
              ),
            ),
          ),
        ),
        if (selected != null)
          Padding(
            padding: const EdgeInsets.only(top: 6, left: 4),
            child: Text(
              '${selected.partNumber} · ${selected.quantityOnHand} on hand',
              style: theme.textTheme.bodySmall?.copyWith(color: scheme.primary),
            ),
          ),
      ],
    );
  }
}

class _InventoryPartPickerSheet extends StatefulWidget {
  const _InventoryPartPickerSheet({required this.onSearch});

  final InventorySearch onSearch;

  @override
  State<_InventoryPartPickerSheet> createState() => _InventoryPartPickerSheetState();
}

class _InventoryPartPickerSheetState extends State<_InventoryPartPickerSheet> {
  final TextEditingController _query = TextEditingController();
  final FocusNode _searchFocus = FocusNode();

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) _searchFocus.requestFocus();
    });
  }

  @override
  void dispose() {
    _query.dispose();
    _searchFocus.dispose();
    super.dispose();
  }

  void _onSelect(InventorySearchItem part) {
    Navigator.pop(context, part);
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final bottomInset = MediaQuery.viewInsetsOf(context).bottom;
    final height = MediaQuery.sizeOf(context).height;

    return Padding(
      padding: EdgeInsets.fromLTRB(20, 0, 20, 16 + bottomInset),
      child: SizedBox(
        height: height * 0.72,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('Choose part', style: theme.textTheme.titleLarge),
            const SizedBox(height: 4),
            Text(
              'Search stock by part number or name.',
              style: theme.textTheme.bodyMedium?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _query,
              focusNode: _searchFocus,
              decoration: const InputDecoration(
                hintText: 'Part number or name',
                prefixIcon: Icon(Icons.search),
              ),
              textInputAction: TextInputAction.search,
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: 12),
            Expanded(
              child: _PaginatedInventorySuggestions(
                key: ValueKey(_query.text),
                query: _query.text,
                onSearch: widget.onSearch,
                onSelect: _onSelect,
                expand: true,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _PaginatedInventorySuggestions extends StatefulWidget {
  const _PaginatedInventorySuggestions({
    super.key,
    required this.query,
    required this.onSearch,
    required this.onSelect,
    this.expand = false,
  });

  final String query;
  final InventorySearch onSearch;
  final ValueChanged<InventorySearchItem> onSelect;
  final bool expand;

  @override
  State<_PaginatedInventorySuggestions> createState() =>
      _PaginatedInventorySuggestionsState();
}

class _PaginatedInventorySuggestionsState extends State<_PaginatedInventorySuggestions> {
  static const _debounceMs = 280;

  final List<InventorySearchItem> _items = [];
  bool _hasMore = false;
  bool _loading = false;
  bool _loadingMore = false;
  String? _error;
  Timer? _debounce;
  int _loadGeneration = 0;
  String _activeQuery = '';

  @override
  void initState() {
    super.initState();
    _scheduleLoad(widget.query);
  }

  @override
  void didUpdateWidget(_PaginatedInventorySuggestions oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.query != widget.query) {
      _scheduleLoad(widget.query);
    }
  }

  @override
  void dispose() {
    _debounce?.cancel();
    super.dispose();
  }

  void _scheduleLoad(String query) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: _debounceMs), () {
      if (!mounted) return;
      _loadFirstPage(query.trim());
    });
  }

  Future<void> _loadFirstPage(String query) async {
    final generation = ++_loadGeneration;
    _activeQuery = query;

    setState(() {
      _loading = true;
      _loadingMore = false;
      _error = null;
      _items.clear();
      _hasMore = false;
    });

    try {
      final page = await widget.onSearch(query, offset: 0);
      if (!mounted || generation != _loadGeneration) return;
      setState(() {
        _items
          ..clear()
          ..addAll(page.items);
        _hasMore = page.hasMore;
        _loading = false;
      });
    } catch (e) {
      if (!mounted || generation != _loadGeneration) return;
      setState(() {
        _loading = false;
        _error = userFacingError(e);
      });
    }
  }

  Future<void> _loadNextPage() async {
    if (_loading || _loadingMore || !_hasMore) return;

    final generation = _loadGeneration;
    final query = _activeQuery;
    final offset = _items.length;

    setState(() => _loadingMore = true);

    try {
      final page = await widget.onSearch(query, offset: offset);
      if (!mounted || generation != _loadGeneration) return;
      setState(() {
        _items.addAll(page.items);
        _hasMore = page.hasMore;
        _loadingMore = false;
      });
    } catch (e) {
      if (!mounted || generation != _loadGeneration) return;
      setState(() {
        _loadingMore = false;
        _error = userFacingError(e);
      });
    }
  }

  bool _onScroll(ScrollNotification notification) {
    if (!_hasMore || _loadingMore || _loading) return false;
    final metrics = notification.metrics;
    if (metrics.maxScrollExtent <= 0) return false;
    if (metrics.pixels >= metrics.maxScrollExtent - 72) {
      _loadNextPage();
    }
    return false;
  }

  Widget _buildListView() {
    return NotificationListener<ScrollNotification>(
      onNotification: _onScroll,
      child: ListView.separated(
        keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
        itemCount: _items.length + (_hasMore || _loadingMore ? 1 : 0),
        separatorBuilder: (_, __) => const Divider(height: 1),
        itemBuilder: (context, index) {
          if (index >= _items.length) {
            return const Padding(
              padding: EdgeInsets.symmetric(vertical: 12),
              child: Center(
                child: SizedBox(
                  width: 22,
                  height: 22,
                  child: CircularProgressIndicator(strokeWidth: 2),
                ),
              ),
            );
          }

          final part = _items[index];
          return ListTile(
            title: Text(part.name),
            subtitle: Text(
              '${part.partNumber} · ${part.quantityOnHand} on hand · '
              'Rs ${part.unitPrice.toStringAsFixed(2)}',
            ),
            onTap: () => widget.onSelect(part),
          );
        },
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final query = widget.query.trim();

    if (_loading && _items.isEmpty) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_error != null && _items.isEmpty) {
      return Center(
        child: Text(_error!, style: TextStyle(color: theme.colorScheme.error, fontSize: 13)),
      );
    }

    if (_items.isEmpty) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              query.isEmpty ? 'No parts in stock' : 'No matches for "$query"',
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 4),
            const Text('Try another part number or name'),
          ],
        ),
      );
    }

    if (widget.expand) {
      return _buildListView();
    }

    return SizedBox(
      height: 280,
      child: _buildListView(),
    );
  }
}
