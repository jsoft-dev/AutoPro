import 'package:flutter/material.dart';

/// Brand mark used on login and other prominent surfaces.
class AutoProLogo extends StatelessWidget {
  const AutoProLogo({super.key, this.height = 120});

  final double height;

  @override
  Widget build(BuildContext context) {
    return Image.asset(
      'assets/images/autopro_logo.png',
      height: height,
      fit: BoxFit.contain,
      semanticLabel: 'AutoPro by Jsoft',
    );
  }
}
