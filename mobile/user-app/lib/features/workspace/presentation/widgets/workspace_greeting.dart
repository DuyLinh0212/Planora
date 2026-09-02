import 'package:flutter/material.dart';

class WorkspaceGreeting extends StatelessWidget {
  const WorkspaceGreeting({super.key});
  @override
  Widget build(BuildContext context) => const Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Text(
        'THỨ BẢY · 29 THÁNG 8',
        style: TextStyle(
          color: Color(0xff8b93a5),
          letterSpacing: 1.5,
          fontWeight: FontWeight.w800,
          fontSize: 9,
        ),
      ),
      SizedBox(height: 8),
      Text(
        'Chào buổi chiều, Linh.',
        style: TextStyle(
          fontFamily: 'serif',
          fontSize: 31,
          letterSpacing: -0.8,
          color: Color(0xff121a2d),
        ),
      ),
      SizedBox(height: 8),
      Text(
        'Bạn có 3 việc cần chú ý hôm nay.',
        style: TextStyle(color: Color(0xff6f788d), fontSize: 12),
      ),
    ],
  );
}
