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
        quantity: _toDecimal(json['quantity']),
        unitPrice: _toDecimal(json['unitPrice']),
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

  static double _toDecimal(dynamic v) {
    if (v is num) return v.toDouble();
    return double.tryParse(v.toString()) ?? 0;
  }
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
  });

  factory JobDetail.fromJson(Map<String, dynamic> json) {
    final jobType = ServiceJobType.parse(json['jobType']);
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

  double get purchasesTotal =>
      purchases.fold(0, (sum, line) => sum + line.lineTotal);

  JobDetail copyWith({
    JobStatus? status,
    List<JobPurchaseLine>? purchases,
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
      );
}
