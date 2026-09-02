import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:planora_mobile/app/planora_app.dart';

void main() {
  testWidgets('renders the primary workspace navigation', (tester) async {
    await tester.pumpWidget(const PlanoraApp());

    expect(find.byType(NavigationBar), findsOneWidget);
    expect(find.text('Tổng quan'), findsOneWidget);
    expect(find.text('Công việc'), findsOneWidget);
  });
}
