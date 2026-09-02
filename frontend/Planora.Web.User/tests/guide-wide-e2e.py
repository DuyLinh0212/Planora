"""Regression for the exact 1920x900 / localhost guide layout used by the browser."""

import importlib.util
from pathlib import Path

from playwright.sync_api import sync_playwright


TESTS = Path(__file__).parent
SPEC = importlib.util.spec_from_file_location("web_user_e2e", TESTS / "web-user-e2e.py")
assert SPEC and SPEC.loader
suite = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(suite)


with sync_playwright() as playwright:
    browser = playwright.chromium.launch(headless=True)
    page = browser.new_page(viewport={"width": 1920, "height": 900})
    suite.authenticate(page)
    page.route("http://127.0.0.1:5273/api/**", suite.api_router)
    page.goto("http://localhost:4200/guide", wait_until="domcontentloaded")
    page.get_by_role("heading", name="Từ project đến kết quả được duyệt").wait_for()

    hero = page.locator(".guide-header").bounding_box()
    card = page.locator(".guide-video-card").bounding_box()
    video = page.locator(".guide-player-shell video").bounding_box()
    steps = page.locator(".guide-steps").bounding_box()
    assert hero and hero["height"] <= 110, hero
    assert card and card["width"] <= 861, card
    assert video and video["height"] <= 311, video
    assert steps and steps["y"] + steps["height"] <= 900, steps
    assert page.evaluate("document.body.scrollWidth <= innerWidth + 1")

    artifact = TESTS / "artifacts" / "e2e" / "28-guide-localhost-1920x900.png"
    page.screenshot(path=artifact, full_page=False)
    browser.close()
    print(f"Guide 1920x900 passed: {artifact}")
