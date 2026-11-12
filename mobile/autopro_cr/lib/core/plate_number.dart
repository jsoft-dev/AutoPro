class PlateNumberParts {
  const PlateNumberParts({required this.prefix, required this.suffix});

  final String prefix;
  final String suffix;

  String get formatted => '${prefix.toUpperCase()}-${suffix.trim()}';

  static PlateNumberParts? tryParse(String? input) {
    if (input == null) return null;
    final compact = input.trim().toUpperCase().replaceAll(' ', '');
    final dash = compact.indexOf('-');
    if (dash <= 0 || dash >= compact.length - 1) return null;
    final prefix = compact.substring(0, dash);
    final suffix = compact.substring(dash + 1);
    if (!isValidPrefix(prefix) || !isValidSuffix(suffix)) return null;
    return PlateNumberParts(prefix: prefix, suffix: suffix);
  }

  static bool isValidPrefix(String value) {
    final p = value.trim().toUpperCase();
    return RegExp(r'^[A-Z]{1,7}$').hasMatch(p);
  }

  static bool isValidSuffix(String value) {
    final s = value.trim();
    return RegExp(r'^[0-9]{1,7}$').hasMatch(s);
  }

  static bool isValidPair(String prefix, String suffix) =>
      isValidPrefix(prefix) && isValidSuffix(suffix);

  static String? validationMessage(String prefix, String suffix) {
    if (prefix.trim().isEmpty || suffix.trim().isEmpty) {
      return 'Enter both plate letters and numbers.';
    }
    if (!isValidPrefix(prefix)) {
      return 'Plate letters must be A–Z only (e.g. CAP, VO).';
    }
    if (!isValidSuffix(suffix)) {
      return 'Plate numbers must be digits only (e.g. 1234).';
    }
    return null;
  }
}
