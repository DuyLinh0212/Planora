from pathlib import Path

from playwright.sync_api import sync_playwright


output = Path(r"F:\NgDuyLinh\Personal_Project\Planora\artifacts\playwright\planora-login-controls.png")
output.parent.mkdir(parents=True, exist_ok=True)

with sync_playwright() as playwright:
    browser = playwright.chromium.launch(headless=True)
    page = browser.new_page(viewport={"width": 1440, "height": 900})
    page.goto("http://127.0.0.1:4210/login", wait_until="networkidle")
    checkbox = page.locator('.check-row input[type="checkbox"]')
    checkbox.wait_for(state="visible")
    box = checkbox.bounding_box()
    page.screenshot(path=output, full_page=True)
    browser.close()

if box is None or box["width"] > 20 or box["height"] > 20:
    raise SystemExit(f"Remember-me checkbox is oversized: {box}")

print(f"Remember-me checkbox verified at {box['width']}x{box['height']} px")
