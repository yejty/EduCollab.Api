# EduCollab Backend ClickUp Plan

## Project Goal
Build the `EduCollab.Api` backend for an education collaboration app centered on Unity scenes and smart 3D assets in 4 months, with MVP ready in 2 months so frontend development can start in parallel.

## MVP Goal By End Of Month 2
- User authentication works
- Workspaces and user groups work
- Asset library folders and assets work
- Folder sharing to groups works
- Scenes can be stored as JSON
- Scenes can reference assets by `assetId`
- Scene-scoped asset access works
- Core authorization rules are enforced

## Scope Summary
### Core domains
- Users
- Workspaces
- UserGroups
- AssetFolders
- Assets
- Scenes
- SceneAssets

### Product framing
- `Workspace` = school, institution, or tenant space
- `UserGroup` = class, team, or collaboration group
- `Asset` = smart 3D Unity asset or reusable learning resource
- `Scene` = standalone Unity scene definition stored as JSON

### Sharing model
- Workspaces are the tenant boundary
- Groups are the collaboration boundary
- Scenes and assets are user-owned
- Scenes, assets, and folders can be shared to groups
- Folder sharing is recursive
- Scene asset access is contextual only

### Roles
- Workspace roles: `Owner`, `Admin`, `Member`
- Group roles: `Viewer`, `Contributor`, `Manager`

## Suggested ClickUp Structure
### Space
- `EduCollab`

### Folder
- `Backend API`

### Lists
- `Sprint 1`
- `Sprint 2`
- `Sprint 3`
- `Sprint 4`
- `Sprint 5`
- `Sprint 6`
- `Sprint 7`
- `Sprint 8`
- `Backlog`
- `TODO Decisions`

## Sprint 1: Foundation And Auth
### Goal
Create a stable backend foundation and finish auth basics.

### Tasks
- Clean project structure by feature
- Stabilize `Program.cs` and DI registration
- Define JWT and database config sections
- Finalize auth DTOs
- Design auth SQL tables
- Implement user repository
- Implement refresh token repository
- Implement password reset repository
- Implement auth service
- Implement auth controller endpoints
- Add JWT token generation
- Implement `GET /api/users/me`
- Test auth endpoints in Swagger/Postman

### Deliverables
- Register/login works
- Refresh token works
- Password reset skeleton or full flow works
- Authenticated `me` endpoint works

## Sprint 2: Workspaces And Groups
### Goal
Establish tenancy and collaboration boundaries.

### Tasks
- Create `workspaces` table and model
- Create `workspace_members` table and model
- Create `user_groups` table and model
- Create `user_group_members` table and model
- Implement workspace CRUD endpoints
- Implement workspace membership endpoints
- Implement group CRUD endpoints
- Implement group membership endpoints
- Implement workspace role checks
- Implement group role checks

### Deliverables
- Users can belong to workspaces
- Groups can be created inside workspaces
- Users can be assigned group roles

## Sprint 3: Asset Library MVP
### Goal
Deliver the first usable asset library backend.

### Tasks
- Create `asset_folders` table and model
- Support recursive folder hierarchy
- Support root-level assets
- Create `assets` table and model
- Implement folder CRUD endpoints
- Implement folder browsing endpoints
- Implement asset CRUD endpoints
- Implement asset move endpoint
- Implement external storage metadata flow
- Implement folder sharing to groups
- Implement group-scoped folder and asset listing
- Implement inherited folder access rules

### Deliverables
- Assets can be organized in folders
- Folders can be shared to groups
- Group library browsing works

## Sprint 4: Scenes MVP
### Goal
Finish MVP with scene storage and scene-scoped asset access.

### Tasks
- Create `scenes` table and model
- Store scene JSON as `jsonb`
- Add `etag` support
- Create `scene_assets` table and model
- Create `scene_group_shares` table and model
- Implement scene CRUD endpoints
- Implement full scene `PUT` save
- Implement `If-Match` / `ETag` concurrency
- Implement scene sharing to groups
- Implement `GET /scenes/{sceneId}/assets`
- Enforce contextual scene asset access

### Deliverables
- Scenes can be created and saved
- Scenes can reference assets by `assetId`
- Scene-scoped asset access works
- MVP backend is ready for frontend integration

## Sprint 5: Frontend Handoff Hardening
### Goal
Make the backend easier for another developer to consume.

### Tasks
- Standardize response payloads
- Standardize error payloads
- Improve validation rules
- Add local seed/dev data
- Write endpoint usage notes
- Fix frontend integration issues

## Sprint 6: Authorization And Edge Cases
### Goal
Harden permission behavior and sharing rules.

### Tasks
- Implement highest-role-wins logic across multiple groups
- Verify recursive folder share resolution
- Verify asset move access recalculation
- Verify scene-vs-asset permission separation
- Review unresolved business-rule TODOs

## Sprint 7: Tests And Maintainability
### Goal
Reduce regression risk and improve code health.

### Tasks
- Add auth integration tests
- Add workspace/group integration tests
- Add asset library integration tests
- Add scene access integration tests
- Improve logging around auth and authorization failures
- Refactor rushed MVP areas

## Sprint 8: Release Preparation
### Goal
Prepare a solid first backend release.

### Tasks
- Fix high-priority bugs
- Review key query performance
- Finalize deployment expectations
- Finalize backend documentation
- Move non-critical ideas to backlog

## Backlog
### Product backlog
- Invitation flow
- Delete behavior rules
- Conflict UI after `412`
- Search and filtering
- Asset previews and thumbnails
- Favorites or pinning
- Audit history
- Admin/reporting features

### Technical backlog
- Move bootstrap SQL to proper migrations
- Add more integration tests
- Add performance review for library queries
- Improve local dev tooling

## TODO Decisions
- Exact delete behavior
- Workspace join/invitation flow
- Conflict UI after `412`

## Recommended ClickUp Fields
### Custom status
- `Todo`
- `In Progress`
- `Review`
- `Blocked`
- `Done`

### Priority
- `Critical`
- `High`
- `Normal`
- `Low`

### Suggested tags
- `auth`
- `workspace`
- `groups`
- `assets`
- `folders`
- `scenes`
- `sharing`
- `sql`
- `api`
- `testing`

## Recommended First Task Order
1. Clean startup and DI
2. Implement auth schema
3. Implement register/login
4. Implement JWT and `me`
5. Implement workspaces
6. Implement groups
7. Implement folders and assets
8. Implement scenes
