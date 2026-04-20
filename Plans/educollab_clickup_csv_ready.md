# EduCollab ClickUp CSV Ready

Use the CSV block below for ClickUp CSV import. Suggested columns:
- `Task Name`
- `List`
- `Description`
- `Status`
- `Priority`
- `Tags`

```csv
Task Name,List,Description,Status,Priority,Tags
Clean project structure by feature,Sprint 1,Organize backend code into feature-based folders across API Application and Contracts,Todo,High,"api,structure"
Stabilize Program.cs,Sprint 1,Make startup and composition root clean and easy to extend,Todo,High,"api,startup"
Clean dependency injection registration,Sprint 1,Make service and repository registration consistent and readable,Todo,High,"api,di"
Define database config section,Sprint 1,Add structured configuration for database connection and related settings,Todo,Normal,"config,sql"
Define JWT config section,Sprint 1,Add structured configuration for JWT secret issuer audience and expiration,Todo,High,"auth,config"
Finalize auth DTOs,Sprint 1,Complete request and response contracts for auth endpoints,Todo,High,"auth,contracts"
Design users table,Sprint 1,Define SQL schema for users and core identity fields,Todo,High,"auth,sql"
Design refresh_tokens table,Sprint 1,Define SQL schema for refresh token storage,Todo,Normal,"auth,sql"
Design password_resets table,Sprint 1,Define SQL schema for password reset tokens,Todo,Normal,"auth,sql"
Implement user repository,Sprint 1,Add persistence methods for user lookup and creation,Todo,High,"auth,repository"
Implement refresh token repository,Sprint 1,Add persistence methods for refresh tokens,Todo,Normal,"auth,repository"
Implement password reset repository,Sprint 1,Add persistence methods for reset flow,Todo,Normal,"auth,repository"
Implement auth service,Sprint 1,Implement register login refresh and password flows in application layer,Todo,High,"auth,service"
Implement POST register,Sprint 1,Expose POST /api/users/register,Todo,High,"auth,api"
Implement POST login,Sprint 1,Expose POST /api/users/login,Todo,High,"auth,api"
Implement POST token,Sprint 1,Expose POST /api/users/token,Todo,High,"auth,api"
Implement POST reset,Sprint 1,Expose POST /api/users/reset,Todo,Normal,"auth,api"
Implement POST reset-confirm,Sprint 1,Expose POST /api/users/reset-confirm,Todo,Normal,"auth,api"
Implement POST change-password,Sprint 1,Expose POST /api/users/change-password,Todo,Normal,"auth,api"
Implement GET me,Sprint 1,Expose GET /api/users/me for authenticated user info,Todo,High,"auth,api"
Implement JWT token generation,Sprint 1,Generate signed access tokens for authenticated users,Todo,High,"auth,jwt"
Test auth endpoints,Sprint 1,Verify auth flows in Swagger or Postman,Todo,High,"auth,testing"
Design workspaces table,Sprint 2,Define SQL schema for workspaces,Todo,High,"workspace,sql"
Design workspace_members table,Sprint 2,Define workspace membership with roles Owner Admin Member,Todo,High,"workspace,sql"
Design user_groups table,Sprint 2,Define SQL schema for groups inside workspace,Todo,High,"groups,sql"
Design user_group_members table,Sprint 2,Define group membership and group roles,Todo,High,"groups,sql"
Implement workspace repository,Sprint 2,Add persistence for workspace CRUD and membership queries,Todo,High,"workspace,repository"
Implement group repository,Sprint 2,Add persistence for group CRUD and membership queries,Todo,High,"groups,repository"
Implement workspace service,Sprint 2,Implement workspace use cases and role checks,Todo,High,"workspace,service"
Implement group service,Sprint 2,Implement group use cases and role checks,Todo,High,"groups,service"
Implement workspace endpoints,Sprint 2,Add CRUD and membership endpoints for workspaces,Todo,High,"workspace,api"
Implement group endpoints,Sprint 2,Add CRUD and membership endpoints for groups,Todo,High,"groups,api"
Implement workspace role checks,Sprint 2,Enforce Owner Admin Member rules,Todo,High,"workspace,auth"
Implement group role checks,Sprint 2,Enforce Viewer Contributor Manager rules,Todo,High,"groups,auth"
Design asset_folders table,Sprint 3,Define recursive folder tree for asset library,Todo,High,"folders,sql"
Design assets table,Sprint 3,Define metadata model for externally stored assets,Todo,High,"assets,sql"
Design asset_folder_group_shares table,Sprint 3,Define folder sharing table with group roles,Todo,High,"sharing,sql"
Design asset_group_shares table,Sprint 3,Define direct asset sharing table with group roles,Todo,Normal,"sharing,sql"
Implement asset folder repository,Sprint 3,Add folder persistence and recursive traversal support,Todo,High,"folders,repository"
Implement asset repository,Sprint 3,Add asset persistence and move operations,Todo,High,"assets,repository"
Implement asset library service,Sprint 3,Implement folder asset and sharing business logic,Todo,High,"assets,service"
Implement folder endpoints,Sprint 3,Add asset folder CRUD browsing and share endpoints,Todo,High,"folders,api"
Implement asset endpoints,Sprint 3,Add asset CRUD move and share endpoints,Todo,High,"assets,api"
Implement group asset library listing,Sprint 3,Add group scoped folder and asset browsing endpoints,Todo,High,"groups,assets,api"
Implement recursive folder share resolution,Sprint 3,Apply folder sharing to nested folders and assets,Todo,High,"sharing,folders"
Implement access recalculation on asset move,Sprint 3,Recompute inherited access when moving assets between folders,Todo,High,"sharing,assets"
Design scenes table,Sprint 4,Define standalone scene storage with jsonb and etag,Todo,High,"scenes,sql"
Design scene_assets table,Sprint 4,Define scene to asset attachment mapping,Todo,High,"scenes,sql"
Design scene_group_shares table,Sprint 4,Define scene sharing table with group roles,Todo,High,"sharing,sql"
Implement scene repository,Sprint 4,Add persistence for scene CRUD and etag updates,Todo,High,"scenes,repository"
Implement scene service,Sprint 4,Implement scene business logic and access rules,Todo,High,"scenes,service"
Implement scene endpoints,Sprint 4,Add scene CRUD and sharing endpoints,Todo,High,"scenes,api"
Implement scene asset endpoints,Sprint 4,Add attach detach and GET /scenes/{sceneId}/assets endpoints,Todo,High,"scenes,assets,api"
Implement scene JSON storage,Sprint 4,Store scene content in jsonb and resolve asset ids,Todo,High,"scenes,sql"
Implement ETag generation,Sprint 4,Generate version token for scenes,Todo,High,"scenes,concurrency"
Implement If-Match checks,Sprint 4,Reject stale updates when etag does not match,Todo,High,"scenes,concurrency"
Return 412 on stale scene save,Sprint 4,Return Precondition Failed on optimistic concurrency conflict,Todo,High,"scenes,concurrency"
Implement scene-context asset visibility,Sprint 4,Allow assets to be used in scene without standalone access,Todo,High,"scenes,assets,auth"
Standardize success response payloads,Sprint 5,Make API responses predictable for frontend use,Todo,Normal,"api,contracts"
Standardize error response payloads,Sprint 5,Make API errors consistent across modules,Todo,High,"api,errors"
Tighten validation rules,Sprint 5,Add missing DTO validation and response handling,Todo,Normal,"api,validation"
Add local seed data,Sprint 5,Provide demo data for frontend developer,Todo,Normal,"dev,data"
Add frontend integration notes,Sprint 5,Document core endpoint flows and auth requirements,Todo,Normal,"docs,frontend"
Fix first integration issues,Sprint 5,Resolve issues found when frontend starts consuming backend,Todo,High,"integration"
Implement highest-role-wins resolution,Sprint 6,Use highest effective group role when multiple groups grant access,Todo,High,"auth,sharing"
Verify workspace override behavior,Sprint 6,Ensure workspace owner and admin override normal access checks,Todo,High,"auth,workspace"
Verify recursive folder access behavior,Sprint 6,Test nested folder access and inheritance rules,Todo,High,"folders,sharing"
Verify direct asset plus folder share behavior,Sprint 6,Ensure direct and inherited access combine correctly,Todo,High,"assets,sharing"
Verify scene vs standalone asset access,Sprint 6,Keep scene-context access separate from standalone asset viewer permission,Todo,High,"scenes,assets,auth"
Review unresolved business TODOs,Sprint 6,Revisit open product decisions that affect behavior,Todo,Normal,"planning,todo"
Add auth integration tests,Sprint 7,Create integration tests for register login refresh and me,Todo,High,"testing,auth"
Add workspace and group integration tests,Sprint 7,Create integration tests for memberships and roles,Todo,High,"testing,workspace,groups"
Add asset folder integration tests,Sprint 7,Test folder CRUD recursion and browsing,Todo,Normal,"testing,folders"
Add asset sharing integration tests,Sprint 7,Test direct and inherited asset access,Todo,High,"testing,sharing,assets"
Add scene CRUD integration tests,Sprint 7,Test create read update delete and concurrency,Todo,High,"testing,scenes"
Add scene asset visibility tests,Sprint 7,Test scene-context asset access behavior,Todo,High,"testing,scenes,assets"
Improve logging for auth failures,Sprint 7,Add useful logs for invalid login and token errors,Todo,Normal,"logging,auth"
Improve logging for authorization failures,Sprint 7,Add useful logs for permission denials,Todo,Normal,"logging,auth"
Improve logging for scene conflicts,Sprint 7,Add useful logs for stale etag saves,Todo,Normal,"logging,scenes"
Refactor rushed MVP code,Sprint 7,Clean up technical debt introduced during MVP delivery,Todo,Normal,"refactor"
Fix high-priority backend bugs,Sprint 8,Resolve the most important stability issues before release,Todo,High,"bugs,release"
Review key query performance,Sprint 8,Check performance of core read and write flows,Todo,Normal,"performance"
Review asset library query performance,Sprint 8,Check folder and asset listing performance,Todo,Normal,"performance,assets"
Review scene load and save performance,Sprint 8,Check scene json load save and asset resolution performance,Todo,Normal,"performance,scenes"
Finalize deployment expectations,Sprint 8,Document environment config and release needs,Todo,Normal,"release,ops"
Finalize backend documentation,Sprint 8,Document architecture endpoints and maintenance notes,Todo,Normal,"docs"
Move non-critical items to backlog,Sprint 8,Separate release scope from future enhancements,Todo,Low,"backlog"
Define delete behavior,TODO Decisions,Decide deletion rules for scenes assets and folders,Todo,Normal,"todo,sql"
Define workspace join flow,TODO Decisions,Decide invitation and membership onboarding behavior,Todo,Normal,"todo,workspace"
Define UI conflict flow after 412,TODO Decisions,Decide client behavior when scene save fails due to etag mismatch,Todo,Normal,"todo,scenes"
Add search and filtering,Backlog,Support search and filters in asset library and scenes,Todo,Low,"backlog,search"
Add asset previews and thumbnails,Backlog,Support preview rendering for assets,Todo,Low,"backlog,assets"
Add favorites or pinning,Backlog,Support favorite or pinned items in UI,Todo,Low,"backlog,ux"
Add audit history,Backlog,Track important changes and user actions,Todo,Low,"backlog,audit"
Add admin and reporting features,Backlog,Add admin dashboards and reporting endpoints,Todo,Low,"backlog,admin"
Move bootstrap SQL to migrations,Backlog,Replace ad hoc bootstrap SQL with proper migration workflow,Todo,Normal,"backlog,sql"
Expand integration test coverage,Backlog,Increase regression coverage over time,Todo,Low,"backlog,testing"
```

## How To Use In ClickUp
- Create Lists named `Sprint 1` through `Sprint 8`, `Backlog`, and `TODO Decisions`
- In ClickUp, use CSV import
- Map:
  - `Task Name` -> task title
  - `List` -> list name
  - `Description` -> task description
  - `Status` -> status
  - `Priority` -> priority
  - `Tags` -> tags
