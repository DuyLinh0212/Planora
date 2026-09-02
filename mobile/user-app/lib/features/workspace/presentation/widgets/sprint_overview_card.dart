import 'package:flutter/material.dart';

class SprintOverviewCard extends StatelessWidget {
  const SprintOverviewCard({super.key});
  @override
  Widget build(BuildContext context) => Container(
    decoration: BoxDecoration(
      color: const Color(0xff121a2d),
      borderRadius: BorderRadius.circular(18),
      boxShadow: const [
        BoxShadow(
          color: Color(0x24121a2d),
          blurRadius: 22,
          offset: Offset(0, 10),
        ),
      ],
    ),
    padding: const EdgeInsets.all(20),
    child: const Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Text(
              'SPRINT 04',
              style: TextStyle(
                color: Color(0xff9ba7c3),
                letterSpacing: 1.5,
                fontSize: 9,
                fontWeight: FontWeight.w800,
              ),
            ),
            Spacer(),
            Text(
              'CÒN 6 NGÀY',
              style: TextStyle(color: Color(0xff9ba7c3), fontSize: 9),
            ),
          ],
        ),
        SizedBox(height: 8),
        Text(
          'Product foundation',
          style: TextStyle(
            color: Colors.white,
            fontFamily: 'serif',
            fontSize: 22,
          ),
        ),
        SizedBox(height: 20),
        ClipRRect(
          borderRadius: BorderRadius.all(Radius.circular(5)),
          child: LinearProgressIndicator(
            value: .68,
            minHeight: 7,
            backgroundColor: Color(0xff303a51),
            color: Color(0xff6f8aff),
          ),
        ),
        SizedBox(height: 11),
        Row(
          children: [
            Text(
              '21 / 32 công việc',
              style: TextStyle(color: Color(0xffb2bbcf), fontSize: 10),
            ),
            Spacer(),
            Text(
              '68%',
              style: TextStyle(
                color: Colors.white,
                fontWeight: FontWeight.w800,
                fontSize: 11,
              ),
            ),
          ],
        ),
        SizedBox(height: 19),
        Row(
          children: [
            _FlowStep(number: '01', label: 'Đang làm'),
            Expanded(child: Divider(color: Color(0xff3e4860))),
            _FlowStep(number: '02', label: 'Chờ duyệt'),
            Expanded(child: Divider(color: Color(0xff3e4860))),
            _FlowStep(number: '03', label: 'Hoàn tất'),
          ],
        ),
      ],
    ),
  );
}

class _FlowStep extends StatelessWidget {
  const _FlowStep({required this.number, required this.label});
  final String number, label;
  @override
  Widget build(BuildContext context) => Column(
    children: [
      Container(
        width: 26,
        height: 26,
        alignment: Alignment.center,
        decoration: BoxDecoration(
          border: Border.all(color: const Color(0xff5e6b87)),
          shape: BoxShape.circle,
        ),
        child: Text(
          number,
          style: const TextStyle(color: Color(0xffbcc5d8), fontSize: 8),
        ),
      ),
      const SizedBox(height: 5),
      Text(
        label,
        style: const TextStyle(color: Color(0xff929db5), fontSize: 8),
      ),
    ],
  );
}
