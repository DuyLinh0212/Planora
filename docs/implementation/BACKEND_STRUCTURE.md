# Backend Structure

Planora follows the same reduced Clean Architecture direction as HuTube: four production projects, two test projects, and folders grouped by business responsibility.

```text
backend/
├── Planora.slnx
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── src/
│   ├── Planora.Domain/
│   │   ├── Users/
│   │   ├── Projects/
│   │   ├── Sprints/
│   │   ├── Tasks/
│   │   ├── Storage/
│   │   └── Common/
│   ├── Planora.Application/
│   │   ├── Authentication/
│   │   ├── Authorization/
│   │   ├── Projects/
│   │   ├── ProjectMembers/
│   │   ├── Sprints/
│   │   ├── Tasks/
│   │   ├── TaskSubmissions/
│   │   ├── TaskDeadlines/
│   │   ├── Storage/
│   │   └── Common/
│   ├── Planora.Infrastructure/
│   │   ├── Authentication/
│   │   ├── ExternalAuth/
│   │   ├── Persistence/
│   │   └── Storage/
│   └── Planora.Api/
│       ├── Controllers/
│       │   ├── Identity/
│       │   ├── Workspace/
│       │   ├── Planning/
│       │   └── Storage/
│       ├── Authorization/
│       ├── BackgroundServices/
│       ├── Extensions/
│       └── Middleware/
└── tests/
    ├── Planora.UnitTests/
    └── Planora.IntegrationTests/
```

Dependency direction:

```text
Planora.Domain
      ↑
Planora.Application
      ↑
Planora.Api

Planora.Infrastructure ──> Planora.Application + Planora.Domain
```

Method names use a verb plus the complete business resource name. Examples: `GetProjectsAsync`, `GetProjectByIdAsync`, `CreateProjectTaskAsync`, `ApproveTaskSubmissionAsync`, and `UploadProjectFileVersionAsync`.
