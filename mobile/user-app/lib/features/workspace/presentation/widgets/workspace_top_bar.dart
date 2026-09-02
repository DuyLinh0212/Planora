import 'package:flutter/material.dart';

class WorkspaceTopBar extends StatelessWidget {
  const WorkspaceTopBar({super.key});
  @override
  Widget build(BuildContext context) => Row(
    children: [
      Container(
        width: 36,
        height: 36,
        decoration: BoxDecoration(
          color: const Color(0xff121a2d),
          borderRadius: BorderRadius.circular(11),
        ),
        alignment: Alignment.center,
        child: const Text(
          'P',
          style: TextStyle(
            color: Colors.white,
            fontFamily: 'serif',
            fontSize: 21,
            fontStyle: FontStyle.italic,
          ),
        ),
      ),
      const SizedBox(width: 11),
      const Expanded(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Atlas launch',
              style: TextStyle(fontWeight: FontWeight.w800, fontSize: 14),
            ),
            Text(
              'Product team',
              style: TextStyle(color: Color(0xff7b8499), fontSize: 10),
            ),
          ],
        ),
      ),
      IconButton(
        onPressed: () {},
        icon: const Badge(
          smallSize: 7,
          backgroundColor: Color(0xfff47b6b),
          child: Icon(Icons.notifications_none_rounded),
        ),
      ),
      const CircleAvatar(
        radius: 18,
        backgroundColor: Color(0xffffe4dc),
        child: Text(
          'DL',
          style: TextStyle(
            color: Color(0xffa64c3d),
            fontWeight: FontWeight.w700,
            fontSize: 10,
          ),
        ),
      ),
    ],
  );
}
