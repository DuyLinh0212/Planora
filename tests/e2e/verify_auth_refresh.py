from playwright.sync_api import sync_playwright


SESSION_SCRIPT = """
localStorage.setItem('planora.accessToken', 'stale-token');
localStorage.setItem('planora.refreshToken', 'valid-refresh');
localStorage.setItem('planora.sessionExpiresAt', String(Date.now() + 86400000));
localStorage.setItem('planora.remember', 'true');
"""

AUTH_RESPONSE = {
    'accessToken': 'fresh-token', 'refreshToken': 'fresh-refresh',
    'accessTokenExpiresAt': '2099-01-01T00:00:00Z', 'userId': 'user-1',
    'email': 'user@gmail.com', 'username': 'user', 'displayName': 'Planora User',
    'avatarUrl': None,
}


with sync_playwright() as playwright:
    browser = playwright.chromium.launch(headless=True)
    context = browser.new_context()
    context.add_init_script(SESSION_SCRIPT)
    page = context.new_page()
    refresh_calls = [0]

    def successful_refresh(route):
        url = route.request.url
        if '/api/auth/refresh' in url:
            refresh_calls[0] += 1
            route.fulfill(status=200, json=AUTH_RESPONSE)
        elif '/api/system/maintenance' in url:
            route.fulfill(status=200, json={'isEnabled': False, 'message': '', 'updatedAt': None})
        elif route.request.headers.get('authorization') == 'Bearer fresh-token':
            if '/api/profile' in url:
                route.fulfill(status=200, json={
                    'userId': 'user-1', 'email': 'user@gmail.com', 'username': 'user',
                    'displayName': 'Planora User', 'avatarUrl': None, 'preferredLanguage': 'vi',
                    'themePreference': 'calm', 'timeZoneId': 'Asia/Bangkok',
                    'participatingProjectCount': 0, 'quota': {'planCode': 'FREE', 'planName': 'Free',
                    'ownedProjects': 0, 'maxOwnedProjects': 1, 'storageBytes': 0, 'maxStorageBytes': 1,
                    'maxProjectStorageBytes': 1, 'maxFileSizeBytes': 1, 'dailyUploadBytes': 1,
                    'dailyUploadCount': 1, 'maxMembersPerProject': 5, 'maxVersionsPerFile': 5,
                    'subscriptionExpiresAt': None, 'autoRenew': False}
                })
            elif '/api/projects' in url:
                route.fulfill(status=200, json={'items': [], 'totalCount': 0, 'page': 1, 'pageSize': 20})
            else:
                route.fulfill(status=200, json=[])
        else:
            route.fulfill(status=401, json={'code': 'unauthorized'})

    page.route('**/api/**', successful_refresh)
    page.goto('http://localhost:4213/overview')
    page.wait_for_load_state('networkidle')
    assert page.url.endswith('/overview'), page.url
    assert refresh_calls[0] == 1, refresh_calls[0]
    assert page.evaluate("localStorage.getItem('planora.accessToken')") == 'fresh-token'
    context.close()

    failed_context = browser.new_context()
    failed_context.add_init_script(SESSION_SCRIPT)
    failed_page = failed_context.new_page()
    failed_page.route('**/api/**', lambda route: route.fulfill(status=401, json={'code': 'unauthorized'}))
    failed_page.goto('http://localhost:4213/overview')
    failed_page.wait_for_url('**/login')
    assert failed_page.evaluate("localStorage.getItem('planora.accessToken')") is None
    failed_context.close()
    browser.close()
