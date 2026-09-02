import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { routes } from './app.routes';

describe('Web.Admin application composition', () => {
  beforeEach(async () => { await TestBed.configureTestingModule({ imports: [App], providers: [provideRouter(routes)] }).compileComponents(); });
  it('creates the routed application root', () => { expect(TestBed.createComponent(App).componentInstance).toBeTruthy(); });
  it('declares every administration feature route', () => { const children = routes.find((route) => route.path === '')?.children?.map((route) => route.path); expect(children).toEqual(jasmine.arrayContaining(['overview', 'accounts', 'plans', 'payments', 'feedback', 'analytics', 'activity', 'settings'])); });
  it('declares administrator recovery routes', () => { const paths = routes.map((route) => route.path); expect(paths).toEqual(jasmine.arrayContaining(['login', 'forgot-password', 'reset-password'])); });
});
