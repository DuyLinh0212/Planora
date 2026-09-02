from __future__ import annotations

import json
from pathlib import Path

from playwright.sync_api import ConsoleMessage, Page, sync_playwright


WORKSPACE = Path(r"F:\NgDuyLinh\Personal_Project\Planora")
OUTPUT_DIRECTORY = WORKSPACE / "artifacts" / "playwright"
USER_URL = "http://127.0.0.1:4200"
ADMIN_URL = "http://127.0.0.1:4300"


def attach_console_collector(page: Page, errors: list[dict[str, str]]) -> None:
    def collect_console_message(message: ConsoleMessage) -> None:
        if message.type == "error" and "favicon.ico" not in message.text:
            errors.append({"page": page.url, "message": message.text})

    page.on("console", collect_console_message)
    page.on("pageerror", lambda exception: errors.append({"page": page.url, "message": str(exception)}))


def capture_user_workspace(browser, errors: list[dict[str, str]], captures: list[str]) -> None:
    context = browser.new_context(viewport={"width": 1600, "height": 1000}, device_scale_factor=1)
    context.add_init_script("localStorage.setItem('planora.preview', 'true')")
    page = context.new_page()
    attach_console_collector(page, errors)
    page.goto(USER_URL, wait_until="networkidle")
    page.locator(".workstream").wait_for(state="visible")
    page.screenshot(path=OUTPUT_DIRECTORY / "planora-web-user-overview.png", full_page=True)
    captures.append("planora-web-user-overview.png")

    page.goto(f"{USER_URL}/views", wait_until="networkidle")
    page.locator(".commitment-horizon").wait_for(state="visible")
    page.screenshot(path=OUTPUT_DIRECTORY / "planora-web-user-project-views.png", full_page=True)
    captures.append("planora-web-user-project-views.png")
    page.goto(f"{USER_URL}/members", wait_until="networkidle")
    page.locator(".member-summary-grid").wait_for(state="visible")
    page.screenshot(path=OUTPUT_DIRECTORY / "planora-web-user-members.png", full_page=True)
    captures.append("planora-web-user-members.png")
    page.goto(f"{USER_URL}/billing", wait_until="networkidle")
    page.locator(".plan-grid").wait_for(state="visible")
    page.screenshot(path=OUTPUT_DIRECTORY / "planora-web-user-billing.png", full_page=True)
    captures.append("planora-web-user-billing.png")
    context.close()

    compact_context = browser.new_context(viewport={"width": 1220, "height": 700}, device_scale_factor=1)
    compact_context.add_init_script("localStorage.setItem('planora.preview', 'true')")
    compact_page = compact_context.new_page()
    attach_console_collector(compact_page, errors)
    compact_page.goto(USER_URL, wait_until="networkidle")
    compact_page.locator(".workstream").wait_for(state="visible")
    compact_page.screenshot(path=OUTPUT_DIRECTORY / "planora-web-user-workstream-1220.png", full_page=False)
    captures.append("planora-web-user-workstream-1220.png")
    compact_context.close()

    mobile_context = browser.new_context(viewport={"width": 390, "height": 844}, device_scale_factor=1)
    mobile_context.add_init_script("localStorage.setItem('planora.preview', 'true')")
    mobile_page = mobile_context.new_page()
    attach_console_collector(mobile_page, errors)
    mobile_page.goto(f"{USER_URL}/views", wait_until="networkidle")
    mobile_page.locator(".commitment-horizon").wait_for(state="visible")
    mobile_page.screenshot(path=OUTPUT_DIRECTORY / "planora-web-user-mobile.png", full_page=True)
    captures.append("planora-web-user-mobile.png")
    mobile_context.close()


def capture_admin_console(browser, errors: list[dict[str, str]], captures: list[str]) -> None:
    context = browser.new_context(viewport={"width": 1600, "height": 1000}, device_scale_factor=1)
    context.add_init_script("localStorage.setItem('planora.admin.preview', 'true')")
    page = context.new_page()
    attach_console_collector(page, errors)
    page.goto(ADMIN_URL, wait_until="networkidle")
    page.locator(".metric-grid").wait_for(state="visible")
    page.screenshot(path=OUTPUT_DIRECTORY / "planora-web-admin-overview.png", full_page=True)
    captures.append("planora-web-admin-overview.png")

    page.goto(f"{ADMIN_URL}/support", wait_until="networkidle")
    page.locator(".support-console").wait_for(state="visible")
    page.screenshot(path=OUTPUT_DIRECTORY / "planora-web-admin-support.png", full_page=True)
    captures.append("planora-web-admin-support.png")
    page.goto(f"{ADMIN_URL}/settings", wait_until="networkidle")
    page.locator(".maintenance-card").wait_for(state="visible")
    page.screenshot(path=OUTPUT_DIRECTORY / "planora-web-admin-settings.png", full_page=True)
    captures.append("planora-web-admin-settings.png")
    context.close()

    mobile_context = browser.new_context(viewport={"width": 390, "height": 844}, device_scale_factor=1)
    mobile_context.add_init_script("localStorage.setItem('planora.admin.preview', 'true')")
    mobile_page = mobile_context.new_page()
    attach_console_collector(mobile_page, errors)
    mobile_page.goto(f"{ADMIN_URL}/support", wait_until="networkidle")
    mobile_page.locator(".support-console").wait_for(state="visible")
    mobile_page.screenshot(path=OUTPUT_DIRECTORY / "planora-web-admin-mobile.png", full_page=True)
    captures.append("planora-web-admin-mobile.png")
    mobile_context.close()


def main() -> None:
    OUTPUT_DIRECTORY.mkdir(parents=True, exist_ok=True)
    console_errors: list[dict[str, str]] = []
    captures: list[str] = []
    with sync_playwright() as playwright:
        browser = playwright.chromium.launch(headless=True)
        capture_user_workspace(browser, console_errors, captures)
        capture_admin_console(browser, console_errors, captures)
        browser.close()

    report = {"captures": captures, "consoleErrors": console_errors, "passed": not console_errors}
    (OUTPUT_DIRECTORY / "playwright-report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(json.dumps(report, indent=2))
    if console_errors:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
