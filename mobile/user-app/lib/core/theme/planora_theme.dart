import 'package:flutter/material.dart';

abstract final class PlanoraColors {
  static const nightInk = Color(0xff121a2d);
  static const draftBlue = Color(0xff3155c6);
  static const porcelain = Color(0xfff5f7fb);
  static const quietMist = Color(0xffdde8ff);
  static const lagoon = Color(0xff36a6a0);
  static const signalCoral = Color(0xfff47b6b);
}

abstract final class PlanoraTheme {
  static ThemeData get light => ThemeData(
    useMaterial3: true,
    scaffoldBackgroundColor: PlanoraColors.porcelain,
    colorScheme: ColorScheme.fromSeed(seedColor: PlanoraColors.draftBlue),
  );
}
