# ML Kit / mobile_scanner (required for release APK — R8 strips these otherwise)
-keep class com.google.mlkit.** { *; }
-keep class com.google.android.libraries.barhopper.** { *; }
-keep class com.google.mlkit.vision.barcode.** { *; }

# mobile_scanner plugin rules
-keep class com.google.photos.** { *; }
