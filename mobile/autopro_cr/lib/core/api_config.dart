/// API base URL for the CR mobile app (no trailing slash).
///
/// Override at build/run time:
/// `flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5080`
///
/// | Target | Typical `API_BASE_URL` |
/// |---|---|
/// | Production / physical phone | `https://autopro-api-qciu.onrender.com` |
/// | Android emulator + local API | `http://10.0.2.2:5080` |
/// | Windows / iOS simulator | `http://localhost:5080` |
class ApiConfig {
  static const String defaultBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'https://autopro-api-qciu.onrender.com',
  );

  /// Normalizes [defaultBaseUrl] (strips trailing slashes).
  static String get baseUrl {
    final trimmed = defaultBaseUrl.trim();
    if (trimmed.isEmpty) return trimmed;
    return trimmed.replaceAll(RegExp(r'/+$'), '');
  }

  static const String qrPrefix = 'autopro://v/';
}
