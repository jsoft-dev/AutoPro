import 'package:flutter/material.dart';

enum NewJobMethod { scanQr, findByPlate, registerVehicle }

class NewJobSheet extends StatelessWidget {
  const NewJobSheet({super.key});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    return SingleChildScrollView(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 4, 20, 24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('Start a job', style: theme.textTheme.titleLarge),
            const SizedBox(height: 4),
            Text(
              'Identify the vehicle, then capture odometer and notes.',
              style: theme.textTheme.bodyMedium?.copyWith(color: scheme.onSurfaceVariant),
            ),
            const SizedBox(height: 16),
            ListTile(
              leading: CircleAvatar(
                backgroundColor: scheme.primaryContainer,
                child: Icon(Icons.qr_code_scanner, color: scheme.onPrimaryContainer),
              ),
              title: const Text('Scan vehicle QR'),
              subtitle: const Text('Preferred when the sticker is available'),
              trailing: const Icon(Icons.chevron_right),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              tileColor: scheme.surfaceContainerLow,
              onTap: () => Navigator.pop(context, NewJobMethod.scanQr),
            ),
            const SizedBox(height: 8),
            ListTile(
              leading: CircleAvatar(
                backgroundColor: scheme.secondaryContainer,
                child: Icon(Icons.pin_outlined, color: scheme.onSecondaryContainer),
              ),
              title: const Text('Registration number'),
              subtitle: const Text('Fallback when QR is missing or unreadable'),
              trailing: const Icon(Icons.chevron_right),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              tileColor: scheme.surfaceContainerLow,
              onTap: () => Navigator.pop(context, NewJobMethod.findByPlate),
            ),
            const SizedBox(height: 8),
            ListTile(
              leading: CircleAvatar(
                backgroundColor: scheme.tertiaryContainer,
                child: Icon(Icons.person_add_alt_1_outlined, color: scheme.onTertiaryContainer),
              ),
              title: const Text('Register customer & vehicle'),
              subtitle: const Text('New walk-in — creates plate record and QR'),
              trailing: const Icon(Icons.chevron_right),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              tileColor: scheme.surfaceContainerLow,
              onTap: () => Navigator.pop(context, NewJobMethod.registerVehicle),
            ),
          ],
        ),
      ),
    );
  }
}
