import 'dart:async';

import 'package:flutter/material.dart';

import '../../../models/inventory_models.dart';

typedef InventorySearch = Future<InventorySearchPage> Function(
  String query, {
  int offset,
});

/// Dropdown-style part picker with debounced search and scroll-to-load-more pages.
class InventoryPartSearchField extends StatefulWidget {
  const InventoryPartSearchField({
    super.key,
    required this.onSearch,
    required this.value,
    required this.onChanged,
  });

  final InventorySearch onSearch;
  final InventorySearchItem? value;
  final ValueChanged<InventorySearchItem?> onChanged;

  @override
  State<InventoryPartSearchField> createState() => _InventoryPartSearchFieldState();
}

class _InventoryPartSearchFieldState extends State<InventoryPartSearchField> {
  final SearchController _searchController = SearchController();

  @override
  void initState() {
    super.initState();
    final part = widget.value;
    if (part != null) {
      _searchController.text = part.label;
    }
  }

  @override
  void didUpdateWidget(InventoryPartSearchField oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.value != widget.value) {
      if (widget.value != null) {
        _searchController.text = widget.value!.label;
      } else {
        _searchController.clear();
      }
    }
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  void _clearSelection() {
    widget.onChanged(null);
    _searchController.clear();
    _searchController.openView();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final selected = widget.value;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        SearchAnchor(
          searchController: _searchController,
          isFullScreen: false,
          viewConstraints: const BoxConstraints(maxHeight: 320),
          viewOnClose: () {
            final part = widget.value;
            if (part != null) {
              _searchController.text = part.label;
            }
          },
          builder: (context, controller) {
            return SearchBar(
              controller: controller,
              hintText: 'Search by part number or name',
              leading: const Icon(Icons.inventory_2_outlined),
              elevation: const WidgetStatePropertyAll(0),
              backgroundColor: WidgetStatePropertyAll(scheme.surfaceContainerHighest),
              trailing: [
                if (selected != null)
                  IconButton(
                    icon: const Icon(Icons.close),
                    tooltip: 'Clear',
                    onPressed: _clearSelection,
                  )
                else
                  Icon(
                    controller.isOpen ? Icons.keyboard_arrow_up : Icons.keyboard_arrow_down,
                    color: scheme.onSurfaceVariant,
                  ),
              ],
              onTap: () => controller.openView(),
              onChanged: (_) => controller.openView(),
            );
          },
          suggestionsBuilder: (context, controller) async {
            return [
              _PaginatedInventorySuggestions(
                key: ValueKey(controller.text),
                query: controller.text,
                onSearch: widget.onSearch,
                onSelect: (part) {
                  widget.onChanged(part);
                  controller.closeView(part.label);
                  _searchController.text = part.label;
                },
              ),
            ];
          },
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

class _PaginatedInventorySuggestions extends StatefulWidget {
  const _PaginatedInventorySuggestions({
    super.key,
    required this.query,
    required this.onSearch,
    required this.onSelect,
  });

  final String query;
  final InventorySearch onSearch;
  final ValueChanged<InventorySearchItem> onSelect;

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
        _error = e.toString();
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
        _error = e.toString();
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

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final query = widget.query.trim();

    if (_loading && _items.isEmpty) {
      return const SizedBox(
        height: 120,
        child: Center(child: CircularProgressIndicator()),
      );
    }

    if (_error != null && _items.isEmpty) {
      return SizedBox(
        height: 100,
        child: Center(
          child: Text(_error!, style: TextStyle(color: theme.colorScheme.error, fontSize: 13)),
        ),
      );
    }

    if (_items.isEmpty) {
      return ListTile(
        enabled: false,
        title: Text(
          query.isEmpty ? 'No parts in stock' : 'No matches for "$query"',
        ),
        subtitle: const Text('Try another part number or name'),
      );
    }

    return SizedBox(
      height: 280,
      child: NotificationListener<ScrollNotification>(
        onNotification: _onScroll,
        child: ListView.separated(
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
      ),
    );
  }
}
