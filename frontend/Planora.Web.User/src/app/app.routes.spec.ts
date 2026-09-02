import { routes } from './app.routes';

describe('Web.User route composition', () => {
  it('declares public authentication routes', () => {
    const paths = routes.map((route) => route.path);

    expect(paths).toEqual(
      jasmine.arrayContaining(['login', 'register', 'forgot-password', 'reset-password', 'terms']),
    );
  });

  it('declares the authenticated workspace routes', () => {
    const workspace = routes.find((route) => route.path === '');
    const paths = workspace?.children?.map((route) => route.path);

    expect(paths).toEqual(
      jasmine.arrayContaining(['projects', 'notifications', 'account', 'billing', 'support', 'guide']),
    );
  });
});
