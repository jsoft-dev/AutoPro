/// Default API base URL for local development.
/// Android emulator: use http://10.0.2.2:5080
/// iOS simulator / desktop: http://localhost:5080
/// Physical device: http://<your-pc-lan-ip>:5080
class ApiConfig {
  static const String defaultBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5080',
  );

  static const String qrPrefix = 'autopro://v/';
}
