from playwright.sync_api import sync_playwright


with sync_playwright() as playwright:
    browser = playwright.chromium.launch(headless=True)
    page = browser.new_page()
    page.add_init_script("localStorage.setItem('planora.preview', 'true')")
    page.goto('http://localhost:4211/projects')
    page.wait_for_load_state('networkidle')

    assert page.url.endswith('/login'), page.url
    assert page.get_by_role('button', name='Continue in preview mode').count() == 0

    page.goto('http://localhost:4211/register')
    page.wait_for_load_state('networkidle')
    assert page.locator('.wordmark').inner_text() == 'Planora'
    browser.close()
