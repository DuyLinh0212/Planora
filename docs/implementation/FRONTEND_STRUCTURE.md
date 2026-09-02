# Frontend Structure

## Angular web frontends

Both Angular CLI 20 frontends follow the same ownership rules:

```text
src/
├── app/
│   ├── core/             # Singleton services, guards, interceptors, auth
│   ├── shared/           # Reusable components, directives, pipes
│   ├── features/         # Business features and feature routes
│   ├── layouts/          # Shells and page layouts
│   ├── app.config.ts     # Application providers and composition
│   └── app.routes.ts     # Application routes
├── assets/               # Static images and feature assets
├── environments/         # Environment-specific public configuration
└── styles/               # Global SCSS tokens and foundations
```

Rules:

- `app.config.ts` and `app.routes.ts` stay as application composition points.
- Business-specific components, services, and routes belong under `features/<feature>/`.
- Singleton infrastructure such as API clients, authentication, guards, and interceptors belongs under `app/core/`.
- Reusable UI primitives belong under `app/shared/`; feature-specific behavior stays with its feature.
- Layout shells belong under `app/layouts/`.
- Public API URLs and other non-secret configuration belong under `environments/`.

## Flutter mobile application

```text
lib/
├── main.dart                           # Process entry point only
├── app/                                # MaterialApp and app-level composition
├── core/
│   └── theme/                          # Shared tokens and ThemeData
├── features/
│   └── workspace/
│       └── presentation/
│           ├── workspace_screen.dart
│           └── widgets/                # Workspace-owned widgets
└── shared/                              # Added only when a real cross-feature abstraction exists
```

Future feature slices can add `domain/` and `data/` below their own feature when API integration begins. Empty architecture folders are intentionally avoided.
