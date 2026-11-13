import 'dart:convert';

import 'package:http/http.dart' as http;

import '../models/inventory_models.dart';
import '../models/job_models.dart';
import 'api_config.dart';
import 'auth_session.dart';

class ApiClient {
  ApiClient(this._session, {String? baseUrl})
      : baseUrl = baseUrl ?? ApiConfig.baseUrl;

  final AuthSession _session;
  final String baseUrl;

  Future<Map<String, dynamic>> login(String userName, String password) async {
    final res = await http.post(
      Uri.parse('$baseUrl/api/v1/auth/login'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'userName': userName, 'password': password}),
    );
    _throwIfError(res);
    return jsonDecode(res.body) as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> getVehicleByQr(String token) async {
    final res = await http.get(
      Uri.parse('$baseUrl/api/v1/vehicles/by-qr/$token'),
      headers: _authHeaders(),
    );
    _throwIfError(res);
    return jsonDecode(res.body) as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> createCustomer({
    required String fullName,
    required String phone,
    String? email,
  }) async {
    final res = await http.post(
      Uri.parse('$baseUrl/api/v1/customers'),
      headers: {
        ..._authHeaders(),
        'Content-Type': 'application/json',
      },
      body: jsonEncode({
        'fullName': fullName,
        'phone': phone,
        'email': email,
      }),
    );
    _throwIfError(res);
    return jsonDecode(res.body) as Map<String, dynamic>;
  }

  Future<List<dynamic>> searchCustomers(String query) async {
    final res = await http.get(
      Uri.parse('$baseUrl/api/v1/customers/search').replace(
        queryParameters: {'q': query.trim()},
      ),
      headers: _authHeaders(),
    );
    _throwIfError(res);
    return jsonDecode(res.body) as List<dynamic>;
  }

  Future<Map<String, dynamic>> createVehicle({
    required String customerId,
    required String platePrefix,
    required String plateSuffix,
    String? vin,
    String? make,
    String? model,
    int? year,
  }) async {
    final res = await http.post(
      Uri.parse('$baseUrl/api/v1/customers/$customerId/vehicles'),
      headers: {
        ..._authHeaders(),
        'Content-Type': 'application/json',
      },
      body: jsonEncode({
        'platePrefix': platePrefix.trim().toUpperCase(),
        'plateSuffix': plateSuffix.trim(),
        'vin': vin,
        'make': make,
        'model': model,
        'year': year,
      }),
    );
    _throwIfError(res);
    return jsonDecode(res.body) as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> getVehicleByPlateParts({
    required String platePrefix,
    required String plateSuffix,
  }) async {
    final res = await http.get(
      Uri.parse('$baseUrl/api/v1/vehicles/by-plate').replace(
        queryParameters: {
          'prefix': platePrefix.trim().toUpperCase(),
          'suffix': plateSuffix.trim(),
        },
      ),
      headers: _authHeaders(),
    );
    _throwIfError(res);
    return jsonDecode(res.body) as Map<String, dynamic>;
  }

  static const int defaultJobsPageSize = 20;

  /// Today's jobs and filters use the paginated jobs list (UTC calendar day bounds).
  Future<JobListPage> getJobs({
    DateTime? from,
    DateTime? to,
    String? status,
    int page = 1,
    int pageSize = defaultJobsPageSize,
  }) async {
    final params = <String, String>{
      'page': '$page',
      'pageSize': '$pageSize',
    };
    if (from != null) params['from'] = from.toUtc().toIso8601String();
    if (to != null) params['to'] = to.toUtc().toIso8601String();
    if (status != null && status.isNotEmpty) params['status'] = status;

    final res = await http.get(
      Uri.parse('$baseUrl/api/v1/jobs').replace(queryParameters: params),
      headers: _authHeaders(),
    );
    _throwIfError(res);
    return JobListPage.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  (DateTime from, DateTime to) utcTodayBounds() {
    final now = DateTime.now().toUtc();
    final start = DateTime.utc(now.year, now.month, now.day);
    return (start, start.add(const Duration(days: 1)));
  }

  Future<Map<String, dynamic>> getJob(String jobId) async {
    final res = await http.get(
      Uri.parse('$baseUrl/api/v1/jobs/$jobId'),
      headers: _authHeaders(),
    );
    _throwIfError(res);
    return jsonDecode(res.body) as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> updateJobDetails(
    String jobId, {
    required String jobType,
    String? jobTypeOtherDescription,
    double? customServiceChargeLkr,
    int? odometerReading,
  }) async {
    final res = await http.patch(
      Uri.parse('$baseUrl/api/v1/jobs/$jobId'),
      headers: {
        ..._authHeaders(),
        'Content-Type': 'application/json',
      },
      body: jsonEncode({
        'jobType': jobType,
        'jobTypeOtherDescription': jobTypeOtherDescription,
        'customServiceChargeLkr': customServiceChargeLkr,
        'odometerReading': odometerReading,
      }),
    );
    _throwIfError(res);
    return jsonDecode(res.body) as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> updateJobStatus(
    String jobId, {
    required String status,
    String? note,
  }) async {
    final res = await http.patch(
      Uri.parse('$baseUrl/api/v1/jobs/$jobId/status'),
      headers: {
        ..._authHeaders(),
        'Content-Type': 'application/json',
      },
      body: jsonEncode({'status': status, 'note': note}),
    );
    _throwIfError(res);
    return jsonDecode(res.body) as Map<String, dynamic>;
  }

  Future<InventorySearchPage> searchInventory(
    String query, {
    int offset = 0,
    int limit = 20,
  }) async {
    final res = await http.get(
      Uri.parse('$baseUrl/api/v1/inventory/search').replace(
        queryParameters: {
          'q': query.trim(),
          'offset': '$offset',
          'limit': '$limit',
        },
      ),
      headers: _authHeaders(),
    );
    _throwIfError(res);
    return InventorySearchPage.fromJson(jsonDecode(res.body) as Map<String, dynamic>);
  }

  Future<Map<String, dynamic>> addJobPurchase(
    String jobId, {
    required String source,
    String? inventoryItemId,
    required String description,
    required double quantity,
    required double unitPrice,
    String? vendorName,
  }) async {
    final res = await http.post(
      Uri.parse('$baseUrl/api/v1/jobs/$jobId/purchases'),
      headers: {
        ..._authHeaders(),
        'Content-Type': 'application/json',
      },
      body: jsonEncode({
        'source': source,
        'inventoryItemId': inventoryItemId,
        'description': description,
        'quantity': quantity,
        'unitPrice': unitPrice,
        'vendorName': vendorName,
      }),
    );
    _throwIfError(res);
    return jsonDecode(res.body) as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> takeJob({
    required String vehicleId,
    required String jobType,
    String? jobTypeOtherDescription,
    double? customServiceChargeLkr,
    int? odometer,
    String? notes,
  }) async {
    final res = await http.post(
      Uri.parse('$baseUrl/api/v1/jobs'),
      headers: {
        ..._authHeaders(),
        'Content-Type': 'application/json',
      },
      body: jsonEncode({
        'vehicleId': vehicleId,
        'jobType': jobType,
        'jobTypeOtherDescription': jobTypeOtherDescription,
        'customServiceChargeLkr': customServiceChargeLkr,
        'odometerReading': odometer,
        'notes': notes,
      }),
    );
    _throwIfError(res);
    return jsonDecode(res.body) as Map<String, dynamic>;
  }

  Map<String, String> _authHeaders() {
    final token = _session.accessToken;
    if (token == null || token.isEmpty) {
      throw ApiException(401, 'Not signed in');
    }
    return {'Authorization': 'Bearer $token'};
  }

  void _throwIfError(http.Response res) {
    if (res.statusCode >= 200 && res.statusCode < 300) return;
    throw ApiException(res.statusCode, _messageFromResponse(res));
  }

  static String _messageFromResponse(http.Response res) {
    try {
      final body = jsonDecode(res.body);
      if (body is Map && body['message'] != null) {
        final message = body['message'].toString().trim();
        if (message.isNotEmpty) return message;
      }
    } catch (_) {}
    return defaultMessageForHttpStatus(res.statusCode);
  }
}

/// Plain-language text for UI — never exposes exception type or status codes.
String userFacingError(Object error, {String fallback = 'Something went wrong. Try again.'}) {
  if (error is ApiException) return error.message;
  return fallback;
}

String defaultMessageForHttpStatus(int statusCode) {
  return switch (statusCode) {
    400 => 'Check your entries and try again.',
    401 => 'Please sign in again.',
    403 => 'You don\'t have permission to do that.',
    404 => 'That item was not found.',
    409 => 'This action conflicts with existing data.',
    422 => 'Check your entries and try again.',
    >= 500 => 'Server error. Try again in a moment.',
    _ => 'Something went wrong. Try again.',
  };
}

class ApiException implements Exception {
  ApiException(this.statusCode, this.message);
  final int statusCode;
  final String message;

  @override
  String toString() => message;
}
