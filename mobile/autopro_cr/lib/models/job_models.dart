enum JobStatus {
  onGoing,
  onHold,
  cancelled,
  completed,
  closed;

  String get apiValue {
    switch (this) {
      case JobStatus.onGoing:
        return 'OnGoing';
      case JobStatus.onHold:
        return 'OnHold';
      case JobStatus.cancelled:
        return 'Cancelled';
      case JobStatus.completed:
        return 'Completed';
      case JobStatus.closed:
        return 'Closed';
    }
  }

  static JobStatus parse(dynamic value) {
    if (value is JobStatus) return value;
    if (value is num) {
      final index = value.toInt();
      if (index >= 0 && index < JobStatus.values.length) {
        return JobStatus.values[index];
      }
    }
    if (value is String) {
      final normalized = value.replaceAll(RegExp(r'[\s_-]'), '').toLowerCase();
      for (final status in JobStatus.values) {
        final api = status.apiValue.replaceAll(RegExp(r'[\s_-]'), '').toLowerCase();
        final name = status.name.toLowerCase();
        if (normalized == api || normalized == name) {
          return status;
        }
      }
    }
    return JobStatus.onGoing;
  }

  static JobStatus parseJsonField(Map<String, dynamic> json) =>
      parse(json['status'] ?? json['Status']);
}

enum ServiceJobType {
  wash,
  fullService,
  repair,
  other;

  String get apiValue {
    switch (this) {
      case ServiceJobType.wash:
        return 'Wash';
      case ServiceJobType.fullService:
        return 'FullService';
      case ServiceJobType.repair:
        return 'Repair';
      case ServiceJobType.other:
        return 'Other';
    }
  }

  String get label {
    switch (this) {
      case ServiceJobType.wash:
        return 'Wash';
      case ServiceJobType.fullService:
        return 'Full Service';
      case ServiceJobType.repair:
        return 'Repair';
      case ServiceJobType.other:
        return 'Other';
    }
  }

  static ServiceJobType parse(dynamic value) {
    if (value is String) {
      final normalized = value.replaceAll(RegExp(r'[\s_-]'), '').toLowerCase();
      for (final type in ServiceJobType.values) {
        final api = type.apiValue.replaceAll(RegExp(r'[\s_-]'), '').toLowerCase();
        if (normalized == api || normalized == type.name.toLowerCase()) {
          return type;
        }
      }
    }
    if (value is int) {
      if (value >= 0 && value < ServiceJobType.values.length) {
        return ServiceJobType.values[value];
      }
    }
    return ServiceJobType.wash;
  }

  static String displayLabel({
    required ServiceJobType type,
    String? otherDescription,
    String? serverLabel,
  }) {
    if (serverLabel != null && serverLabel.isNotEmpty) return serverLabel;
    if (type == ServiceJobType.other &&
        otherDescription != null &&
        otherDescription.trim().isNotEmpty) {
      return otherDescription.trim();
    }
    return type.label;
  }
}

enum InvoiceLineItemKind {
  serviceCharge,
  purchase;

  static InvoiceLineItemKind parse(dynamic value) {
    if (value is String) {
      if (value == 'ServiceCharge') return InvoiceLineItemKind.serviceCharge;
      if (value == 'Purchase') return InvoiceLineItemKind.purchase;
    }
    return InvoiceLineItemKind.purchase;
  }

  String get label => this == InvoiceLineItemKind.serviceCharge ? 'Service charge' : 'Purchase';
}

enum LineItemSource {
  inHouse,
  outSource;

  static LineItemSource? parse(dynamic value) {
    if (value == null) return null;
    if (value is String) {
      if (value == 'InHouse') return LineItemSource.inHouse;
      if (value == 'OutSource') return LineItemSource.outSource;
    }
    return null;
  }

  String get label => this == LineItemSource.inHouse ? 'In house' : 'Outsource';
}

class JobInvoiceLineItem {
  JobInvoiceLineItem({
    required this.id,
    required this.kind,
    this.source,
    required this.description,
    required this.quantity,
    required this.unitPrice,
    this.vendorName,
    this.inventoryPartNumber,
  });

  factory JobInvoiceLineItem.fromJson(Map<String, dynamic> json) => JobInvoiceLineItem(
        id: json['id'] as String,
        kind: InvoiceLineItemKind.parse(json['kind']),
        source: LineItemSource.parse(json['source']),
        description: json['description'] as String,
        quantity: decimalFromJson(json['quantity']),
        unitPrice: decimalFromJson(json['unitPrice']),
        vendorName: json['vendorName'] as String?,
        inventoryPartNumber: json['inventoryPartNumber'] as String?,
      );

  final String id;
  final InvoiceLineItemKind kind;
  final LineItemSource? source;
  final String description;
  final double quantity;
  final double unitPrice;
  final String? vendorName;
  final String? inventoryPartNumber;

  double get lineTotal => quantity * unitPrice;
}

class JobInvoice {
  JobInvoice({
    required this.id,
    required this.finalizedAtUtc,
    required this.grandTotal,
    required this.lineItems,
  });

  factory JobInvoice.fromJson(Map<String, dynamic> json) => JobInvoice(
        id: json['id'] as String,
        finalizedAtUtc: DateTime.parse(json['finalizedAtUtc'] as String),
        grandTotal: decimalFromJson(json['grandTotal']),
        lineItems: (json['lineItems'] as List<dynamic>? ?? [])
            .map((e) => JobInvoiceLineItem.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

  final String id;
  final DateTime finalizedAtUtc;
  final double grandTotal;
  final List<JobInvoiceLineItem> lineItems;
}

enum PurchaseSource {
  inventory,
  externalVendor;

  String get apiValue =>
      this == PurchaseSource.inventory ? 'Inventory' : 'ExternalVendor';

  static PurchaseSource parse(dynamic value) {
    if (value is String) {
      if (value == 'Inventory') return PurchaseSource.inventory;
      if (value == 'ExternalVendor') return PurchaseSource.externalVendor;
    }
    if (value is int) return value == 0 ? PurchaseSource.inventory : PurchaseSource.externalVendor;
    return PurchaseSource.inventory;
  }

  String get label =>
      this == PurchaseSource.inventory ? 'Shop inventory' : 'External vendor';
}

class JobListItem {
  JobListItem({
    required this.id,
    required this.jobNumber,
    required this.status,
    required this.jobType,
    this.jobTypeOtherDescription,
    required this.jobTypeLabel,
    required this.plateNumber,
    required this.customerName,
    this.odometerReading,
    required this.createdAtUtc,
  });

  factory JobListItem.fromJson(Map<String, dynamic> json) {
    final jobType = ServiceJobType.parse(json['jobType']);
    return JobListItem(
        id: json['id'] as String,
        jobNumber: json['jobNumber'] as String,
        status: JobStatus.parseJsonField(json),
        jobType: jobType,
        jobTypeOtherDescription: json['jobTypeOtherDescription'] as String?,
        jobTypeLabel: ServiceJobType.displayLabel(
          type: jobType,
          otherDescription: json['jobTypeOtherDescription'] as String?,
          serverLabel: json['jobTypeLabel'] as String?,
        ),
        plateNumber: json['plateNumber'] as String? ?? '',
        customerName: json['customerName'] as String? ?? '',
        odometerReading: json['odometerReading'] as int?,
        createdAtUtc: DateTime.parse(json['createdAtUtc'] as String),
      );
  }

  final String id;
  final String jobNumber;
  final JobStatus status;
  final ServiceJobType jobType;
  final String? jobTypeOtherDescription;
  final String jobTypeLabel;
  final String plateNumber;
  final String customerName;
  final int? odometerReading;
  final DateTime createdAtUtc;
}

class JobListPage {
  JobListPage({
    required this.items,
    required this.page,
    required this.pageSize,
    required this.totalCount,
  });

  factory JobListPage.fromJson(Map<String, dynamic> json) {
    final rawItems = json['items'] as List<dynamic>? ?? const [];
    return JobListPage(
      items: rawItems
          .map((e) => JobListItem.fromJson(e as Map<String, dynamic>))
          .toList(),
      page: json['page'] as int? ?? 1,
      pageSize: json['pageSize'] as int? ?? rawItems.length,
      totalCount: json['totalCount'] as int? ?? rawItems.length,
    );
  }

  final List<JobListItem> items;
  final int page;
  final int pageSize;
  final int totalCount;

  bool get hasMore => page * pageSize < totalCount;
}

class JobPurchaseLine {
  JobPurchaseLine({
    required this.id,
    required this.source,
    this.inventoryItemId,
    this.inventoryPartNumber,
    required this.description,
    required this.quantity,
    required this.unitPrice,
    this.vendorName,
    required this.createdAtUtc,
  });

  factory JobPurchaseLine.fromJson(Map<String, dynamic> json) => JobPurchaseLine(
        id: json['id'] as String,
        source: PurchaseSource.parse(json['source']),
        inventoryItemId: json['inventoryItemId'] as String?,
        inventoryPartNumber: json['inventoryPartNumber'] as String?,
        description: json['description'] as String,
        quantity: decimalFromJson(json['quantity']),
        unitPrice: decimalFromJson(json['unitPrice']),
        vendorName: json['vendorName'] as String?,
        createdAtUtc: DateTime.parse(json['createdAtUtc'] as String),
      );

  final String id;
  final PurchaseSource source;
  final String? inventoryItemId;
  final String? inventoryPartNumber;
  final String description;
  final double quantity;
  final double unitPrice;
  final String? vendorName;
  final DateTime createdAtUtc;

  double get lineTotal => quantity * unitPrice;
}

double decimalFromJson(dynamic v) {
  if (v is num) return v.toDouble();
  return double.tryParse(v.toString()) ?? 0;
}

class JobDetail {
  JobDetail({
    required this.id,
    required this.jobNumber,
    required this.status,
    required this.jobType,
    this.jobTypeOtherDescription,
    required this.jobTypeLabel,
    required this.plateNumber,
    required this.customerName,
    this.odometerReading,
    this.notes,
    required this.createdAtUtc,
    required this.purchases,
    this.invoice,
    this.jobTypeServiceChargeLkr = 0,
    this.customServiceChargeLkr,
    this.vehicleLastRecordedOdometerKm,
  });

  factory JobDetail.fromJson(Map<String, dynamic> json) {
    final jobType = ServiceJobType.parse(json['jobType']);
    final invoiceJson = json['invoice'] as Map<String, dynamic>?;
    return JobDetail(
        id: json['id'] as String,
        jobNumber: json['jobNumber'] as String,
        status: JobStatus.parseJsonField(json),
        jobType: jobType,
        jobTypeOtherDescription: json['jobTypeOtherDescription'] as String?,
        jobTypeLabel: ServiceJobType.displayLabel(
          type: jobType,
          otherDescription: json['jobTypeOtherDescription'] as String?,
          serverLabel: json['jobTypeLabel'] as String?,
        ),
        plateNumber: json['plateNumber'] as String? ?? '',
        customerName: json['customerName'] as String? ?? '',
        odometerReading: json['odometerReading'] as int?,
        notes: json['notes'] as String?,
        createdAtUtc: DateTime.parse(json['createdAtUtc'] as String),
        purchases: (json['purchases'] as List<dynamic>? ?? [])
            .map((e) => JobPurchaseLine.fromJson(e as Map<String, dynamic>))
            .toList(),
        invoice: invoiceJson == null ? null : JobInvoice.fromJson(invoiceJson),
        jobTypeServiceChargeLkr: decimalFromJson(json['jobTypeServiceChargeLkr'] ?? 0),
        customServiceChargeLkr: json['customServiceChargeLkr'] == null
            ? null
            : decimalFromJson(json['customServiceChargeLkr']),
        vehicleLastRecordedOdometerKm: json['vehicleLastRecordedOdometerKm'] as int?,
      );
  }

  final String id;
  final String jobNumber;
  final JobStatus status;
  final ServiceJobType jobType;
  final String? jobTypeOtherDescription;
  final String jobTypeLabel;
  final String plateNumber;
  final String customerName;
  final int? odometerReading;
  final String? notes;
  final DateTime createdAtUtc;
  final List<JobPurchaseLine> purchases;
  final JobInvoice? invoice;
  final double jobTypeServiceChargeLkr;
  final double? customServiceChargeLkr;
  final int? vehicleLastRecordedOdometerKm;

  double get purchasesTotal =>
      purchases.fold(0, (sum, line) => sum + line.lineTotal);

  double get amountDue =>
      invoice?.grandTotal ?? purchasesTotal + jobTypeServiceChargeLkr;

  bool get hasInvoice => invoice != null;

  JobDetail copyWith({
    JobStatus? status,
    List<JobPurchaseLine>? purchases,
    JobInvoice? invoice,
    double? jobTypeServiceChargeLkr,
    double? customServiceChargeLkr,
    String? notes,
    int? odometerReading,
  }) =>
      JobDetail(
        id: id,
        jobNumber: jobNumber,
        status: status ?? this.status,
        jobType: jobType,
        jobTypeOtherDescription: jobTypeOtherDescription,
        jobTypeLabel: jobTypeLabel,
        plateNumber: plateNumber,
        customerName: customerName,
        odometerReading: odometerReading ?? this.odometerReading,
        notes: notes ?? this.notes,
        createdAtUtc: createdAtUtc,
        purchases: purchases ?? this.purchases,
        invoice: invoice ?? this.invoice,
        jobTypeServiceChargeLkr:
            jobTypeServiceChargeLkr ?? this.jobTypeServiceChargeLkr,
        customServiceChargeLkr:
            customServiceChargeLkr ?? this.customServiceChargeLkr,
        vehicleLastRecordedOdometerKm: vehicleLastRecordedOdometerKm,
      );
}

String? validateOdometerAgainstLast(int? value, int? lastRecordedKm) {
  if (value == null || lastRecordedKm == null) return null;
  if (value < lastRecordedKm) {
    return 'Cannot be less than last recorded ($lastRecordedKm km).';
  }
  return null;
}
