from __future__ import annotations

import importlib.util
import os
from pathlib import Path

from playwright.sync_api import sync_playwright


WORKSPACE = Path(r"F:\NgDuyLinh\Personal_Project\Planora")
BASE_URL = os.getenv("PLANORA_TEST_BASE_URL", "http://127.0.0.1:4200")
ARTIFACTS = WORKSPACE / "artifacts" / "playwright"
VIDEO = WORKSPACE / "frontend" / "Planora.Web.User" / "public" / "media" / "planora-guide-vi.mp4"

spec = importlib.util.spec_from_file_location("guide_video_builder", WORKSPACE / "tools" / "create-guide-video.py")
assert spec and spec.loader
guide_video_builder = importlib.util.module_from_spec(spec)
spec.loader.exec_module(guide_video_builder)


def prepare(page) -> None:
    page.route("http://127.0.0.1:5273/api/**", guide_video_builder.mock_api)
    page.add_init_script(
        """localStorage.setItem('planora.user.accessToken', 'guide-test-token');
        localStorage.setItem('planora.user.refreshToken', 'guide-test-refresh');"""
    )


def main() -> None:
    assert VIDEO.exists() and VIDEO.stat().st_size > 1_000_000
    ARTIFACTS.mkdir(parents=True, exist_ok=True)
    console_errors: list[str] = []

    with sync_playwright() as playwright:
        browser = playwright.chromium.launch(headless=True)
        page = browser.new_page(viewport={"width": 1440, "height": 900})
        prepare(page)
        page.on("console", lambda message: console_errors.append(message.text) if message.type == "error" else None)
        page.goto(f"{BASE_URL}/guide", wait_until="networkidle")
        page.locator(".guide-video-card").wait_for()
        video = page.locator("video")
        video.evaluate("element => new Promise(resolve => element.readyState >= 1 ? resolve() : element.addEventListener('loadedmetadata', resolve, { once: true }))")
        metadata = video.evaluate(
            "element => ({ duration: element.duration, width: element.videoWidth, height: element.videoHeight, tracks: element.textTracks.length })"
        )
        assert 85 <= metadata["duration"] <= 88, metadata
        assert metadata["width"] == 1280 and metadata["height"] == 720, metadata
        assert metadata["tracks"] == 1, metadata
        assert page.locator(".guide-media-meta span").count() == 2

        page.locator(".guide-chapters > button").nth(4).click()
        page.wait_for_timeout(500)
        assert video.evaluate("element => element.currentTime") >= 60
        page.screenshot(path=ARTIFACTS / "planora-guide-video-desktop.png", full_page=False)

        mobile = browser.new_page(viewport={"width": 390, "height": 844})
        prepare(mobile)
        mobile.goto(f"{BASE_URL}/guide", wait_until="networkidle")
        mobile.locator(".guide-video-card").wait_for()
        width = mobile.evaluate("() => ({ viewport: innerWidth, body: document.body.scrollWidth, html: document.documentElement.scrollWidth })")
        assert width["body"] <= width["viewport"] + 1 and width["html"] <= width["viewport"] + 1, width
        mobile.screenshot(path=ARTIFACTS / "planora-guide-video-mobile.png", full_page=True)
        mobile.close()

        actionable_errors = [error for error in console_errors if "favicon" not in error.lower()]
        assert not actionable_errors, actionable_errors
        browser.close()

    print(f"Guide video verified: {metadata}")


if __name__ == "__main__":
    main()
