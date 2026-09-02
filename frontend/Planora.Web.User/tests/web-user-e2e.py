"""Full Web.User browser contract.

The API is mocked deliberately: this suite checks routing, rendering, critical UX and
browser regressions without depending on a developer database. Screenshots are kept
under tests/artifacts/e2e for review after every run.
"""

import os
from pathlib import Path
from urllib.parse import urlparse

from playwright.sync_api import Page, Route, sync_playwright


BASE_URL = os.getenv("PLANORA_TEST_BASE_URL", "http://127.0.0.1:4200")
ARTIFACTS = Path(__file__).parent / "artifacts" / "e2e"
PROJECT_ID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
SPRINT_ID = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"

PROFILE = {
    "userId": "11111111-1111-1111-1111-111111111111",
    "email": "linh@example.com",
    "username": "linh",
    "displayName": "Duy Linh Nguyễn",
    "avatarUrl": None,
    "preferredLanguage": "vi",
    "themePreference": "calm",
    "timeZoneId": "Asia/Ho_Chi_Minh",
    "participatingProjectCount": 1,
    "quota": {
        "planCode": "FREE",
        "planName": "Free",
        "ownedProjects": 1,
        "maxOwnedProjects": 1,
        "storageBytes": 536870912,
        "maxStorageBytes": 536870912,
        "maxProjectStorageBytes": 536870912,
        "maxFileSizeBytes": 26214400,
        "dailyUploadBytes": 0,
        "dailyUploadCount": 0,
        "maxMembersPerProject": 5,
        "maxVersionsPerFile": 5,
        "subscriptionExpiresAt": None,
        "autoRenew": False,
    },
}

PROJECT = {
    "id": PROJECT_ID,
    "name": "Planora Launch",
    "description": "Project mẫu dùng để kiểm thử toàn bộ Web.User.",
    # Simulate an older API process that still serializes C# enums as numbers.
    "status": 1,
    "startAt": "2026-08-01T00:00:00Z",
    "endAt": "2026-09-30T00:00:00Z",
    "memberCount": 2,
    "updatedAt": "2026-08-30T12:00:00Z",
}

SPRINT = {
    "id": SPRINT_ID,
    "projectId": PROJECT_ID,
    "name": "Sprint 4 · Web.User",
    "goal": "Bắt lỗi giao diện trước khi phát hành.",
    "startAt": "2026-08-25T00:00:00Z",
    "endAt": "2026-09-07T00:00:00Z",
    "status": 0,
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
        "acceptanceCriteria": ["Build xanh", "Có ảnh E2E"],
        "assigneeMemberIds": [],
        "type": task_type,
        "submissionRequirement": "Any",
        "allowedExtensions": [],
        "dependsOnTaskId": None,
        "isMilestone": index == 2,
    }
    for index, (title, priority, status, task_type) in enumerate(
        [
            ("Hoàn thiện checkout", "High", 1, "Feature"),
            ("Rà soát mobile", "Medium", 2, "Testing"),
            ("Phát hành beta", "High", 0, "Release nội bộ"),
        ]
    )
]

MEMBERS = [
    {
        "membershipId": "dddddddd-dddd-dddd-dddd-dddddddddddd",
        "userId": PROFILE["userId"],
        "displayName": PROFILE["displayName"],
        "email": PROFILE["email"],
        "status": 0,
        "roles": ["Owner"],
    }
]

PLANS = [
    {
        "id": "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee1",
        "code": "FREE",
        "name": "Free",
        "price": 0,
        "currency": "VND",
        "billingPeriod": 1,
        "maxOwnedProjects": 1,
        "maxStorageBytes": 536870912,
        "entitlements": ["1 project sở hữu", "500 MB lưu trữ"],
    },
    {
        "id": "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee2",
        "code": "PRO",
        "name": "Pro",
        "price": 149000,
        "currency": "VND",
        "billingPeriod": 1,
        "maxOwnedProjects": 10,
        "maxStorageBytes": 21474836480,
        "entitlements": ["10 project sở hữu", "20 GB lưu trữ", "25 thành viên / project"],
    },
    {
        "id": "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeee3",
        "code": "VIP",
        "name": "VIP",
        "price": 399000,
        "currency": "VND",
        "billingPeriod": 1,
        "maxOwnedProjects": 50,
        "maxStorageBytes": 107374182400,
        "entitlements": ["50 project sở hữu", "100 GB lưu trữ", "Ưu tiên hỗ trợ"],
    },
]

PAYMENTS = [
    {
        "id": "99999999-9999-9999-9999-999999999999",
        "planName": "Pro",
        "provider": 0,
        "amount": 149000,
        "currency": "VND",
        "status": 1,
        "createdAt": "2026-08-30T17:37:16.7681821+00:00",
    }
]


def api_router(route: Route) -> None:
    request = route.request
    path = urlparse(request.url).path
    method = request.method

    if path == "/api/profile/avatar" and method == "POST":
        payload = {"avatarUrl": "https://res.cloudinary.com/demo/image/upload/planora/identity/avatars/e2e/profile.png"}
    elif path == "/api/profile":
        payload = PROFILE
    elif path == "/api/projects" and method == "GET":
        payload = {"items": [PROJECT], "totalCount": 1, "page": 1, "pageSize": 50}
    elif path == f"/api/projects/{PROJECT_ID}":
        payload = PROJECT
    elif path == f"/api/projects/{PROJECT_ID}/sprints" and method == "GET":
        payload = [SPRINT]
    elif path == f"/api/projects/{PROJECT_ID}/sprints" and method == "POST":
        body = request.post_data_json or {}
        payload = {**SPRINT, **body, "id": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2", "status": 0}
    elif path == f"/api/projects/{PROJECT_ID}/tasks" and method == "GET":
        payload = TASKS
    elif path == f"/api/projects/{PROJECT_ID}/tasks" and method == "POST":
        body = request.post_data_json or {}
        payload = {**TASKS[0], **body, "id": "cccccccc-cccc-cccc-cccc-cccccccccc99"}
    elif path == f"/api/projects/{PROJECT_ID}/members":
        payload = MEMBERS
    elif path == f"/api/projects/{PROJECT_ID}/roles":
        payload = [{"id": "ffffffff-ffff-ffff-ffff-ffffffffffff", "name": "Member", "permissions": []}]
    elif path == f"/api/projects/{PROJECT_ID}/invitations":
        payload = []
    elif path == f"/api/projects/{PROJECT_ID}/storage":
        payload = {"folders": [], "files": [], "documents": []}
    elif path == "/api/billing/plans":
        payload = PLANS
    elif path == "/api/billing/payments":
        payload = PAYMENTS
    elif path == "/api/support/conversations":
        payload = []
    elif path == "/api/notifications":
        payload = []
    elif path == "/api/system/maintenance":
        payload = {"isEnabled": False, "message": "", "updatedAt": None}
    else:
        payload = {}

    route.fulfill(status=200, json=payload)


def authenticate(page: Page) -> None:
    page.add_init_script(
        """localStorage.setItem('planora.user.accessToken', 'e2e-access');
        localStorage.setItem('planora.user.refreshToken', 'e2e-refresh');"""
    )


def screenshot(page: Page, name: str) -> None:
    page.screenshot(path=str(ARTIFACTS / f"{name}.png"), full_page=False)


def assert_no_body_overflow(page: Page, name: str) -> None:
    metrics = page.evaluate(
        """() => ({
          width: window.innerWidth,
          bodyWidth: document.body.scrollWidth,
          htmlWidth: document.documentElement.scrollWidth
        })"""
    )
    assert metrics["bodyWidth"] <= metrics["width"] + 1, f"{name}: body overflow {metrics}"
    assert metrics["htmlWidth"] <= metrics["width"] + 1, f"{name}: html overflow {metrics}"


def open_route(page: Page, path: str, heading: str, shot: str) -> None:
    page.goto(f"{BASE_URL}{path}")
    page.wait_for_load_state("networkidle")
    page.get_by_role("heading", name=heading, exact=True).first.wait_for()
    assert_no_body_overflow(page, shot)
    screenshot(page, shot)


def run() -> None:
    ARTIFACTS.mkdir(parents=True, exist_ok=True)
    failures: list[str] = []

    def check(name: str, action) -> None:
        try:
            action()
            print(f"PASS  {name}")
        except Exception as error:  # keep running so every screenshot is available
            failures.append(f"{name}: {error}")
            print(f"FAIL  {name}: {error}")

    with sync_playwright() as playwright:
        browser = playwright.chromium.launch(headless=True)
        public_page = browser.new_page(viewport={"width": 1440, "height": 900})
        # The first lazy-loaded public route can take longer while Angular's dev server
        # finishes an initial rebuild; later interactions remain on the tighter timeout.
        public_page.set_default_timeout(10_000)
        public_page.route("http://127.0.0.1:5273/api/**", api_router)

        page = browser.new_page(viewport={"width": 1440, "height": 900})
        page.set_default_timeout(4_000)
        authenticate(page)
        page.route("http://127.0.0.1:5273/api/**", api_router)
        console_errors: list[str] = []
        page_errors: list[str] = []
        page.on("console", lambda message: console_errors.append(message.text) if message.type == "error" else None)
        page.on("pageerror", lambda error: page_errors.append(str(error)))

        public_routes = [
            ("/login", "Đăng nhập Planora", "01-login"),
            ("/register", "Tạo tài khoản", "02-register"),
            ("/forgot-password", "Quên mật khẩu?", "03-forgot-password"),
            ("/reset-password?token=e2e", "Đặt mật khẩu mới", "04-reset-password"),
            ("/terms", "Điều khoản sử dụng", "05-terms"),
        ]
        for path, heading, shot in public_routes:
            check(f"public {path}", lambda p=path, h=heading, s=shot: open_route(public_page, p, h, s))
        public_page.close()

        protected_routes = [
            ("/projects", "Chọn một project để bắt đầu", "10-projects"),
            (f"/projects/{PROJECT_ID}/overview", "Sprint 4 · Web.User", "11-overview"),
            (f"/projects/{PROJECT_ID}/tasks", "Bảng công việc", "12-tasks"),
            (f"/projects/{PROJECT_ID}/sprints", "Sprint & product backlog", "13-sprints"),
            (f"/projects/{PROJECT_ID}/views", "Góc nhìn project", "14-views"),
            (f"/projects/{PROJECT_ID}/files", "Tệp & tài liệu", "15-files"),
            (f"/projects/{PROJECT_ID}/members", "Thành viên", "16-members"),
            (f"/projects/{PROJECT_ID}/analytics", "Phân tích project", "17-analytics"),
            (f"/projects/{PROJECT_ID}/settings", "Cài đặt dự án", "18-project-settings"),
            ("/account", "Tài khoản cá nhân", "20-account"),
            ("/billing", "Gói & thanh toán", "21-billing"),
            ("/support", "Phòng hỗ trợ", "22-support"),
            ("/guide", "Từ project đến kết quả được duyệt", "23-guide"),
        ]
        for path, heading, shot in protected_routes:
            check(f"protected {path}", lambda p=path, h=heading, s=shot: open_route(page, p, h, s))

        def numeric_sprint_status_contract() -> None:
            page.goto(f"{BASE_URL}/projects/{PROJECT_ID}/overview")
            page.wait_for_load_state("networkidle")
            page.get_by_role("link", name="Xem sprint").click()
            page.get_by_role("heading", name="Sprint & product backlog").wait_for()
            page.get_by_role("button", name="Bắt đầu sprint").wait_for()
            page.get_by_role("button", name="Tạo sprint").first.click()
            dialog = page.locator(".dialog")
            dialog.get_by_role("heading", name="Tạo sprint").wait_for()
            dialog.locator('input[name="name"]').fill("Sprint kiểm thử enum")
            dialog.locator('input[name="startAt"]').fill("2026-09-10")
            dialog.locator('input[name="endAt"]').fill("2026-09-17")
            dialog.get_by_role("button", name="Tạo sprint").click()
            page.get_by_text("Đã tạo sprint.", exact=True).wait_for()
            screenshot(page, "13a-numeric-sprint-status")

        check("numeric enum statuses still allow opening and creating a sprint", numeric_sprint_status_contract)

        def quota_notice_contract() -> None:
            page.goto(f"{BASE_URL}/projects")
            page.wait_for_load_state("networkidle")
            page.get_by_role("button", name="Tạo dự án").first.click()
            page.get_by_role("heading", name="Đã đạt giới hạn project").wait_for()
            assert page.locator(".dialog").count() == 0

            page.goto(f"{BASE_URL}/projects/{PROJECT_ID}/files")
            page.wait_for_load_state("networkidle")
            page.get_by_role("button", name="Tải tệp").click()
            page.get_by_role("heading", name="Đã hết dung lượng lưu trữ").wait_for()
            screenshot(page, "13b-quota-notice")

        check("quota blocks project creation and upload with one floating notice", quota_notice_contract)

        def billing_contract() -> None:
            page.goto(f"{BASE_URL}/billing")
            page.wait_for_load_state("networkidle")
            page.get_by_role("heading", name="Pro", exact=True).wait_for()
            assert page.get_by_role("button", name="Chọn Pro").is_visible()
            assert page.get_by_text("Lịch sử giao dịch", exact=True).is_visible()
            assert page.get_by_text("Đã thanh toán", exact=True).count() == 0
            page.get_by_text("Xem lịch sử", exact=True).click()
            page.get_by_text("MoMo", exact=True).wait_for()
            assert page.get_by_text("Đã thanh toán", exact=True).is_visible()
            assert page.get_by_text("Theo tháng", exact=True).count() >= 1
            page.get_by_role("button", name="Chọn Pro").click()
            page.get_by_role("heading", name="Nâng cấp Pro").wait_for()
            screenshot(page, "24-billing-checkout")

        check("billing normalizes enum labels and keeps payment history collapsed", billing_contract)

        def avatar_upload_contract() -> None:
            page.goto(f"{BASE_URL}/account")
            page.wait_for_load_state("networkidle")
            page.locator('input[type="file"]').set_input_files({"name": "avatar.png", "mimeType": "image/png", "buffer": b"\x89PNG\r\n\x1a\n"})
            page.get_by_text("Đã cập nhật ảnh đại diện.", exact=True).wait_for()
            assert "res.cloudinary.com" in page.locator(".profile-panel .avatar img").get_attribute("src")
            screenshot(page, "20a-avatar-upload")

        check("account uploads avatar instead of accepting an arbitrary URL", avatar_upload_contract)

        def task_editor_contract() -> None:
            page.goto(f"{BASE_URL}/projects/{PROJECT_ID}/tasks?create=1")
            page.wait_for_load_state("networkidle")
            dialog = page.get_by_role("heading", name="Tạo công việc").locator("..",).locator("..")
            page.get_by_role("heading", name="Tạo công việc").wait_for()
            page.get_by_role("button", name="Tạo loại mới").wait_for()
            assert page.get_by_text("Release nội bộ", exact=True).count() >= 1
            assert page.get_by_text("Mốc quan trọng của project", exact=True).is_visible()
            assert page.get_by_text("phát hành, bàn giao", exact=False).is_visible()
            assert page.locator('input[name="extensions"]').count() == 0
            page.get_by_label("Yêu cầu nộp").select_option("FileOnly")
            page.get_by_role("button", name="PDF").click()
            page.get_by_role("button", name="Word").click()
            assert page.get_by_text(".pdf", exact=True).is_visible()
            assert page.get_by_text(".docx", exact=True).is_visible()
            screenshot(page, "25-task-create")
            assert dialog.count() == 1

        check("task editor explains project type, file formats and milestone", task_editor_contract)

        def guide_layout_contract() -> None:
            page.goto(f"{BASE_URL}/guide")
            page.wait_for_load_state("networkidle")
            steps = page.locator(".guide-steps")
            video_card = page.locator(".guide-video-card")
            steps_box = steps.bounding_box()
            video_box = video_card.bounding_box()
            assert steps_box
            assert page.locator(".guide-transcript").count() == 0
            assert video_box and video_box["width"] <= 861, video_box
            cue_contract = page.locator("video").evaluate(
                """video => new Promise(resolve => {
                  const track = video.textTracks[0];
                  const done = () => resolve(Array.from(track.cues || []).map(cue => ({ text: cue.text, size: cue.size, line: cue.line })));
                  if (track?.cues?.length) done(); else setTimeout(done, 800);
                })"""
            )
            assert len(cue_contract) >= 12, cue_contract
            assert all(len(cue["text"]) <= 100 and cue["size"] <= 72 for cue in cue_contract), cue_contract
            screenshot(page, "26-guide-layout")

        check("guide captions are compact and sections do not overlap", guide_layout_contract)

        wide = browser.new_page(viewport={"width": 1920, "height": 900})
        authenticate(wide)
        wide.route("http://127.0.0.1:5273/api/**", api_router)

        def wide_guide_contract() -> None:
            wide.goto(f"{BASE_URL}/guide")
            wide.wait_for_load_state("networkidle")
            card_box = wide.locator(".guide-video-card").bounding_box()
            video_box = wide.locator(".guide-player-shell video").bounding_box()
            assert card_box and card_box["width"] <= 861, card_box
            assert video_box and video_box["height"] <= 311, video_box
            assert video_box["y"] + video_box["height"] <= 900, video_box
            steps_box = wide.locator(".guide-steps").bounding_box()
            assert steps_box and steps_box["y"] + steps_box["height"] <= 900, steps_box
            screenshot(wide, "27-guide-wide-1920x900")

        check("guide video stays inside 1920x900 viewport", wide_guide_contract)
        wide.close()

        mobile = browser.new_page(viewport={"width": 390, "height": 844})
        authenticate(mobile)
        mobile.route("http://127.0.0.1:5273/api/**", api_router)
        check("mobile projects", lambda: open_route(mobile, "/projects", "Chọn một project để bắt đầu", "30-projects-mobile"))
        check("mobile task board", lambda: open_route(mobile, f"/projects/{PROJECT_ID}/tasks", "Bảng công việc", "31-tasks-mobile"))
        mobile.close()

        ignored_console = [message for message in console_errors if "503" not in message and "favicon" not in message.lower()]
        check("no Angular page errors", lambda: (_ for _ in ()).throw(AssertionError(page_errors)) if page_errors else None)
        check("no unexpected console errors", lambda: (_ for _ in ()).throw(AssertionError(ignored_console)) if ignored_console else None)
        browser.close()

    if failures:
        raise AssertionError("\n" + "\n".join(failures))
    print(f"Web.User E2E passed. Screenshots: {ARTIFACTS}")


if __name__ == "__main__":
    run()
