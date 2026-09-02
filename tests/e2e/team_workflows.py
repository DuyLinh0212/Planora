"""Planora team collaboration E2E contract.

This suite intentionally uses the real Angular UI and the real API/database. UI gaps
(invitation acceptance, task assignment, submission review and deadline review) are
driven through Playwright's APIRequestContext so the browser journey can still verify
the complete backend contract. Every major state is captured under the artifact folder.
"""

from __future__ import annotations

import json
import os
import sys
import time
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
from pathlib import Path
from typing import Any

from playwright.sync_api import APIRequestContext, APIResponse, Browser, BrowserContext, Page, sync_playwright


WEB_URL = os.getenv("PLANORA_TEST_BASE_URL", "http://127.0.0.1:4200").rstrip("/")
API_URL = os.getenv("PLANORA_TEST_API_URL", "http://127.0.0.1:5273").rstrip("/")
ARTIFACTS = Path(os.getenv("PLANORA_TEAM_E2E_ARTIFACTS", Path(__file__).parent / "artifacts" / "team-workflows"))
PASSWORD = os.getenv("PLANORA_TEAM_E2E_PASSWORD", "Planora#E2E2026!")

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="backslashreplace")


@dataclass(frozen=True)
class Account:
    label: str
    display_name: str
    username: str
    email: str


class ContractFailure(AssertionError):
    pass


def utc_iso(value: datetime) -> str:
    return value.astimezone(timezone.utc).isoformat().replace("+00:00", "Z")


def enum_is(value: Any, name: str, numeric: int) -> bool:
    return value == numeric or str(value).lower() == name.lower()


class TeamWorkflow:
    def __init__(self, browser: Browser, playwright: Any) -> None:
        self.browser = browser
        self.playwright = playwright
        self.failures: list[str] = []
        self.notes: list[str] = []
        self.shot_index = 0

    def screenshot(self, page: Page, name: str) -> None:
        self.shot_index += 1
        page.screenshot(path=str(ARTIFACTS / f"{self.shot_index:02d}-{name}.png"), full_page=True)

    def contract(self, name: str, condition: bool, detail: str = "") -> None:
        if condition:
            print(f"PASS  {name}")
            return
        message = f"{name}: {detail or 'contract was not satisfied'}"
        self.failures.append(message)
        print(f"FAIL  {message}")

    def required(self, name: str, condition: bool, detail: str = "") -> None:
        self.contract(name, condition, detail)
        if not condition:
            raise ContractFailure(f"{name}: {detail}")

    def api_context(self, access_token: str) -> APIRequestContext:
        return self.playwright.request.new_context(
            base_url=API_URL,
            extra_http_headers={"Authorization": f"Bearer {access_token}"},
        )

    def api(
        self,
        request: APIRequestContext,
        method: str,
        path: str,
        data: Any | None = None,
        expected: tuple[int, ...] = (200, 204),
    ) -> tuple[APIResponse, Any]:
        response = request.fetch(path, method=method, data=data, fail_on_status_code=False)
        try:
            body = response.json()
        except Exception:
            body = response.text()
        if response.status not in expected:
            raise ContractFailure(f"{method} {path} returned {response.status}: {body}")
        return response, body

    def register(self, account: Account, shot: bool = False) -> dict[str, Any]:
        context = self.browser.new_context(viewport={"width": 1440, "height": 900})
        page = context.new_page()
        page.set_default_timeout(12_000)
        page.goto(f"{WEB_URL}/register")
        page.wait_for_load_state("networkidle")
        page.locator('input[name="displayName"]').fill(account.display_name)
        page.locator('input[name="username"]').fill(account.username)
        page.locator('input[name="email"]').fill(account.email)
        page.locator('input[name="password"]').fill(PASSWORD)
        page.locator('input[name="confirmPassword"]').fill(PASSWORD)
        # The custom checkmark SVG visually covers the native checkbox.
        page.locator('input[name="terms"]').check(force=True)
        with page.expect_response(lambda response: response.url.endswith("/api/auth/register")) as pending:
            page.get_by_role("button", name="Tạo tài khoản", exact=True).click()
        response = pending.value
        body = response.json()
        self.required(f"register {account.label}", response.status in (200, 201), f"status={response.status}, body={body}")
        page.wait_for_url("**/projects")
        if shot:
            self.screenshot(page, "scenario-1-three-accounts-created")
        context.close()
        return body

    def login(self, account: Account) -> tuple[BrowserContext, Page, dict[str, Any]]:
        context = self.browser.new_context(viewport={"width": 1440, "height": 900})
        page = context.new_page()
        page.set_default_timeout(12_000)
        page.goto(f"{WEB_URL}/login")
        page.wait_for_load_state("networkidle")
        page.locator('input[name="identifier"]').fill(account.email)
        page.locator('input[name="password"]').fill(PASSWORD)
        with page.expect_response(lambda response: response.url.endswith("/api/auth/login")) as pending:
            page.get_by_role("button", name="Đăng nhập", exact=True).click()
        response = pending.value
        body = response.json()
        self.required(f"login {account.label}", response.status == 200, str(body))
        page.wait_for_url("**/projects")
        page.wait_for_load_state("networkidle")
        return context, page, body

    def create_project(self, page: Page, name: str, now: datetime) -> dict[str, Any]:
        page.get_by_role("button", name="Tạo dự án").first.click()
        dialog = page.locator(".dialog")
        dialog.locator('input[name="name"]').fill(name)
        dialog.locator('textarea[name="description"]').fill("Project E2E cho luồng giao việc, nộp bài và gia hạn.")
        dialog.locator('input[name="startAt"]').fill((now - timedelta(days=2)).date().isoformat())
        dialog.locator('input[name="endAt"]').fill((now + timedelta(days=45)).date().isoformat())
        with page.expect_response(lambda response: response.url.endswith("/api/projects") and response.request.method == "POST") as pending:
            dialog.get_by_role("button", name="Tạo và mở project").click()
        response = pending.value
        body = response.json()
        self.required("leader creates project", response.status == 201, str(body))
        page.wait_for_url(f"**/projects/{body['id']}/overview")
        page.wait_for_load_state("networkidle")
        self.screenshot(page, "scenario-1-project-created")
        return body

    def invite(self, page: Page, project_id: str, member: Account) -> dict[str, Any]:
        page.goto(f"{WEB_URL}/projects/{project_id}/members")
        page.wait_for_load_state("networkidle")
        page.get_by_role("button", name="Mời thành viên").click()
        dialog = page.locator(".dialog")
        dialog.locator('select[name="role"] option').first.wait_for(state="attached")
        dialog.locator('input[name="email"]').fill(member.email)
        with page.expect_response(lambda response: response.url.endswith(f"/api/projects/{project_id}/invitations") and response.request.method == "POST") as pending:
            dialog.get_by_role("button", name="Gửi lời mời").click()
        response = pending.value
        body = response.json()
        self.required(f"invite {member.label}", response.status == 201, str(body))
        return body

    def create_sprint(self, page: Page, project_id: str, now: datetime) -> dict[str, Any]:
        page.goto(f"{WEB_URL}/projects/{project_id}/sprints")
        page.wait_for_load_state("networkidle")
        page.get_by_role("button", name="Tạo sprint").first.click()
        dialog = page.locator(".dialog")
        dialog.locator('input[name="name"]').fill("Sprint E2E cộng tác")
        dialog.locator('textarea[name="goal"]').fill("Kiểm chứng giao việc, bài nộp và thống kê.")
        dialog.locator('input[name="startAt"]').fill(now.date().isoformat())
        dialog.locator('input[name="endAt"]').fill((now + timedelta(days=14)).date().isoformat())
        with page.expect_response(lambda response: response.url.endswith(f"/api/projects/{project_id}/sprints") and response.request.method == "POST") as pending:
            dialog.get_by_role("button", name="Tạo sprint", exact=True).click()
        response = pending.value
        body = response.json()
        self.required("leader creates sprint", response.status == 201, str(body))
        page.get_by_text("Đã tạo sprint.", exact=True).wait_for()
        self.screenshot(page, "scenario-1-sprint-created")
        return body

    def create_task_ui(
        self,
        page: Page,
        project_id: str,
        sprint_id: str | None,
        title: str,
        description: str,
        due_date: str,
    ) -> dict[str, Any]:
        page.goto(f"{WEB_URL}/projects/{project_id}/tasks?create=1")
        page.wait_for_load_state("networkidle")
        dialog = page.locator(".dialog--task")
        dialog.get_by_role("heading", name="Tạo công việc").wait_for()
        dialog.locator('input[name="title"]').fill(title)
        dialog.locator('textarea[name="description"]').fill(description)
        if sprint_id:
            dialog.locator('select[name="sprintId"]').select_option(sprint_id)
        dialog.locator('select[name="priority"]').select_option("High")
        dialog.locator('input[name="dueAt"]').fill(due_date)
        dialog.locator('textarea[name="criteria"]').fill("Có tệp minh chứng\nNộp trước deadline hiệu lực")
        with page.expect_response(lambda response: response.url.endswith(f"/api/projects/{project_id}/tasks") and response.request.method == "POST") as pending:
            dialog.get_by_role("button", name="Lưu công việc").click()
        response = pending.value
        body = response.json()
        self.required(f"create task {title}", response.status == 201, str(body))
        page.get_by_text("Đã tạo công việc.", exact=True).wait_for()
        return body

    def update_task_ui(self, page: Page, project_id: str, task: dict[str, Any], updated_title: str) -> None:
        page.goto(f"{WEB_URL}/projects/{project_id}/tasks")
        page.wait_for_load_state("networkidle")
        page.get_by_role("button", name=task["title"], exact=False).click()
        page.locator(".task-drawer").get_by_role("button", name="Chỉnh sửa").click()
        dialog = page.locator(".dialog--task")
        dialog.locator('input[name="title"]').fill(updated_title)
        dialog.locator('textarea[name="description"]').fill("Nội dung đã được leader cập nhật và cần lưu audit history.")
        with page.expect_response(lambda response: response.url.endswith(f"/api/tasks/{task['id']}") and response.request.method == "PUT") as pending:
            dialog.get_by_role("button", name="Lưu công việc").click()
        response = pending.value
        self.required("leader updates task content", response.status in (200, 204), response.text())
        page.get_by_text("Đã cập nhật công việc.", exact=True).wait_for()
        self.screenshot(page, "scenario-2-task-updated")

    def check_notification(self, account: Account, expected_title: str, expected_message: str, shot_name: str) -> tuple[dict[str, Any], BrowserContext, Page]:
        context, page, auth = self.login(account)
        page.get_by_role("button", name="Thông báo").click()
        panel = page.locator(".notification-panel")
        panel.get_by_role("heading", name="Thông báo").wait_for()
        title_visible = panel.get_by_text(expected_title, exact=True).count() > 0
        message_visible = panel.get_by_text(expected_message, exact=True).count() > 0
        self.contract(f"{account.label} receives {expected_title}", title_visible and message_visible, f"expected message: {expected_message}")
        self.screenshot(page, shot_name)
        return auth, context, page

    def upload_evidence(self, page: Page, project_id: str, request: APIRequestContext, member: Account, image: bool) -> str:
        _, folder = self.api(request, "POST", f"/api/projects/{project_id}/storage/folders", {"name": f"Bai nop {member.username}", "parentFolderId": None}, (201,))
        page.goto(f"{WEB_URL}/projects/{project_id}/files")
        page.wait_for_load_state("networkidle")
        page.get_by_role("button", name=f"Bai nop {member.username}", exact=False).click()
        payload = (
            {"name": f"{member.username}.png", "mimeType": "image/png", "buffer": b"\x89PNG\r\n\x1a\nE2E"}
            if image
            else {"name": f"{member.username}.txt", "mimeType": "text/plain", "buffer": b"Planora E2E evidence"}
        )
        with page.expect_response(lambda response: response.url.endswith(f"/api/projects/{project_id}/storage/files") and response.request.method == "POST") as pending:
            page.locator('input[type="file"]').set_input_files(payload)
        response = pending.value
        try:
            body = response.json()
        except Exception:
            body = response.text()
        self.contract(f"{member.label} uploads evidence file", response.status == 201, str(body))
        self.screenshot(page, f"scenario-1-{member.label}-upload")
        return body.get("id", member.username) if isinstance(body, dict) else member.username

    def create_task_api(
        self,
        request: APIRequestContext,
        project_id: str,
        title: str,
        due_at: datetime,
    ) -> dict[str, Any]:
        _, task = self.api(
            request,
            "POST",
            f"/api/projects/{project_id}/tasks",
            {
                "sprintId": None,
                "title": title,
                "description": "Task E2E deadline có độ chính xác tới giờ.",
                "priority": "High",
                "dueAt": utc_iso(due_at),
                "acceptanceCriteria": ["Gia hạn đúng nhánh nghiệp vụ"],
                "type": "Testing",
                "submissionRequirement": "Any",
                "allowedExtensions": [],
                "dependsOnTaskId": None,
                "isMilestone": False,
            },
            (201,),
        )
        return task

    def run(self) -> None:
        ARTIFACTS.mkdir(parents=True, exist_ok=True)
        for previous_shot in ARTIFACTS.glob("*.png"):
            previous_shot.unlink()
        (ARTIFACTS / "report.json").unlink(missing_ok=True)
        run_id = datetime.now(timezone.utc).strftime("%m%d%H%M%S")
        accounts = [
            Account("leader", "E2E Leader", f"e2eleader{run_id}", f"planora.e2e.leader.{run_id}@gmail.com"),
            Account("member-1", "E2E Member One", f"e2emember1{run_id}", f"planora.e2e.member1.{run_id}@gmail.com"),
            Account("member-2", "E2E Member Two", f"e2emember2{run_id}", f"planora.e2e.member2.{run_id}@gmail.com"),
        ]
        now = datetime.now(timezone.utc).replace(microsecond=0)

        # Scenario 1: accounts -> project -> invitations -> sprint -> two assignee tasks -> submissions -> analytics.
        registration = [self.register(account, shot=index == 2) for index, account in enumerate(accounts)]
        leader_context, leader_page, leader_auth = self.login(accounts[0])
        leader_api = self.api_context(leader_auth["accessToken"])
        project = self.create_project(leader_page, f"Planora E2E {run_id}", now)
        project_id = project["id"]
        invitations = [self.invite(leader_page, project_id, member) for member in accounts[1:]]

        member_apis: list[APIRequestContext] = []
        for auth, invitation in zip(registration[1:], invitations):
            request = self.api_context(auth["accessToken"])
            self.api(request, "POST", f"/api/project-invitations/{invitation['id']}/accept", {})
            member_apis.append(request)

        _, members = self.api(leader_api, "GET", f"/api/projects/{project_id}/members")
        member_by_email = {member["email"].lower(): member for member in members}
        self.required("both invitations become active memberships", all(account.email.lower() in member_by_email for account in accounts[1:]), str(members))
        leader_page.goto(f"{WEB_URL}/projects/{project_id}/members")
        leader_page.wait_for_load_state("networkidle")
        self.screenshot(leader_page, "scenario-1-three-active-members")

        sprint = self.create_sprint(leader_page, project_id, now)
        due_date = (now + timedelta(days=7)).date().isoformat()
        tasks = [
            self.create_task_ui(leader_page, project_id, sprint["id"], "E2E · Nộp tài liệu TXT", "Member 1 chuẩn bị báo cáo văn bản.", due_date),
            self.create_task_ui(leader_page, project_id, sprint["id"], "E2E · Nộp ảnh minh chứng", "Member 2 chuẩn bị ảnh minh chứng.", due_date),
        ]
        for task, account in zip(tasks, accounts[1:]):
            membership_id = member_by_email[account.email.lower()]["membershipId"]
            self.api(leader_api, "POST", f"/api/tasks/{task['id']}/assignees", {"projectMemberId": membership_id})

        logged_in_members: list[tuple[dict[str, Any], BrowserContext, Page]] = []
        for index, (account, task) in enumerate(zip(accounts[1:], tasks), start=1):
            logged_in_members.append(self.check_notification(account, "Bạn có công việc mới", task["title"], f"scenario-1-member-{index}-assignment-notification"))

        for index, ((auth, context, page), account, task) in enumerate(zip(logged_in_members, accounts[1:], tasks)):
            request = member_apis[index]
            evidence_id = self.upload_evidence(page, project_id, request, account, image=index == 1)
            _, submission = self.api(
                request,
                "POST",
                f"/api/tasks/{task['id']}/submit",
                {
                    "description": f"Bài nộp E2E của {account.display_name}",
                    "links": [{"url": f"https://example.invalid/evidence/{evidence_id}", "linkType": "evidence", "title": "Tệp đã tải lên project"}],
                },
                (201,),
            )
            self.api(leader_api, "POST", f"/api/submissions/{submission['id']}/approve", {})
            _, completed = self.api(leader_api, "GET", f"/api/tasks/{task['id']}")
            self.contract(f"approved task for {account.label} is done", enum_is(completed["status"], "Done", 4), str(completed))
            context.close()

        leader_page.goto(f"{WEB_URL}/projects/{project_id}/analytics")
        leader_page.wait_for_load_state("networkidle")
        self.contract("analytics shows two completed tasks", leader_page.get_by_text("Hoàn tất").locator("..").get_by_text("2", exact=True).count() > 0)
        self.contract("analytics completion rate is 100%", leader_page.get_by_text("100%", exact=True).count() >= 1)
        self.screenshot(leader_page, "scenario-1-leader-analytics")

        # Scenario 2: shared task update, audit history, update notifications and multi-assignee completion.
        shared = self.create_task_ui(leader_page, project_id, sprint["id"], "E2E · Task chung hai thành viên", "Cả hai thành viên phải nộp.", due_date)
        for account in accounts[1:]:
            self.api(leader_api, "POST", f"/api/tasks/{shared['id']}/assignees", {"projectMemberId": member_by_email[account.email.lower()]["membershipId"]})
        updated_title = "E2E · Task chung đã cập nhật"
        self.update_task_ui(leader_page, project_id, shared, updated_title)
        _, history = self.api(leader_api, "GET", f"/api/tasks/{shared['id']}/history")
        self.contract("task edit is stored in audit history", any(item.get("action") == "task.updated" and item.get("beforeJson") and item.get("afterJson") for item in history), str(history))

        for index, (account, request) in enumerate(zip(accounts[1:], member_apis), start=1):
            _, notifications = self.api(request, "GET", "/api/notifications?unreadOnly=false&limit=30")
            self.contract(
                f"{account.label} receives task update notification",
                any(item.get("type") == "task.updated" and updated_title in item.get("message", "") for item in notifications),
                "backend must create task.updated notification for every active assignee",
            )
            _, update_context, _ = self.check_notification(
                account,
                "Công việc đã được cập nhật",
                updated_title,
                f"scenario-2-member-{index}-update-notification",
            )
            update_context.close()

        submissions: list[dict[str, Any]] = []
        for account, request in zip(accounts[1:], member_apis):
            response = request.post(
                f"/api/tasks/{shared['id']}/submit",
                data={"description": f"Phần việc của {account.display_name}", "links": [{"url": f"https://example.invalid/{account.username}.txt", "linkType": "evidence", "title": "E2E"}]},
                fail_on_status_code=False,
            )
            body = response.json()
            self.contract(f"shared task accepts submission from {account.label}", response.status == 201, f"status={response.status}, body={body}")
            if response.status == 201:
                submissions.append(body)
        _, shared_state = self.api(leader_api, "GET", f"/api/tasks/{shared['id']}")
        self.contract("shared task automatically completes after all assignees submit", len(submissions) == 2 and enum_is(shared_state["status"], "Done", 4), str(shared_state))
        leader_page.goto(f"{WEB_URL}/projects/{project_id}/tasks")
        leader_page.wait_for_load_state("networkidle")
        self.screenshot(leader_page, "scenario-2-shared-task-after-submissions")

        # Scenario 3: overdue tasks, member-requested extension counts as late.
        overdue_tasks = [
            self.create_task_api(leader_api, project_id, f"E2E · Quá hạn {account.label}", now - timedelta(minutes=10))
            for account in accounts[1:]
        ]
        for task, account in zip(overdue_tasks, accounts[1:]):
            self.api(leader_api, "POST", f"/api/tasks/{task['id']}/assignees", {"projectMemberId": member_by_email[account.email.lower()]["membershipId"]})

        deadline = time.monotonic() + 75
        expired: list[dict[str, Any]] = []
        while time.monotonic() < deadline:
            expired = [self.api(leader_api, "GET", f"/api/tasks/{task['id']}")[1] for task in overdue_tasks]
            if all(enum_is(task["status"], "Expired", 5) for task in expired):
                break
            time.sleep(2)
        self.contract("deadline worker marks both tasks expired", len(expired) == 2 and all(enum_is(task["status"], "Expired", 5) for task in expired), str(expired))

        requested_due = now + timedelta(days=2)
        for task, account, request in zip(overdue_tasks, accounts[1:], member_apis):
            _, extension_id = self.api(request, "POST", f"/api/tasks/{task['id']}/extension-requests", {"requestedDueAt": utc_iso(requested_due), "reason": f"Gia hạn E2E cho {account.label}"}, (201,))
            self.api(leader_api, "POST", f"/api/extension-requests/{extension_id}/approve", {"note": "Leader đồng ý gia hạn"})
            _, deadline_history = self.api(leader_api, "GET", f"/api/tasks/{task['id']}/deadline-history")
            newest = deadline_history[0] if deadline_history else {}
            self.contract(
                f"approved request for {account.label} counts as late extension",
                bool(newest.get("countsAsLate")) and enum_is(newest.get("changeType"), "MemberRequestApproved", 0) and bool(newest.get("extensionRequestId")),
                str(newest),
            )
            _, submission = self.api(request, "POST", f"/api/tasks/{task['id']}/submit", {"description": "Nộp sau hạn gốc nhưng trong hạn gia hạn", "links": [{"url": f"https://example.invalid/late/{account.username}.txt", "linkType": "evidence", "title": "Late E2E"}]}, (201,))
            self.api(leader_api, "POST", f"/api/submissions/{submission['id']}/approve", {})

        leader_page.goto(f"{WEB_URL}/projects/{project_id}/analytics")
        leader_page.wait_for_load_state("networkidle")
        self.screenshot(leader_page, "scenario-3-late-submissions-after-approved-extension")

        # Scenario 4: leader directly moves a deadline; it must not count as an extension/late event.
        direct_task = self.create_task_api(leader_api, project_id, "E2E · Leader đổi deadline trực tiếp", now + timedelta(days=3))
        direct_due = now + timedelta(days=5)
        self.api(leader_api, "POST", f"/api/tasks/{direct_task['id']}/extend-deadline", {"newDueAt": utc_iso(direct_due), "reason": "Leader cân đối lại lịch"})
        _, direct_history = self.api(leader_api, "GET", f"/api/tasks/{direct_task['id']}/deadline-history")
        direct_change = direct_history[0] if direct_history else {}
        self.contract(
            "leader direct deadline change does not count as late extension",
            direct_change.get("countsAsLate") is False and enum_is(direct_change.get("changeType"), "LeaderDirect", 1) and not direct_change.get("extensionRequestId"),
            str(direct_change),
        )
        leader_page.goto(f"{WEB_URL}/projects/{project_id}/tasks")
        leader_page.wait_for_load_state("networkidle")
        leader_page.get_by_role("button", name="E2E · Leader đổi deadline trực tiếp", exact=False).click()
        self.screenshot(leader_page, "scenario-4-leader-direct-deadline-change")

        # Scenario 5: document history records editor/time; both members can view but cannot persist edits.
        _, roles = self.api(leader_api, "GET", f"/api/projects/{project_id}/roles")
        member_role = next((role for role in roles if role["code"].lower() == "member"), None)
        self.required("project has Member role", member_role is not None, str(roles))
        for account in accounts[1:]:
            self.api(
                leader_api,
                "PUT",
                f"/api/projects/{project_id}/members/{member_by_email[account.email.lower()]['membershipId']}/role",
                {"roleId": member_role["id"]},
            )
        _, restricted_folder = self.api(
            leader_api,
            "POST",
            f"/api/projects/{project_id}/storage/folders",
            {"name": "E2E · Tài liệu chỉ leader sửa", "parentFolderId": None},
            (201,),
        )
        for account in accounts[1:]:
            self.api(
                leader_api,
                "PUT",
                f"/api/storage/folders/{restricted_folder['id']}/permissions",
                {
                    "roleId": None,
                    "projectMemberId": member_by_email[account.email.lower()]["membershipId"],
                    "canView": True,
                    "canCreate": False,
                    "canUpload": False,
                    "canEdit": False,
                    "canDelete": False,
                },
            )
        _, document = self.api(
            leader_api,
            "POST",
            f"/api/projects/{project_id}/storage/documents",
            {
                "folderId": restricted_folder["id"],
                "title": "E2E · Biên bản có lịch sử",
                "content": "# Phiên bản 1\nNội dung do leader tạo.",
                "contentFormat": "markdown",
            },
            (201,),
        )

        leader_page.goto(f"{WEB_URL}/projects/{project_id}/files")
        leader_page.wait_for_load_state("networkidle")
        leader_page.get_by_role("button", name="E2E · Tài liệu chỉ leader sửa", exact=False).click()
        leader_page.get_by_role("button", name="E2E · Biên bản có lịch sử", exact=False).wait_for()
        leader_page.get_by_role("button", name="E2E · Biên bản có lịch sử", exact=False).click()
        editor = leader_page.locator(".dialog--wide")
        editor.get_by_role("heading", name="E2E · Biên bản có lịch sử").wait_for()
        editor.locator('textarea[name="content"]').fill("# Phiên bản 2\nLeader đã chỉnh sửa nội dung và lưu audit trail.")
        editor.locator('input[name="changeNote"]').fill("Leader cập nhật nội dung E2E")
        with leader_page.expect_response(lambda response: response.url.endswith(f"/api/storage/documents/{document['id']}") and response.request.method == "PUT") as pending_document_update:
            editor.get_by_role("button", name="Lưu tài liệu").click()
        self.required("leader can save document version", pending_document_update.value.status in (200, 204), pending_document_update.value.text())
        leader_page.get_by_text("Đã lưu tài liệu.", exact=True).wait_for()
        editor.get_by_role("button", name="Hủy").click()
        leader_page.get_by_role("button", name="E2E · Biên bản có lịch sử", exact=False).wait_for()
        leader_page.get_by_role("button", name="E2E · Biên bản có lịch sử", exact=False).click()
        editor = leader_page.locator(".dialog--wide")
        editor.get_by_text("v2 · E2E Leader", exact=True).wait_for()
        editor.get_by_text("v1 · E2E Leader", exact=True).wait_for()
        self.screenshot(leader_page, "scenario-5-leader-document-version-history")
        editor.get_by_role("button", name="Hủy").click()

        _, versions = self.api(leader_api, "GET", f"/api/storage/documents/{document['id']}/versions")
        self.contract("document keeps two ordered versions", [item["versionNumber"] for item in versions] == [2, 1], str(versions))
        self.contract(
            "document history stores leader and edit timestamps",
            len(versions) == 2
            and all(item["editedByUserId"] == leader_auth["userId"] and item["editedByDisplayName"] == accounts[0].display_name and item["createdAt"] for item in versions)
            and versions[0]["createdAt"] >= versions[1]["createdAt"],
            str(versions),
        )

        for index, (account, request) in enumerate(zip(accounts[1:], member_apis), start=1):
            _, member_storage = self.api(request, "GET", f"/api/projects/{project_id}/storage?folderId={restricted_folder['id']}")
            self.contract(
                f"{account.label} can view restricted folder and document",
                any(item["id"] == document["id"] for item in member_storage["documents"]),
                str(member_storage),
            )
            _, member_versions = self.api(request, "GET", f"/api/storage/documents/{document['id']}/versions")
            self.contract(f"{account.label} can view document history", len(member_versions) == 2, str(member_versions))
            denied = request.put(
                f"/api/storage/documents/{document['id']}",
                data={"content": f"Unauthorized edit by {account.label}", "contentFormat": "markdown", "changeNote": "must be denied"},
                fail_on_status_code=False,
            )
            self.contract(f"{account.label} cannot edit restricted document", denied.status == 403, f"status={denied.status}, body={denied.text()}")

            member_context, member_page, _ = self.login(account)
            member_page.goto(f"{WEB_URL}/projects/{project_id}/files")
            member_page.wait_for_load_state("networkidle")
            member_page.get_by_role("button", name="E2E · Tài liệu chỉ leader sửa", exact=False).click()
            member_page.get_by_role("button", name="E2E · Biên bản có lịch sử", exact=False).wait_for()
            member_page.get_by_role("button", name="E2E · Biên bản có lịch sử", exact=False).click()
            member_editor = member_page.locator(".dialog--wide")
            member_editor.get_by_text("v2 · E2E Leader", exact=True).wait_for()
            member_editor.locator('textarea[name="content"]').fill(f"Unauthorized UI edit by {account.label}")
            with member_page.expect_response(lambda response: response.url.endswith(f"/api/storage/documents/{document['id']}") and response.request.method == "PUT") as pending_denied_edit:
                member_editor.get_by_role("button", name="Lưu tài liệu").click()
            self.contract(f"{account.label} UI edit is denied", pending_denied_edit.value.status == 403, pending_denied_edit.value.text())
            denied_toast = member_page.locator(".toast")
            denied_toast.wait_for()
            self.contract(f"{account.label} sees edit denial feedback", denied_toast.inner_text() != "Đã lưu tài liệu.", denied_toast.inner_text())
            self.screenshot(member_page, f"scenario-5-{account.label}-view-only-document")
            member_context.close()

        # Scenario 6: leader assigns a full-access folder to member 1 and a view-only folder to member 2.
        _, editor_folder = self.api(
            leader_api,
            "POST",
            f"/api/projects/{project_id}/storage/folders",
            {"name": "E2E · Member 1 toàn quyền", "parentFolderId": None},
            (201,),
        )
        _, viewer_folder = self.api(
            leader_api,
            "POST",
            f"/api/projects/{project_id}/storage/folders",
            {"name": "E2E · Member 2 chỉ xem", "parentFolderId": None},
            (201,),
        )
        self.api(
            leader_api,
            "PUT",
            f"/api/storage/folders/{editor_folder['id']}/permissions",
            {"roleId": None, "projectMemberId": member_by_email[accounts[1].email.lower()]["membershipId"], "canView": True, "canCreate": True, "canUpload": True, "canEdit": True, "canDelete": True},
        )
        self.api(
            leader_api,
            "PUT",
            f"/api/storage/folders/{viewer_folder['id']}/permissions",
            {"roleId": None, "projectMemberId": member_by_email[accounts[2].email.lower()]["membershipId"], "canView": True, "canCreate": False, "canUpload": False, "canEdit": False, "canDelete": False},
        )
        _, viewer_document = self.api(
            leader_api,
            "POST",
            f"/api/projects/{project_id}/storage/documents",
            {"folderId": viewer_folder["id"], "title": "E2E · Viewer không được sửa", "content": "Leader-owned content", "contentFormat": "markdown"},
            (201,),
        )

        member_one_api, member_two_api = member_apis
        _, member_one_view = self.api(member_one_api, "GET", f"/api/projects/{project_id}/storage?folderId={editor_folder['id']}")
        self.contract("member-1 granted folder.view", isinstance(member_one_view, dict))
        _, member_one_child = self.api(
            member_one_api,
            "POST",
            f"/api/projects/{project_id}/storage/folders",
            {"name": "E2E · Member 1 tạo folder", "parentFolderId": editor_folder["id"]},
            (201,),
        )
        member_one_upload = member_one_api.post(
            f"/api/projects/{project_id}/storage/files",
            multipart={
                "folderId": editor_folder["id"],
                "changeNote": "member-1 upload permission",
                "file": {"name": "member-1-permission.txt", "mimeType": "text/plain", "buffer": b"permission matrix"},
            },
            fail_on_status_code=False,
        )
        member_one_file = member_one_upload.json()
        self.contract("member-1 granted file.upload", member_one_upload.status == 201, f"status={member_one_upload.status}, body={member_one_file}")
        rename = member_one_api.put(f"/api/storage/files/{member_one_file['id']}/name", data={"name": "member-1-renamed.txt"}, fail_on_status_code=False)
        self.contract("member-1 granted file.edit", rename.status in (200, 204), f"status={rename.status}, body={rename.text()}")
        deletable_upload = member_one_api.post(
            f"/api/projects/{project_id}/storage/files",
            multipart={
                "folderId": editor_folder["id"],
                "changeNote": "delete permission",
                "file": {"name": "member-1-delete.txt", "mimeType": "text/plain", "buffer": b"delete me"},
            },
            fail_on_status_code=False,
        )
        deletable_file = deletable_upload.json()
        deleted = member_one_api.delete(f"/api/storage/files/{deletable_file['id']}", fail_on_status_code=False)
        self.contract("member-1 granted file.delete", deleted.status in (200, 204), f"status={deleted.status}, body={deleted.text()}")

        member_one_context, member_one_page, _ = self.login(accounts[1])
        member_one_page.goto(f"{WEB_URL}/projects/{project_id}/files")
        member_one_page.wait_for_load_state("networkidle")
        member_one_page.get_by_role("button", name="E2E · Member 1 toàn quyền", exact=False).click()
        member_one_page.get_by_text("member-1-renamed.txt", exact=True).wait_for()
        self.screenshot(member_one_page, "scenario-6-member-1-granted-permissions")
        member_one_context.close()

        _, member_two_view = self.api(member_two_api, "GET", f"/api/projects/{project_id}/storage?folderId={viewer_folder['id']}")
        self.contract("member-2 granted folder.view", any(item["id"] == viewer_document["id"] for item in member_two_view["documents"]), str(member_two_view))
        denied_create = member_two_api.post(f"/api/projects/{project_id}/storage/folders", data={"name": "Denied", "parentFolderId": viewer_folder["id"]}, fail_on_status_code=False)
        self.contract("member-2 denied folder.create", denied_create.status == 404, f"status={denied_create.status}, body={denied_create.text()}")
        denied_upload = member_two_api.post(
            f"/api/projects/{project_id}/storage/files",
            multipart={"folderId": viewer_folder["id"], "changeNote": "denied", "file": {"name": "denied.txt", "mimeType": "text/plain", "buffer": b"denied"}},
            fail_on_status_code=False,
        )
        self.contract("member-2 denied file.upload", denied_upload.status == 404, f"status={denied_upload.status}, body={denied_upload.text()}")
        denied_rename = member_two_api.put(f"/api/storage/documents/{viewer_document['id']}/name", data={"name": "Denied rename"}, fail_on_status_code=False)
        self.contract("member-2 denied document.edit", denied_rename.status == 404, f"status={denied_rename.status}, body={denied_rename.text()}")
        denied_delete = member_two_api.delete(f"/api/storage/documents/{viewer_document['id']}", fail_on_status_code=False)
        self.contract("member-2 denied document.delete", denied_delete.status == 404, f"status={denied_delete.status}, body={denied_delete.text()}")
        self.notes.append(f"Permission matrix child created by member-1: {member_one_child['id']}")

        member_two_context, member_two_page, _ = self.login(accounts[2])
        member_two_page.goto(f"{WEB_URL}/projects/{project_id}/files")
        member_two_page.wait_for_load_state("networkidle")
        member_two_page.get_by_role("button", name="E2E · Member 2 chỉ xem", exact=False).click()
        member_two_page.get_by_role("button", name="E2E · Viewer không được sửa", exact=False).wait_for()
        member_two_page.get_by_role("button", name="E2E · Viewer không được sửa", exact=False).click()
        viewer_editor = member_two_page.locator(".dialog--wide")
        viewer_editor.locator('textarea[name="content"]').fill("Member 2 denied content")
        with member_two_page.expect_response(lambda response: response.url.endswith(f"/api/storage/documents/{viewer_document['id']}") and response.request.method == "PUT") as pending_viewer_denied:
            viewer_editor.get_by_role("button", name="Lưu tài liệu").click()
        self.contract("member-2 view-only UI cannot save", pending_viewer_denied.value.status == 403, pending_viewer_denied.value.text())
        viewer_toast = member_two_page.locator(".toast")
        viewer_toast.wait_for()
        self.contract("member-2 sees view-only denial feedback", viewer_toast.inner_text() != "Đã lưu tài liệu.", viewer_toast.inner_text())
        self.screenshot(member_two_page, "scenario-6-member-2-view-only-permissions")
        member_two_context.close()

        for request in [leader_api, *member_apis]:
            request.dispose()
        leader_context.close()

        report = {
            "runId": run_id,
            "webUrl": WEB_URL,
            "apiUrl": API_URL,
            "accounts": [{"label": account.label, "email": account.email} for account in accounts],
            "projectId": project_id,
            "screenshots": self.shot_index,
            "failures": self.failures,
            "notes": self.notes,
        }
        (ARTIFACTS / "report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")


def main() -> None:
    ARTIFACTS.mkdir(parents=True, exist_ok=True)
    with sync_playwright() as playwright:
        browser = playwright.chromium.launch(headless=True)
        workflow = TeamWorkflow(browser, playwright)
        try:
            workflow.run()
        finally:
            browser.close()
        if workflow.failures:
            raise AssertionError("\n" + "\n".join(workflow.failures))
        print(f"Planora team workflows passed. Screenshots: {ARTIFACTS}")


if __name__ == "__main__":
    main()
