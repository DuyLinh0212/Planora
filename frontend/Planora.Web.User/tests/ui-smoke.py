from pathlib import Path
from urllib.parse import urlparse
from playwright.sync_api import sync_playwright

BASE_URL = "http://127.0.0.1:4200"
ARTIFACTS = Path(__file__).parent / "artifacts"

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
    "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "name": "Planora Launch",
    "description": "Xây dựng trải nghiệm quản lý project rõ ràng và có kiểm chứng.",
    "status": "Active",
    "startAt": "2026-08-01T00:00:00Z",
    "endAt": "2026-09-30T00:00:00Z",
    "memberCount": 4,
    "updatedAt": "2026-08-30T12:00:00Z",
}

SPRINT = {
    "id": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    "projectId": PROJECT["id"],
    "name": "Sprint 4 · User web rebuild",
    "goal": "Hoàn thiện điều hướng project detail và mật độ giao diện.",
    "startAt": "2026-08-25T00:00:00Z",
    "endAt": "2026-09-07T00:00:00Z",
    "status": "Active",
}

TASKS = [
    {
        "id": f"cccccccc-cccc-cccc-cccc-ccccccccccc{i}",
        "projectId": PROJECT["id"],
        "sprintId": SPRINT["id"],
        "title": title,
        "description": "Công việc có phạm vi và tiêu chí hoàn thành rõ ràng.",
        "priority": priority,
        "status": status,
        "originalDueAt": "2026-09-02T00:00:00Z",
        "effectiveDueAt": "2026-09-02T00:00:00Z",
        "acceptanceCriteria": ["Build xanh", "Không có body scroll thừa"],
        "assigneeMemberIds": [],
        "type": "Feature",
        "submissionRequirement": "Any",
        "allowedExtensions": [],
        "dependsOnTaskId": None,
        "isMilestone": i == 5,
    }
    for i, (title, priority, status) in enumerate(
        [
            ("Chốt route project detail", "High", "Done"),
            ("Dựng task board", "High", "InProgress"),
            ("Kiểm tra typography", "Medium", "Submitted"),
            ("Rà soát mobile", "Medium", "Rework"),
            ("Tối ưu màn Files", "Low", "ToDo"),
            ("Phát hành bản mới", "High", "ToDo"),
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


def mock_api(route):
    request = route.request
    path = urlparse(request.url).path
    payload = None
    status = 200
    if path == "/api/profile":
        payload = PROFILE
    elif path == "/api/projects":
        payload = {"items": [PROJECT], "totalCount": 1, "page": 1, "pageSize": 50}
    elif path == f"/api/projects/{PROJECT['id']}":
        payload = PROJECT
    elif path == f"/api/projects/{PROJECT['id']}/sprints":
        payload = [SPRINT]
    elif path == f"/api/projects/{PROJECT['id']}/tasks":
        payload = TASKS
    elif path == f"/api/projects/{PROJECT['id']}/members":
        payload = MEMBERS
    elif path == f"/api/projects/{PROJECT['id']}/storage":
        payload = {"folders": [], "files": [], "documents": []}
    elif path == "/api/notifications":
        payload = []
    elif path == "/api/system/maintenance":
        payload = {"isEnabled": False, "message": "", "updatedAt": None}
    else:
        payload = {}
    route.fulfill(status=status, json=payload)


def assert_viewport(page, label):
    metrics = page.evaluate(
        """() => ({
          innerHeight: window.innerHeight,
          innerWidth: window.innerWidth,
          bodyHeight: document.body.scrollHeight,
          bodyWidth: document.body.scrollWidth,
          htmlHeight: document.documentElement.scrollHeight,
          htmlWidth: document.documentElement.scrollWidth
        })"""
    )
    assert metrics["bodyHeight"] <= metrics["innerHeight"] + 1, f"{label}: body scrolls vertically {metrics}"
    assert metrics["bodyWidth"] <= metrics["innerWidth"] + 1, f"{label}: body overflows horizontally {metrics}"


with sync_playwright() as playwright:
    browser = playwright.chromium.launch(headless=True)
    page = browser.new_page(viewport={"width": 1440, "height": 900})
    console_errors = []
    page.on("console", lambda message: console_errors.append(message.text) if message.type == "error" else None)
    page.route("http://127.0.0.1:5273/api/**", mock_api)

    page.goto(f"{BASE_URL}/login")
    page.wait_for_load_state("networkidle")
    page.get_by_role("heading", name="Đăng nhập Planora").wait_for()
    assert page.get_by_role("button", name="Hiện mật khẩu").count() == 1
    assert page.get_by_text("Tiếp tục với Google").count() == 1
    assert_viewport(page, "login-desktop")
    page.screenshot(path=str(ARTIFACTS / "login-desktop.png"), full_page=False)

    page.goto(f"{BASE_URL}/register")
    page.wait_for_load_state("networkidle")
    page.get_by_role("heading", name="Tạo tài khoản").wait_for()
    assert page.get_by_text("Chưa nhập").count() == 0
    password = page.locator('input[name="password"]')
    password.fill("weak")
    password.blur()
    page.get_by_text("Mật khẩu cần ít nhất 9 ký tự", exact=False).wait_for()
    assert page.locator(".password-strength").count() == 1
    assert_viewport(page, "register-desktop")
    page.screenshot(path=str(ARTIFACTS / "register-desktop.png"), full_page=False)

    page.evaluate(
        """() => {
          localStorage.setItem('planora.user.accessToken', 'ui-smoke-token');
          localStorage.setItem('planora.user.refreshToken', 'ui-smoke-refresh');
        }"""
    )
    page.goto(f"{BASE_URL}/projects")
    page.wait_for_load_state("networkidle")
    page.get_by_role("heading", name="Chọn một project để bắt đầu").wait_for()
    assert page.locator(".global-nav").get_by_text("Công việc").count() == 0
    assert page.locator(".global-nav").get_by_text("Sprint").count() == 0
    assert page.locator(".project-tabs").count() == 0
    assert_viewport(page, "projects-desktop")
    page.screenshot(path=str(ARTIFACTS / "projects-desktop.png"), full_page=False)

    page.get_by_role("link", name="Mở project").click()
    page.wait_for_load_state("networkidle")
    page.get_by_role("navigation", name="Điều hướng trong project").wait_for()
    assert page.get_by_role("navigation", name="Điều hướng trong project").get_by_text("Công việc").count() == 1
    assert page.get_by_role("navigation", name="Điều hướng trong project").get_by_text("Sprint").count() == 1
    assert_viewport(page, "project-overview-desktop")
    page.screenshot(path=str(ARTIFACTS / "project-overview-desktop.png"), full_page=False)

    page.get_by_role("navigation", name="Điều hướng trong project").get_by_text("Công việc").click()
    page.wait_for_load_state("networkidle")
    page.get_by_role("heading", name="Bảng công việc").wait_for()
    assert_viewport(page, "tasks-desktop")
    page.screenshot(path=str(ARTIFACTS / "tasks-desktop.png"), full_page=False)

    mobile = browser.new_page(viewport={"width": 390, "height": 844})
    mobile.route("http://127.0.0.1:5273/api/**", mock_api)
    mobile.add_init_script(
        """localStorage.setItem('planora.user.accessToken', 'ui-smoke-token');
        localStorage.setItem('planora.user.refreshToken', 'ui-smoke-refresh');"""
    )
    mobile.goto(f"{BASE_URL}/projects")
    mobile.wait_for_load_state("networkidle")
    mobile.get_by_role("heading", name="Chọn một project để bắt đầu").wait_for()
    assert_viewport(mobile, "projects-mobile")
    mobile.screenshot(path=str(ARTIFACTS / "projects-mobile.png"), full_page=False)
    mobile.close()

    actionable_errors = [error for error in console_errors if "favicon" not in error.lower()]
    assert not actionable_errors, f"Browser console errors: {actionable_errors}"
    print("UI smoke passed: login, global projects, project detail, task board, mobile viewport")
    browser.close()
