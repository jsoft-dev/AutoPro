import 'package:flutter/material.dart';

import 'core/app_theme.dart';
import 'core/auth_session.dart';
import 'features/auth/login_screen.dart';
import 'features/home/home_screen.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  final session = AuthSession();
  await session.load();
  runApp(AutoProApp(session: session));
}

class AutoProApp extends StatelessWidget {
  const AutoProApp({super.key, required this.session});

  final AuthSession session;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'AutoPro CR',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light(),
      darkTheme: AppTheme.dark(),
      themeMode: ThemeMode.system,
      home: session.isLoggedIn
          ? HomeScreen(session: session)
          : LoginScreen(session: session),
    );
  }
}
