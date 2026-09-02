import 'package:flutter/material.dart';
import 'package:planora_mobile/core/theme/planora_theme.dart';
import 'package:planora_mobile/features/workspace/presentation/widgets/sprint_overview_card.dart';
import 'package:planora_mobile/features/workspace/presentation/widgets/workspace_activity_card.dart';
import 'package:planora_mobile/features/workspace/presentation/widgets/workspace_greeting.dart';
import 'package:planora_mobile/features/workspace/presentation/widgets/workspace_section_title.dart';
import 'package:planora_mobile/features/workspace/presentation/widgets/workspace_task_card.dart';
import 'package:planora_mobile/features/workspace/presentation/widgets/workspace_top_bar.dart';

class WorkspaceScreen extends StatefulWidget {
  const WorkspaceScreen({super.key});

  @override
  State<WorkspaceScreen> createState() => _WorkspaceScreenState();
}

class _WorkspaceScreenState extends State<WorkspaceScreen> {
  int selectedIndex = 0;

  @override
  Widget build(BuildContext context) => Scaffold(
    body: selectedIndex == 0 ? SafeArea(
      child: CustomScrollView(
        slivers: [
          SliverPadding(
            padding: const EdgeInsets.fromLTRB(20, 14, 20, 110),
            sliver: SliverList.list(
              children: const [
                WorkspaceTopBar(),
                SizedBox(height: 30),
                WorkspaceGreeting(),
                SizedBox(height: 22),
                SprintOverviewCard(),
                SizedBox(height: 29),
                WorkspaceSectionTitle(
                  eyebrow: 'ƯU TIÊN',
                  title: 'Cần bạn chú ý',
                ),
                SizedBox(height: 13),
                WorkspaceTaskCard(
                  code: 'AUTH-18',
                  title: 'Hoàn thiện luồng đăng ký',
                  status: 'ĐANG LÀM',
                  color: PlanoraColors.draftBlue,
                  due: 'Hôm nay',
                  people: 'LN  HA',
                ),
                SizedBox(height: 11),
                WorkspaceTaskCard(
                  code: 'MOB-07',
                  title: 'Kiểm tra mobile navigation',
                  status: 'CHỜ DUYỆT',
                  color: Color(0xffd86653),
                  due: '30/08',
                  people: 'NT',
                ),
                SizedBox(height: 29),
                WorkspaceSectionTitle(
                  eyebrow: 'HOẠT ĐỘNG',
                  title: 'Mới diễn ra',
                ),
                SizedBox(height: 13),
                WorkspaceActivityCard(),
              ],
            ),
          ),
        ],
      ),
    ) : SafeArea(child: IndexedStack(index: selectedIndex - 1, children: const [_MobileTasksView(), _MobileFilesView(), _MobileProfileView()])),
    floatingActionButton: FloatingActionButton(
      backgroundColor: PlanoraColors.nightInk,
      foregroundColor: Colors.white,
      elevation: 5,
      onPressed: () {},
      child: const Icon(Icons.add_rounded),
    ),
    bottomNavigationBar: NavigationBar(
      height: 72,
      selectedIndex: selectedIndex,
      indicatorColor: PlanoraColors.quietMist,
      onDestinationSelected: (value) => setState(() => selectedIndex = value),
      destinations: const [
        NavigationDestination(
          icon: Icon(Icons.grid_view_rounded),
          label: 'Tổng quan',
        ),
        NavigationDestination(
          icon: Icon(Icons.checklist_rounded),
          label: 'Công việc',
        ),
        NavigationDestination(
          icon: Icon(Icons.folder_outlined),
          label: 'Kho tệp',
        ),
        NavigationDestination(
          icon: Icon(Icons.person_outline_rounded),
          label: 'Cá nhân',
        ),
      ],
    ),
  );
}

class _MobilePageHeader extends StatelessWidget {
  const _MobilePageHeader({required this.eyebrow, required this.title, required this.description});
  final String eyebrow, title, description;
  @override
  Widget build(BuildContext context) => Padding(padding: const EdgeInsets.fromLTRB(20, 22, 20, 16), child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [const WorkspaceTopBar(), const SizedBox(height: 28), Text(eyebrow, style: const TextStyle(color: PlanoraColors.draftBlue, letterSpacing: 1.4, fontWeight: FontWeight.w800, fontSize: 9)), const SizedBox(height: 7), Text(title, style: const TextStyle(fontFamily: 'serif', fontSize: 31, color: PlanoraColors.nightInk)), const SizedBox(height: 5), Text(description, style: const TextStyle(color: Color(0xff6f788d), fontSize: 11, height: 1.5))]));
}

class _MobileTasksView extends StatelessWidget {
  const _MobileTasksView();
  @override
  Widget build(BuildContext context) => ListView(padding: const EdgeInsets.only(bottom: 110), children: const [_MobilePageHeader(eyebrow: 'COMMITMENT HORIZON', title: 'Công việc', description: 'Tập trung vào việc gần hạn, phụ thuộc và mốc bàn giao.'), Padding(padding: EdgeInsets.symmetric(horizontal: 20), child: Row(children: [Expanded(child: _FilterChip(label: 'Đang làm', selected: true)), SizedBox(width: 8), Expanded(child: _FilterChip(label: 'Chờ duyệt')), SizedBox(width: 8), Expanded(child: _FilterChip(label: 'Quá hạn'))])), SizedBox(height: 14), Padding(padding: EdgeInsets.symmetric(horizontal: 20), child: WorkspaceTaskCard(code: 'AUTH-18', title: 'Hoàn thiện luồng đăng ký', status: 'ĐANG LÀM', color: PlanoraColors.draftBlue, due: 'Hôm nay', people: 'LN  HA')), SizedBox(height: 11), Padding(padding: EdgeInsets.symmetric(horizontal: 20), child: WorkspaceTaskCard(code: 'API-24', title: 'Kiểm tra điều kiện phụ thuộc', status: 'BỊ CHẶN', color: PlanoraColors.signalCoral, due: 'Ngày mai', people: 'DL'))]);
}

class _FilterChip extends StatelessWidget {
  const _FilterChip({required this.label, this.selected = false}); final String label; final bool selected;
  @override Widget build(BuildContext context) => Container(height: 36, alignment: Alignment.center, decoration: BoxDecoration(color: selected ? PlanoraColors.draftBlue : Colors.white, border: Border.all(color: selected ? PlanoraColors.draftBlue : const Color(0xffdce3ee)), borderRadius: BorderRadius.circular(9)), child: Text(label, style: TextStyle(color: selected ? Colors.white : const Color(0xff687186), fontWeight: FontWeight.w700, fontSize: 9)));
}

class _MobileFilesView extends StatelessWidget {
  const _MobileFilesView();
  @override Widget build(BuildContext context) => ListView(padding: const EdgeInsets.only(bottom: 110), children: [_MobilePageHeader(eyebrow: 'VERSIONED STORAGE', title: 'Kho tệp', description: 'Tệp, tài liệu sống và lịch sử phiên bản trong cùng project.'), Padding(padding: const EdgeInsets.symmetric(horizontal: 20), child: Container(padding: const EdgeInsets.all(16), decoration: BoxDecoration(color: Colors.white, border: Border.all(color: const Color(0xffdce3ee)), borderRadius: BorderRadius.circular(15)), child: const Column(children: [_FileRow(icon: Icons.folder_rounded, name: 'Tài liệu dự án', meta: '12 mục'), Divider(height: 28), _FileRow(icon: Icons.description_outlined, name: 'Product brief.md', meta: 'v4 · vừa cập nhật'), Divider(height: 28), _FileRow(icon: Icons.picture_as_pdf_outlined, name: 'Research.pdf', meta: '2.4 MB · v2')])))]);
}

class _FileRow extends StatelessWidget {
  const _FileRow({required this.icon, required this.name, required this.meta}); final IconData icon; final String name, meta;
  @override Widget build(BuildContext context) => Row(children: [Container(width: 38, height: 38, decoration: BoxDecoration(color: PlanoraColors.quietMist, borderRadius: BorderRadius.circular(9)), child: Icon(icon, size: 19, color: PlanoraColors.draftBlue)), const SizedBox(width: 12), Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [Text(name, style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 11)), const SizedBox(height: 3), Text(meta, style: const TextStyle(color: Color(0xff8790a2), fontSize: 9))])), const Icon(Icons.chevron_right_rounded, color: Color(0xffa1a8b7))]);
}

class _MobileProfileView extends StatelessWidget {
  const _MobileProfileView();
  @override Widget build(BuildContext context) => ListView(padding: const EdgeInsets.only(bottom: 110), children: [_MobilePageHeader(eyebrow: 'PERSONAL WORKSPACE', title: 'Cá nhân', description: 'Hồ sơ, hạn mức và tùy chọn trải nghiệm.'), Padding(padding: const EdgeInsets.symmetric(horizontal: 20), child: Column(children: [Container(padding: const EdgeInsets.all(18), decoration: BoxDecoration(color: Colors.white, border: Border.all(color: const Color(0xffdce3ee)), borderRadius: BorderRadius.circular(15)), child: const Row(children: [CircleAvatar(radius: 28, backgroundColor: PlanoraColors.quietMist, child: Text('DL', style: TextStyle(color: PlanoraColors.draftBlue, fontWeight: FontWeight.w800))), SizedBox(width: 13), Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [Text('Duy Linh', style: TextStyle(fontFamily: 'serif', fontSize: 20)), Text('@duylinh · Free', style: TextStyle(color: Color(0xff7b8499), fontSize: 9))])), Icon(Icons.edit_outlined, size: 18)])), const SizedBox(height: 12), const _SettingRow(icon: Icons.palette_outlined, label: 'Giao diện', value: 'Calm'), const _SettingRow(icon: Icons.language_rounded, label: 'Ngôn ngữ', value: 'Tiếng Việt'), const _SettingRow(icon: Icons.workspace_premium_outlined, label: 'Gói & hạn mức', value: 'Free'), const _SettingRow(icon: Icons.help_outline_rounded, label: 'Hỗ trợ', value: '')]))]);
}

class _SettingRow extends StatelessWidget {
  const _SettingRow({required this.icon, required this.label, required this.value}); final IconData icon; final String label, value;
  @override Widget build(BuildContext context) => Container(margin: const EdgeInsets.only(bottom: 8), padding: const EdgeInsets.symmetric(horizontal: 14), height: 54, decoration: BoxDecoration(color: Colors.white, border: Border.all(color: const Color(0xffdce3ee)), borderRadius: BorderRadius.circular(12)), child: Row(children: [Icon(icon, size: 18, color: PlanoraColors.draftBlue), const SizedBox(width: 11), Expanded(child: Text(label, style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 11))), Text(value, style: const TextStyle(color: Color(0xff7b8499), fontSize: 9)), const SizedBox(width: 5), const Icon(Icons.chevron_right_rounded, size: 17, color: Color(0xffa1a8b7))]));
}
