import 'package:flutter/material.dart';

class WorkspaceTaskCard extends StatelessWidget {
  const WorkspaceTaskCard({
    super.key,
    required this.code,
    required this.title,
    required this.status,
    required this.color,
    required this.due,
    required this.people,
  });
  final String code, title, status, due, people;
  final Color color;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.all(17),
    decoration: BoxDecoration(
      color: Colors.white,
      borderRadius: BorderRadius.circular(15),
      border: Border.all(color: const Color(0xffe3e7ef)),
    ),
    child: Row(
      children: [
        Container(
          width: 4,
          height: 72,
          decoration: BoxDecoration(
            color: color,
            borderRadius: BorderRadius.circular(4),
          ),
        ),
        const SizedBox(width: 14),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                '$code  ·  $status',
                style: TextStyle(
                  color: color,
                  letterSpacing: .7,
                  fontWeight: FontWeight.w800,
                  fontSize: 8,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                title,
                style: const TextStyle(
                  fontSize: 13,
                  color: Color(0xff121a2d),
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 13),
              Row(
                children: [
                  const Icon(
                    Icons.schedule_rounded,
                    size: 13,
                    color: Color(0xff8992a5),
                  ),
                  const SizedBox(width: 4),
                  Text(
                    due,
                    style: const TextStyle(
                      color: Color(0xff778095),
                      fontSize: 9,
                    ),
                  ),
                  const Spacer(),
                  Text(
                    people,
                    style: const TextStyle(
                      color: Color(0xff53607a),
                      fontWeight: FontWeight.w700,
                      fontSize: 9,
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
        const SizedBox(width: 8),
        const Icon(Icons.chevron_right_rounded, color: Color(0xffa1a8b7)),
      ],
    ),
  );
}
