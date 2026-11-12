class InventorySearchPage {
  const InventorySearchPage({required this.items, required this.hasMore});

  factory InventorySearchPage.fromJson(Map<String, dynamic> json) => InventorySearchPage(
        items: (json['items'] as List<dynamic>? ?? [])
            .map((e) => InventorySearchItem.fromJson(e as Map<String, dynamic>))
            .toList(),
        hasMore: json['hasMore'] as bool? ?? false,
      );

  final List<InventorySearchItem> items;
  final bool hasMore;
}

class InventorySearchItem {
  InventorySearchItem({
    required this.id,
    required this.partNumber,
    required this.name,
    required this.quantityOnHand,
    required this.unitPrice,
  });

  factory InventorySearchItem.fromJson(Map<String, dynamic> json) => InventorySearchItem(
        id: json['id'] as String,
        partNumber: json['partNumber'] as String? ?? '',
        name: json['name'] as String? ?? '',
        quantityOnHand: _toDecimal(json['quantityOnHand']),
        unitPrice: _toDecimal(json['unitPrice']),
      );

  final String id;
  final String partNumber;
  final String name;
  final double quantityOnHand;
  final double unitPrice;

  String get label => partNumber.isEmpty ? name : '$partNumber · $name';

  static double _toDecimal(dynamic v) {
    if (v is num) return v.toDouble();
    return double.tryParse(v.toString()) ?? 0;
  }
}
