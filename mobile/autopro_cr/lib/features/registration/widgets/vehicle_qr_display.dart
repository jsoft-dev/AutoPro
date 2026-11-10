import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:qr_flutter/qr_flutter.dart';

class VehicleQrDisplay extends StatelessWidget {
  const VehicleQrDisplay({
    super.key,
    required this.qrPayload,
    this.plateNumber,
  });

  final String qrPayload;
  final String? plateNumber;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          children: [
            if (plateNumber != null) ...[
              Text(
                plateNumber!,
                style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 4),
              Text(
                'Vehicle QR for future visits',
                style: theme.textTheme.bodySmall?.copyWith(color: scheme.onSurfaceVariant),
              ),
              const SizedBox(height: 20),
            ],
            Material(
              color: Colors.white,
              borderRadius: BorderRadius.circular(12),
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: QrImageView(
                  data: qrPayload,
                  size: 200,
                  backgroundColor: Colors.white,
                ),
              ),
            ),
            const SizedBox(height: 16),
            SelectableText(
              qrPayload,
              textAlign: TextAlign.center,
              style: theme.textTheme.bodySmall,
            ),
            const SizedBox(height: 12),
            TextButton.icon(
              onPressed: () {
                Clipboard.setData(ClipboardData(text: qrPayload));
                ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(content: Text('QR payload copied')),
                );
              },
              icon: const Icon(Icons.copy_outlined),
              label: const Text('Copy payload'),
            ),
          ],
        ),
      ),
    );
  }
}
