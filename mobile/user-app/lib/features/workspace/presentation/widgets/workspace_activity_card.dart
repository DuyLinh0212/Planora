import 'package:flutter/material.dart';

class WorkspaceActivityCard extends StatelessWidget {
  const WorkspaceActivityCard({super.key});
  @override
  Widget build(BuildContext context) => Container(
    decoration: BoxDecoration(
      color: Colors.white,
      borderRadius: BorderRadius.circular(15),
      border: Border.all(color: const Color(0xffe3e7ef)),
    ),
    child: const Column(
      children: [
        _Activity(
          initials: 'HA',
          color: Color(0xffdcf3ef),
          textColor: Color(0xff237a71),
          name: 'Hà Anh',
          action: 'đã nộp Mobile navigation',
          time: '12 phút trước',
        ),
        Divider(height: 1, indent: 62, color: Color(0xffedf0f5)),
        _Activity(
          initials: 'MK',
          color: Color(0xffe2e8ff),
          textColor: Color(0xff3453ab),
          name: 'Minh Khoa',
          action: 'đã duyệt Database schema',
          time: '48 phút trước',
        ),
      ],
    ),
  );
}

class _Activity extends StatelessWidget {
  const _Activity({
    required this.initials,
    required this.color,
    required this.textColor,
    required this.name,
    required this.action,
    required this.time,
  });
  final String initials, name, action, time;
  final Color color, textColor;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.all(15),
    child: Row(
      children: [
        CircleAvatar(
          radius: 17,
          backgroundColor: color,
          child: Text(
            initials,
            style: TextStyle(
              color: textColor,
              fontWeight: FontWeight.w800,
              fontSize: 9,
            ),
          ),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text.rich(
                TextSpan(
                  style: const TextStyle(
                    fontSize: 10,
                    color: Color(0xff687186),
                  ),
                  children: [
                    TextSpan(
                      text: name,
                      style: const TextStyle(
                        fontWeight: FontWeight.w800,
                        color: Color(0xff121a2d),
                      ),
                    ),
                    TextSpan(text: ' $action'),
                  ],
                ),
              ),
              const SizedBox(height: 4),
              Text(
                time,
                style: const TextStyle(color: Color(0xff9aa1af), fontSize: 8),
              ),
            ],
          ),
        ),
      ],
    ),
  );
}
