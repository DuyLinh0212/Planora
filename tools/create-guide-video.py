from __future__ import annotations

import asyncio
import json
import re
import subprocess
import time
from pathlib import Path
from typing import Callable
from urllib.parse import urlparse

import edge_tts
import imageio_ffmpeg
from playwright.sync_api import Page, Route, sync_playwright


WORKSPACE = Path(__file__).resolve().parents[1]
BASE_URL = "http://127.0.0.1:4200"
MEDIA_DIRECTORY = WORKSPACE / "frontend" / "Planora.Web.User" / "public" / "media"
BUILD_DIRECTORY = WORKSPACE / "outputs" / "guide-video-build"
FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()

PROJECT_ID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
SPRINT_ID = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"

NARRATION = [
    (
        "Tổng quan",
        "Chào bạn! Trong ít phút tới, mình sẽ cùng bạn đi qua quy trình chính trên Planora. "
        "Đầu tiên là tạo project; sau đó lập sprint, giao công việc, rồi nộp và duyệt kết quả.",
    ),
    (
        "Tạo project",
        "Từ danh sách dự án, chọn Tạo dự án. Bạn nhập tên, một mô tả ngắn, rồi chọn khoảng thời gian dự kiến. "
        "Mỗi project là một không gian riêng cho công việc, sprint, tệp và các thành viên trong nhóm.",
    ),
    (
        "Lập sprint",
        "Sau khi mở project, chuyển sang mục Sprint. Tại đây, hãy chọn ngày bắt đầu, ngày kết thúc, "
        "và viết một mục tiêu thật rõ ràng. Nhớ kiểm tra để deadline của công việc nằm trong khoảng thời gian này.",
    ),
    (
        "Giao công việc",
        "Tiếp theo, mở bảng Công việc, rồi chọn Tạo công việc. Điền tiêu đề, mức ưu tiên và deadline. "
        "Sau đó, chọn loại kết quả cần nộp và ghi rõ tiêu chí hoàn thành. Thông tin càng cụ thể, việc bàn giao càng dễ kiểm chứng.",
    ),
    (
        "Nộp và duyệt",
        "Khi làm xong, mở thẻ công việc, thêm ghi chú, rồi chọn Nộp duyệt. Người phụ trách có thể duyệt để hoàn tất; "
        "hoặc gửi yêu cầu làm lại. Planora vẫn giữ trạng thái và lịch sử, để cả nhóm dễ dàng theo dõi.",
    ),
    (
        "Hoàn tất",
        "Vậy là bạn đã sẵn sàng! Nếu muốn xem lại, bạn chỉ cần chọn đúng chương ở bên cạnh video, rồi làm theo từng bước.",
    ),
]


PROFILE = {
    "userId": "11111111-1111-1111-1111-111111111111",
    "email": "linh@example.com",
    "username": "linh",
    "displayName": "Duy Linh Nguyễn",
    "avatarUrl": None,
    "preferredLanguage": "vi",
    "themePreference": "calm",
    "timeZoneId": "Asia/Ho_Chi_Minh",
    "participatingProjectCount": 2,
    "quota": {
        "planCode": "PRO",
        "planName": "Pro",
        "ownedProjects": 1,
        "maxOwnedProjects": 10,
        "storageBytes": 2147483648,
        "maxStorageBytes": 21474836480,
        "maxProjectStorageBytes": 5368709120,
        "maxFileSizeBytes": 104857600,
        "dailyUploadBytes": 0,
        "dailyUploadCount": 0,
        "maxMembersPerProject": 25,
        "maxVersionsPerFile": 30,
        "subscriptionExpiresAt": "2027-08-30T00:00:00Z",
        "autoRenew": True,
    },
}

PROJECT = {
    "id": PROJECT_ID,
    "name": "Ra mắt Planora Mobile",
    "description": "Chuẩn bị trải nghiệm quản lý công việc rõ ràng cho đội sản phẩm.",
    "status": "Active",
    "startAt": "2026-08-01T00:00:00Z",
    "endAt": "2026-09-30T00:00:00Z",
    "memberCount": 4,
    "updatedAt": "2026-08-30T12:00:00Z",
}

SPRINT = {
    "id": SPRINT_ID,
    "projectId": PROJECT_ID,
    "name": "Sprint 4 · Hoàn thiện trải nghiệm",
    "goal": "Chốt luồng tạo, giao và duyệt công việc trên web.",
    "startAt": "2026-08-25T00:00:00Z",
    "endAt": "2026-09-07T00:00:00Z",
    "status": "Active",
}

TASKS = [
    {
        "id": f"cccccccc-cccc-cccc-cccc-ccccccccccc{index}",
        "projectId": PROJECT_ID,
        "sprintId": SPRINT_ID,
        "title": title,
        "description": "Công việc có phạm vi và tiêu chí hoàn thành rõ ràng.",
        "priority": priority,
        "status": status,
        "originalDueAt": "2026-09-02T00:00:00Z",
        "effectiveDueAt": "2026-09-02T00:00:00Z",
        "acceptanceCriteria": ["Đáp ứng đúng thiết kế", "Không có lỗi trên màn hình nhỏ"],
        "assigneeMemberIds": [],
        "type": "Feature",
        "submissionRequirement": "Any",
        "allowedExtensions": [],
        "dependsOnTaskId": None,
        "isMilestone": index == 5,
    }
    for index, (title, priority, status) in enumerate(
        [
            ("Chốt luồng đăng nhập", "High", "Done"),
            ("Dựng bảng công việc", "High", "InProgress"),
            ("Kiểm tra giao diện mobile", "Medium", "Submitted"),
            ("Bổ sung trạng thái trống", "Medium", "Rework"),
            ("Hoàn thiện trang tài liệu", "Low", "ToDo"),
            ("Phát hành bản thử nghiệm", "High", "ToDo"),
        ]
    )
]

MEMBERS = [
    {
        "membershipId": "dddddddd-dddd-dddd-dddd-dddddddddddd",
        "userId": PROFILE["userId"],
        "displayName": PROFILE["displayName"],
        "email": PROFILE["email"],
        "status": "Active",
        "roles": ["Owner"],
    }
]


def mock_api(route: Route) -> None:
    path = urlparse(route.request.url).path
    if path == "/api/profile":
        payload = PROFILE
    elif path == "/api/projects":
        payload = {"items": [PROJECT], "totalCount": 1, "page": 1, "pageSize": 50}
    elif path == f"/api/projects/{PROJECT_ID}":
        payload = PROJECT
    elif path == f"/api/projects/{PROJECT_ID}/sprints":
        payload = [SPRINT]
    elif path == f"/api/projects/{PROJECT_ID}/tasks":
        payload = TASKS
    elif path == f"/api/projects/{PROJECT_ID}/members":
        payload = MEMBERS
    elif path == f"/api/projects/{PROJECT_ID}/storage":
        payload = {"folders": [], "files": [], "documents": []}
    elif path == "/api/notifications":
        payload = []
    elif path == "/api/system/maintenance":
        payload = {"isEnabled": False, "message": "", "updatedAt": None}
    else:
        payload = {}
    route.fulfill(status=200, json=payload)


async def create_narration() -> list[Path]:
    segment_paths: list[Path] = []
    for index, (_, text) in enumerate(NARRATION):
        target = BUILD_DIRECTORY / f"narration-natural-v2-{index:02d}.mp3"
        if not target.exists():
            await edge_tts.Communicate(
                text,
                "vi-VN-NamMinhNeural",
                rate="-7%",
                pitch="-3Hz",
            ).save(str(target))
        segment_paths.append(target)
    return segment_paths


def media_duration(path: Path) -> float:
    result = subprocess.run(
        [FFMPEG, "-hide_banner", "-i", str(path)],
        capture_output=True,
        text=True,
        check=False,
    )
    match = re.search(r"Duration: (\d+):(\d+):([\d.]+)", result.stderr)
    if not match:
        raise RuntimeError(f"Không đọc được thời lượng của {path}")
    hours, minutes, seconds = match.groups()
    return int(hours) * 3600 + int(minutes) * 60 + float(seconds)


def format_timestamp(seconds: float) -> str:
    milliseconds = int(round(seconds * 1000))
    hours, remainder = divmod(milliseconds, 3_600_000)
    minutes, remainder = divmod(remainder, 60_000)
    secs, millis = divmod(remainder, 1000)
    return f"{hours:02d}:{minutes:02d}:{secs:02d}.{millis:03d}"


def split_caption_text(text: str, maximum_length: int = 72) -> list[str]:
    chunks: list[str] = []
    for sentence in re.split(r"(?<=[.!?])\s+", text.strip()):
        words = sentence.split()
        current: list[str] = []
        for word in words:
            candidate = " ".join([*current, word])
            if current and len(candidate) > maximum_length:
                chunks.append(" ".join(current))
                current = [word]
            else:
                current.append(word)
        if current:
            chunks.append(" ".join(current))
    return chunks


def write_captions(durations: list[float]) -> list[float]:
    starts: list[float] = []
    cursor = 0.0
    lines = ["WEBVTT", ""]
    cue_index = 1
    for title, text, duration in ((item[0], item[1], duration) for item, duration in zip(NARRATION, durations, strict=True)):
        starts.append(cursor)
        segment_end = cursor + duration
        chunks = split_caption_text(text)
        total_weight = sum(max(18, len(chunk)) for chunk in chunks)
        for chunk_index, chunk in enumerate(chunks):
            cue_end = segment_end if chunk_index == len(chunks) - 1 else cursor + duration * max(18, len(chunk)) / total_weight
            prefix = f"{title} · " if chunk_index == 0 else ""
            lines.extend(
                [
                    str(cue_index),
                    f"{format_timestamp(cursor)} --> {format_timestamp(cue_end)} line:82% position:50% size:72% align:center",
                    f"{prefix}{chunk}",
                    "",
                ]
            )
            cue_index += 1
            cursor = cue_end
        cursor = segment_end
    (MEDIA_DIRECTORY / "planora-guide-vi.vtt").write_text("\n".join(lines), encoding="utf-8")
    return starts


def combine_narration(segment_paths: list[Path]) -> Path:
    concat_file = BUILD_DIRECTORY / "narration-list.txt"
    concat_file.write_text(
        "\n".join(f"file '{path.as_posix()}'" for path in segment_paths),
        encoding="utf-8",
    )
    target = BUILD_DIRECTORY / "planora-guide-narration.m4a"
    subprocess.run(
        [
            FFMPEG,
            "-y",
            "-f",
            "concat",
            "-safe",
            "0",
            "-i",
            str(concat_file),
            "-c:a",
            "aac",
            "-b:a",
            "144k",
            str(target),
        ],
        check=True,
    )
    return target


CURSOR_SCRIPT = """
(() => {
  const style = document.createElement('style');
  style.textContent = `
    #planora-video-cursor { position: fixed; z-index: 99999; width: 20px; height: 20px;
      border: 3px solid white; border-radius: 50%; background: #3657d7;
      box-shadow: 0 2px 14px rgba(8,19,35,.38); pointer-events: none;
      transform: translate(-50%, -50%); transition: width 90ms, height 90ms, background 90ms; }
    #planora-video-cursor.is-down { width: 32px; height: 32px; background: #f56f61; }
    #planora-video-step { position: fixed; z-index: 99998; top: 20px; left: 50%;
      transform: translateX(-50%); padding: 10px 15px; border: 1px solid rgba(255,255,255,.26);
      border-radius: 999px; color: white; background: rgba(10,25,45,.92);
      box-shadow: 0 12px 34px rgba(8,19,35,.28); backdrop-filter: blur(10px);
      font: 700 13px Manrope, sans-serif; letter-spacing: .01em; pointer-events: none; }
  `;
  document.documentElement.append(style);
  const cursor = document.createElement('div');
  cursor.id = 'planora-video-cursor';
  cursor.style.left = '1080px'; cursor.style.top = '650px';
  document.documentElement.append(cursor);
  window.addEventListener('mousemove', event => {
    cursor.style.left = `${event.clientX}px`; cursor.style.top = `${event.clientY}px`;
  }, true);
  window.addEventListener('mousedown', () => cursor.classList.add('is-down'), true);
  window.addEventListener('mouseup', () => cursor.classList.remove('is-down'), true);
})();
"""


def show_step(page: Page, label: str) -> None:
    page.evaluate(
        """label => {
          let step = document.querySelector('#planora-video-step');
          if (!step) { step = document.createElement('div'); step.id = 'planora-video-step'; document.documentElement.append(step); }
          step.textContent = label;
        }""",
        label,
    )


def move_and_click(page: Page, selector) -> None:
    box = selector.bounding_box()
    if box:
        page.mouse.move(box["x"] + box["width"] / 2, box["y"] + box["height"] / 2, steps=18)
        page.wait_for_timeout(240)
    selector.click()


def timed_scene(page: Page, duration: float, actions: list[tuple[float, Callable[[], None]]]) -> None:
    started_at = time.monotonic()
    for fraction, action in actions:
        wait = duration * fraction - (time.monotonic() - started_at)
        if wait > 0:
            page.wait_for_timeout(round(wait * 1000))
        action()
    remainder = duration - (time.monotonic() - started_at)
    if remainder > 0:
        page.wait_for_timeout(round(remainder * 1000))


def record_walkthrough(durations: list[float]) -> Path:
    with sync_playwright() as playwright:
        browser = playwright.chromium.launch(headless=True)
        context = browser.new_context(
            viewport={"width": 1280, "height": 720},
            device_scale_factor=1,
            record_video_dir=str(BUILD_DIRECTORY),
            record_video_size={"width": 1280, "height": 720},
        )
        context.add_init_script(
            """localStorage.setItem('planora.user.accessToken', 'guide-video-token');
            localStorage.setItem('planora.user.refreshToken', 'guide-video-refresh');"""
        )
        context.add_init_script(CURSOR_SCRIPT)
        page = context.new_page()
        page.route("http://127.0.0.1:5273/api/**", mock_api)
        page.goto(f"{BASE_URL}/projects", wait_until="networkidle")
        page.get_by_role("heading", name="Chọn một project để bắt đầu").wait_for()

        show_step(page, "Quy trình Planora · Từ kế hoạch đến kết quả")
        timed_scene(page, durations[0], [])

        show_step(page, "Bước 1 · Tạo project")
        create_project = page.get_by_role("button", name="Tạo dự án").last
        timed_scene(
            page,
            durations[1],
            [
                (0.13, lambda: move_and_click(page, create_project)),
                (0.30, lambda: page.get_by_label("Tên project").fill("Ra mắt ứng dụng Planora")),
                (0.46, lambda: page.get_by_label("Mô tả").fill("Hoàn thiện sản phẩm và chuẩn bị phát hành.")),
                (0.61, lambda: page.get_by_label("Ngày bắt đầu").fill("2026-09-01")),
                (0.72, lambda: page.get_by_label("Ngày kết thúc").fill("2026-09-30")),
                (0.82, lambda: page.mouse.move(1088, 650, steps=18)),
            ],
        )

        page.locator(".dialog header .icon-button").click()
        move_and_click(page, page.get_by_role("link", name="Mở project"))
        page.get_by_role("navigation", name="Điều hướng trong project").wait_for()
        show_step(page, "Bước 2 · Lập sprint")
        sprint_link = page.locator('.project-tabs a[href$="/sprints"]')
        timed_scene(
            page,
            durations[2],
            [
                (0.10, lambda: move_and_click(page, sprint_link)),
                (0.27, lambda: move_and_click(page, page.get_by_role("button", name="Tạo sprint"))),
                (0.43, lambda: page.get_by_label("Tên sprint").fill("Sprint 5 · Sẵn sàng phát hành")),
                (0.57, lambda: page.get_by_label("Mục tiêu").fill("Kiểm thử và chốt các hạng mục quan trọng.")),
                (0.70, lambda: page.get_by_label("Bắt đầu").fill("2026-09-01")),
                (0.79, lambda: page.get_by_label("Kết thúc").fill("2026-09-14")),
                (0.87, lambda: page.mouse.move(1080, 650, steps=18)),
            ],
        )

        page.locator(".dialog header .icon-button").click()
        show_step(page, "Bước 3 · Giao công việc rõ ràng")
        task_link = page.locator('.project-tabs a[href$="/tasks"]')
        timed_scene(
            page,
            durations[3],
            [
                (0.06, lambda: move_and_click(page, task_link)),
                (0.18, lambda: move_and_click(page, page.get_by_role("button", name="Tạo công việc"))),
                (0.31, lambda: page.get_by_label("Tiêu đề").fill("Kiểm thử luồng thanh toán")),
                (0.44, lambda: page.get_by_label("Mô tả").fill("Kiểm tra toàn bộ luồng nâng cấp gói trên web.")),
                (0.56, lambda: page.get_by_label("Deadline").fill("2026-09-10")),
                (0.67, lambda: (page.get_by_label("Yêu cầu nộp").select_option("FileOnly"), page.get_by_role("button", name="PDF").click())),
                (0.77, lambda: page.get_by_label("Tiêu chí hoàn thành · mỗi dòng một mục").fill("Thanh toán thành công\nCó ảnh chụp kết quả\nKhông có lỗi console")),
                (0.88, lambda: page.mouse.move(1090, 650, steps=18)),
            ],
        )

        page.locator(".dialog header .icon-button").click()
        show_step(page, "Bước 4 · Nộp kết quả và chờ duyệt")
        submitted_task = page.locator('.task-column[data-status="Submitted"] .task-card').first
        timed_scene(
            page,
            durations[4],
            [
                (0.10, lambda: move_and_click(page, submitted_task)),
                (0.31, lambda: page.get_by_label("Ghi chú duyệt / nộp").fill("Đã kiểm tra trên desktop và mobile. Nhờ bạn duyệt giúp.")),
                (0.57, lambda: page.mouse.move(1160, 680, steps=18)),
                (0.72, lambda: page.get_by_role("button", name="Nộp duyệt").hover()),
            ],
        )

        show_step(page, "Sẵn sàng bắt đầu với Planora")
        timed_scene(page, durations[5], [(0.55, lambda: page.mouse.move(1090, 650, steps=18))])

        video = page.video
        context.close()
        browser.close()
        if video is None:
            raise RuntimeError("Playwright không tạo được video thô.")
        return Path(video.path())


def mux_video(raw_video: Path, narration: Path) -> Path:
    target = MEDIA_DIRECTORY / "planora-guide-vi.mp4"
    subprocess.run(
        [
            FFMPEG,
            "-y",
            "-ss",
            "00:00:14.500",
            "-i",
            str(raw_video),
            "-i",
            str(narration),
            "-map",
            "0:v:0",
            "-map",
            "1:a:0",
            "-vf",
            "scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2:#07111f,fps=30",
            "-c:v",
            "libx264",
            "-preset",
            "medium",
            "-crf",
            "23",
            "-pix_fmt",
            "yuv420p",
            "-c:a",
            "aac",
            "-b:a",
            "144k",
            "-shortest",
            "-movflags",
            "+faststart",
            str(target),
        ],
        check=True,
    )
    return target


def create_poster(video: Path) -> Path:
    target = MEDIA_DIRECTORY / "planora-guide-vi-poster.jpg"
    subprocess.run(
        [FFMPEG, "-y", "-ss", "00:00:02.000", "-i", str(video), "-frames:v", "1", "-update", "1", "-q:v", "3", str(target)],
        check=True,
    )
    return target


def main() -> None:
    MEDIA_DIRECTORY.mkdir(parents=True, exist_ok=True)
    BUILD_DIRECTORY.mkdir(parents=True, exist_ok=True)
    segment_paths = asyncio.run(create_narration())
    durations = [media_duration(path) for path in segment_paths]
    chapter_starts = write_captions(durations)
    narration = combine_narration(segment_paths)
    raw_video = record_walkthrough(durations)
    video = mux_video(raw_video, narration)
    poster = create_poster(video)
    result = {
        "video": str(video),
        "poster": str(poster),
        "captions": str(MEDIA_DIRECTORY / "planora-guide-vi.vtt"),
        "duration": round(media_duration(video), 2),
        "chapters": [round(value, 2) for value in chapter_starts],
        "sizeBytes": video.stat().st_size,
    }
    (BUILD_DIRECTORY / "build-report.json").write_text(json.dumps(result, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps(result, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
