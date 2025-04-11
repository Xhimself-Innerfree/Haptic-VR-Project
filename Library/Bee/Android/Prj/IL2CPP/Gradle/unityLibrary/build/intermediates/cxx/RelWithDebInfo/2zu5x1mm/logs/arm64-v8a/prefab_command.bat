@echo off
"D:\\Unity Editor\\6000.0.45f1\\Editor\\Data\\PlaybackEngines\\AndroidPlayer\\OpenJDK\\bin\\java" ^
  --class-path ^
  "C:\\Users\\xhims\\.gradle\\caches\\modules-2\\files-2.1\\com.google.prefab\\cli\\2.1.0\\aa32fec809c44fa531f01dcfb739b5b3304d3050\\cli-2.1.0-all.jar" ^
  com.google.prefab.cli.AppKt ^
  --build-system ^
  cmake ^
  --platform ^
  android ^
  --abi ^
  arm64-v8a ^
  --os-version ^
  23 ^
  --stl ^
  c++_shared ^
  --ndk-version ^
  27 ^
  --output ^
  "C:\\Users\\xhims\\AppData\\Local\\Temp\\agp-prefab-staging16142443389020365740\\staged-cli-output" ^
  "C:\\Users\\xhims\\.gradle\\caches\\8.11\\transforms\\cf11df13d2fd9a509170edfac64a04e6\\transformed\\jetified-games-activity-3.0.5\\prefab"
