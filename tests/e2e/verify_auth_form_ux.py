from pathlib import Path
from playwright.sync_api import sync_playwright


artifact = Path(r'F:\NgDuyLinh\Personal_Project\Planora\artifacts\playwright\planora-register-auth-ux.png')
artifact.parent.mkdir(parents=True, exist_ok=True)

with sync_playwright() as playwright:
    browser = playwright.chromium.launch(headless=True)
    page = browser.new_page(viewport={"width": 1440, "height": 1050})
    page.goto('http://localhost:4212/register')
    page.wait_for_load_state('networkidle')

    password = page.locator('input[name="password"]')
    confirm = page.locator('input[name="confirmPassword"]')
    assert page.locator('.password-meter').count() == 0

    password.fill('abc')
    assert page.locator('.password-meter').is_visible()
    assert page.locator('.password-meter span').inner_text() == 'Rất yếu'
    confirm.focus()
    assert page.get_by_text('Mật khẩu cần ít nhất 9 ký tự.').is_visible()

    password.fill('StrongPass!1')
    assert page.locator('.password-meter span').inner_text() == 'Mạnh'
    assert page.locator('.password-meter i.active').count() == 4
    assert page.get_by_text('Mật khẩu cần ít nhất 9 ký tự.').count() == 0

    page.get_by_role('button', name='Hiện mật khẩu', exact=True).click()
    assert password.evaluate('(input) => input.type') == 'text'
    page.get_by_role('button', name='Ẩn mật khẩu', exact=True).click()
    assert password.evaluate('(input) => input.type') == 'password'

    confirm.fill('Different!1')
    password.focus()
    assert page.get_by_text('Mật khẩu xác nhận chưa trùng khớp.').is_visible()
    assert page.locator('.google-button svg').count() == 0
    page.screenshot(path=str(artifact), full_page=True)

    page.goto('http://localhost:4212/login')
    page.wait_for_load_state('networkidle')
    assert page.locator('.google-button svg path').count() == 4
    login_password = page.locator('input[name="password"]')
    page.get_by_role('button', name='Hiện mật khẩu', exact=True).click()
    assert login_password.evaluate('(input) => input.type') == 'text'
    browser.close()
