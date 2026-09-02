import 'package:flutter/material.dart';

class WorkspaceSectionTitle extends StatelessWidget {
  const WorkspaceSectionTitle({
    super.key,
    required this.eyebrow,
    required this.title,
  });
  final String eyebrow, title;
  @override
  Widget build(BuildContext context) => Row(
    crossAxisAlignment: CrossAxisAlignment.end,
    children: [
      Expanded(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              eyebrow,
              style: const TextStyle(
                color: Color(0xff8b93a5),
                letterSpacing: 1.4,
                fontSize: 8,
                fontWeight: FontWeight.w800,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              title,
              style: const TextStyle(
                fontFamily: 'serif',
                fontSize: 23,
                color: Color(0xff121a2d),
              ),
            ),
          ],
        ),
      ),
      const Text(
        'Xem tất cả  →',
        style: TextStyle(
          color: Color(0xff3155c6),
          fontWeight: FontWeight.w700,
          fontSize: 10,
        ),
      ),
    ],
  );
}
