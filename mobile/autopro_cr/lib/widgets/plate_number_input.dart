import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

/// Forces A–Z as the user types (Sri Lanka plate prefix).
class UpperCaseLettersFormatter extends TextInputFormatter {
  @override
  TextEditingValue formatEditUpdate(TextEditingValue oldValue, TextEditingValue newValue) {
    final upper = newValue.text.toUpperCase();
    if (upper == newValue.text) return newValue;
    return TextEditingValue(
      text: upper,
      selection: newValue.selection.copyWith(
        baseOffset: _mapOffset(newValue.text, upper, newValue.selection.baseOffset),
        extentOffset: _mapOffset(newValue.text, upper, newValue.selection.extentOffset),
      ),
      composing: TextRange.empty,
    );
  }

  int _mapOffset(String source, String target, int offset) {
    offset = offset.clamp(0, source.length);
    return offset.clamp(0, target.length);
  }
}

/// Sri Lanka–style plate entry: prefix letters + numeric suffix (CAP-1234).
class PlateNumberInput extends StatefulWidget {
  const PlateNumberInput({
    super.key,
    required this.prefixController,
    required this.suffixController,
    this.enabled = true,
  });

  final TextEditingController prefixController;
  final TextEditingController suffixController;
  final bool enabled;

  @override
  State<PlateNumberInput> createState() => _PlateNumberInputState();
}

class _PlateNumberInputState extends State<PlateNumberInput> {
  final _suffixFocus = FocusNode();

  @override
  void initState() {
    super.initState();
    _ensurePrefixUpperCase();
    widget.prefixController.addListener(_ensurePrefixUpperCase);
  }

  @override
  void dispose() {
    widget.prefixController.removeListener(_ensurePrefixUpperCase);
    _suffixFocus.dispose();
    super.dispose();
  }

  void _ensurePrefixUpperCase() {
    final c = widget.prefixController;
    final upper = c.text.toUpperCase();
    if (c.text == upper) return;
    final offset = c.selection.baseOffset.clamp(0, upper.length);
    c.value = TextEditingValue(
      text: upper,
      selection: TextSelection.collapsed(offset: offset),
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          'Registration number',
          style: theme.textTheme.labelLarge,
        ),
        const SizedBox(height: 8),
        Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(
              flex: 2,
              child: TextField(
                controller: widget.prefixController,
                enabled: widget.enabled,
                textCapitalization: TextCapitalization.characters,
                autocorrect: false,
                inputFormatters: [
                  UpperCaseLettersFormatter(),
                  FilteringTextInputFormatter.allow(RegExp(r'[A-Za-z]')),
                  LengthLimitingTextInputFormatter(7),
                ],
                decoration: const InputDecoration(
                  hintText: 'CAP',
                  prefixIcon: Icon(Icons.pin_outlined),
                ),
                textInputAction: TextInputAction.next,
                onSubmitted: (_) => _suffixFocus.requestFocus(),
              ),
            ),
            Padding(
              padding: const EdgeInsets.only(top: 14, left: 8, right: 8),
              child: Text(
                '–',
                style: theme.textTheme.headlineSmall?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
            ),
            Expanded(
              flex: 3,
              child: TextField(
                controller: widget.suffixController,
                focusNode: _suffixFocus,
                enabled: widget.enabled,
                keyboardType: TextInputType.number,
                inputFormatters: [
                  FilteringTextInputFormatter.digitsOnly,
                  LengthLimitingTextInputFormatter(7),
                ],
                decoration: const InputDecoration(
                  hintText: '1234',
                ),
                textInputAction: TextInputAction.done,
              ),
            ),
          ],
        ),
      ],
    );
  }
}
