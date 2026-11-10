import 'package:shared_preferences/shared_preferences.dart';

class AuthSession {
  static const _tokenKey = 'access_token';
  static const _displayNameKey = 'display_name';

  String? accessToken;
  String? displayName;

  Future<void> load() async {
    final prefs = await SharedPreferences.getInstance();
    accessToken = prefs.getString(_tokenKey);
    displayName = prefs.getString(_displayNameKey);
  }

  Future<void> save(String token, String name) async {
    accessToken = token;
    displayName = name;
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_tokenKey, token);
    await prefs.setString(_displayNameKey, name);
  }

  Future<void> clear() async {
    accessToken = null;
    displayName = null;
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_tokenKey);
    await prefs.remove(_displayNameKey);
  }

  bool get isLoggedIn => accessToken != null && accessToken!.isNotEmpty;
}
