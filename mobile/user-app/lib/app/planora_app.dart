import 'package:flutter/material.dart';
import 'package:planora_mobile/core/theme/planora_theme.dart';
import 'package:planora_mobile/features/workspace/presentation/workspace_screen.dart';

class PlanoraApp extends StatelessWidget {
  const PlanoraApp({super.key});

  @override
  Widget build(BuildContext context) => MaterialApp(
    debugShowCheckedModeBanner: false,
    title: 'Planora',
    theme: PlanoraTheme.light,
    home: const WorkspaceScreen(),
  );
}
