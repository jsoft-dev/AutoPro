import 'dart:convert';

import 'package:http/http.dart' as http;

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
    required String plateNumber,
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
        'plateNumber': plateNumber,
        'vin': vin,
        'make': make,
        'model': model,
        'year': year,
      }),
    );
    _throwIfError(res);
    return jsonDecode(res.body) as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> getVehicleByPlate(String plate) async {
    final res = await http.get(
      Uri.parse('$baseUrl/api/v1/vehicles/by-plate').replace(
        queryParameters: {'plate': plate.trim()},
      ),
      headers: _authHeaders(),
    );
    _throwIfError(res);
    return jsonDecode(res.body) as Map<String, dynamic>;
  }

  Future<List<dynamic>> getTodayJobs() async {
    final res = await http.get(
      Uri.parse('$baseUrl/api/v1/jobs/today'),
      headers: _authHeaders(),
    );
    _throwIfError(res);
    return jsonDecode(res.body) as List<dynamic>;
  }

  Future<Map<String, dynamic>> getJob(String jobId) async {
    final res = await http.get(
      Uri.parse('$baseUrl/api/v1/jobs/$jobId'),
      headers: _authHeaders(),
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
    String message = res.body;
    try {
      final body = jsonDecode(res.body);
      if (body is Map && body['message'] != null) {
        message = body['message'].toString();
      }
    } catch (_) {}
    throw ApiException(res.statusCode, message);
  }
}

class ApiException implements Exception {
  ApiException(this.statusCode, this.message);
  final int statusCode;
  final String message;

  @override
  String toString() => 'ApiException($statusCode): $message';
}
