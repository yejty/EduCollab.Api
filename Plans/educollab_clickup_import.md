# EduCollab ClickUp Import

## Sprint 1
- [ ] Clean project structure by feature
- [ ] Stabilize `Program.cs`
- [ ] Clean dependency injection registration
- [ ] Define database config section
- [ ] Define JWT config section
- [ ] Finalize auth DTOs
- [ ] Design `users` table
- [ ] Design `refresh_tokens` table
- [ ] Design `password_resets` table
- [ ] Implement user repository
- [ ] Implement refresh token repository
- [ ] Implement password reset repository
- [ ] Implement auth service
- [ ] Implement `POST /api/users/register`
- [ ] Implement `POST /api/users/login`
- [ ] Implement `POST /api/users/token`
- [ ] Implement `POST /api/users/reset`
- [ ] Implement `POST /api/users/reset-confirm`
- [ ] Implement `POST /api/users/change-password`
- [ ] Implement `GET /api/users/me`
- [ ] Implement JWT token generation
- [ ] Test auth endpoints in Swagger/Postman

## Sprint 2
- [ ] Design `workspaces` table
- [ ] Design `workspace_members` table
- [ ] Design `user_groups` table
- [ ] Design `user_group_members` table
- [ ] Implement workspace repository
- [ ] Implement group repository
- [ ] Implement workspace service
- [ ] Implement group service
- [ ] Implement `POST /api/workspaces`
- [ ] Implement `GET /api/workspaces`
- [ ] Implement `GET /api/workspaces/{id}`
- [ ] Implement `PUT /api/workspaces/{id}`
- [ ] Implement `GET /api/workspaces/{id}/members`
- [ ] Implement `POST /api/workspaces/{id}/members`
- [ ] Implement `PUT /api/workspaces/{id}/members/{userId}`
- [ ] Implement `DELETE /api/workspaces/{id}/members/{userId}`
- [ ] Implement `POST /api/workspaces/{workspaceId}/groups`
- [ ] Implement `GET /api/workspaces/{workspaceId}/groups`
- [ ] Implement `GET /api/workspaces/{workspaceId}/groups/{groupId}`
- [ ] Implement `PUT /api/workspaces/{workspaceId}/groups/{groupId}`
- [ ] Implement `DELETE /api/workspaces/{workspaceId}/groups/{groupId}`
- [ ] Implement `GET /api/workspaces/{workspaceId}/groups/{groupId}/members`
- [ ] Implement `POST /api/workspaces/{workspaceId}/groups/{groupId}/members`
- [ ] Implement `PUT /api/workspaces/{workspaceId}/groups/{groupId}/members/{userId}`
- [ ] Implement `DELETE /api/workspaces/{workspaceId}/groups/{groupId}/members/{userId}`
- [ ] Implement workspace role checks
- [ ] Implement group role checks

## Sprint 3
- [ ] Design `asset_folders` table
- [ ] Design `assets` table
- [ ] Design `asset_folder_group_shares` table
- [ ] Design `asset_group_shares` table
- [ ] Implement asset folder repository
- [ ] Implement asset repository
- [ ] Implement asset library service
- [ ] Implement `POST /api/workspaces/{workspaceId}/asset-folders`
- [ ] Implement `GET /api/workspaces/{workspaceId}/asset-folders`
- [ ] Implement `GET /api/workspaces/{workspaceId}/asset-folders/{folderId}`
- [ ] Implement `PUT /api/workspaces/{workspaceId}/asset-folders/{folderId}`
- [ ] Implement `DELETE /api/workspaces/{workspaceId}/asset-folders/{folderId}`
- [ ] Implement `GET /api/workspaces/{workspaceId}/asset-folders/{folderId}/folders`
- [ ] Implement `GET /api/workspaces/{workspaceId}/asset-folders/{folderId}/assets`
- [ ] Implement `POST /api/workspaces/{workspaceId}/asset-folders/{folderId}/groups`
- [ ] Implement `GET /api/workspaces/{workspaceId}/asset-folders/{folderId}/groups`
- [ ] Implement `DELETE /api/workspaces/{workspaceId}/asset-folders/{folderId}/groups/{groupId}`
- [ ] Implement `POST /api/workspaces/{workspaceId}/assets`
- [ ] Implement `GET /api/workspaces/{workspaceId}/assets`
- [ ] Implement `GET /api/workspaces/{workspaceId}/assets/{assetId}`
- [ ] Implement `PUT /api/workspaces/{workspaceId}/assets/{assetId}`
- [ ] Implement `DELETE /api/workspaces/{workspaceId}/assets/{assetId}`
- [ ] Implement `GET /api/workspaces/{workspaceId}/assets/mine`
- [ ] Implement `POST /api/workspaces/{workspaceId}/assets/{assetId}/move`
- [ ] Implement `POST /api/workspaces/{workspaceId}/assets/{assetId}/groups`
- [ ] Implement `GET /api/workspaces/{workspaceId}/assets/{assetId}/groups`
- [ ] Implement `DELETE /api/workspaces/{workspaceId}/assets/{assetId}/groups/{groupId}`
- [ ] Implement `GET /api/workspaces/{workspaceId}/groups/{groupId}/asset-folders`
- [ ] Implement `GET /api/workspaces/{workspaceId}/groups/{groupId}/asset-folders/{folderId}/folders`
- [ ] Implement `GET /api/workspaces/{workspaceId}/groups/{groupId}/asset-folders/{folderId}/assets`
- [ ] Implement `GET /api/workspaces/{workspaceId}/groups/{groupId}/assets`
- [ ] Implement recursive folder share resolution
- [ ] Implement access recalculation on asset move

## Sprint 4
- [ ] Design `scenes` table
- [ ] Design `scene_assets` table
- [ ] Design `scene_group_shares` table
- [ ] Implement scene repository
- [ ] Implement scene service
- [ ] Implement `POST /api/workspaces/{workspaceId}/scenes`
- [ ] Implement `GET /api/workspaces/{workspaceId}/scenes`
- [ ] Implement `GET /api/workspaces/{workspaceId}/scenes/{sceneId}`
- [ ] Implement `PUT /api/workspaces/{workspaceId}/scenes/{sceneId}`
- [ ] Implement `DELETE /api/workspaces/{workspaceId}/scenes/{sceneId}`
- [ ] Implement `GET /api/workspaces/{workspaceId}/scenes/mine`
- [ ] Implement `GET /api/workspaces/{workspaceId}/scenes/{sceneId}/assets`
- [ ] Implement `POST /api/workspaces/{workspaceId}/scenes/{sceneId}/groups`
- [ ] Implement `GET /api/workspaces/{workspaceId}/scenes/{sceneId}/groups`
- [ ] Implement `DELETE /api/workspaces/{workspaceId}/scenes/{sceneId}/groups/{groupId}`
- [ ] Implement `POST /api/workspaces/{workspaceId}/scenes/{sceneId}/assets/{assetId}`
- [ ] Implement `DELETE /api/workspaces/{workspaceId}/scenes/{sceneId}/assets/{assetId}`
- [ ] Implement scene JSON storage in `jsonb`
- [ ] Implement `ETag` generation
- [ ] Implement `If-Match` concurrency checks
- [ ] Return `412 Precondition Failed` on stale scene save
- [ ] Implement scene-context asset visibility

## Sprint 5
- [ ] Standardize success response payloads
- [ ] Standardize error response payloads
- [ ] Tighten validation rules
- [ ] Add local seed/dev data
- [ ] Add frontend integration notes
- [ ] Fix issues found during first frontend integration

## Sprint 6
- [ ] Implement highest-role-wins resolution
- [ ] Verify workspace override behavior
- [ ] Verify recursive folder access behavior
- [ ] Verify direct asset share plus folder share behavior
- [ ] Verify scene share vs standalone asset access behavior
- [ ] Review unresolved business TODOs

## Sprint 7
- [ ] Add auth integration tests
- [ ] Add workspace/group integration tests
- [ ] Add asset folder integration tests
- [ ] Add asset sharing integration tests
- [ ] Add scene CRUD integration tests
- [ ] Add scene asset visibility integration tests
- [ ] Improve logging for auth failures
- [ ] Improve logging for authorization failures
- [ ] Improve logging for scene concurrency conflicts
- [ ] Refactor rushed MVP code

## Sprint 8
- [ ] Fix high-priority backend bugs
- [ ] Review key query performance
- [ ] Review asset library query performance
- [ ] Review scene load/save query performance
- [ ] Finalize deployment expectations
- [ ] Finalize backend documentation
- [ ] Move non-critical items to backlog

## Backlog
- [ ] Define delete behavior for scenes, assets, and folders
- [ ] Define workspace join/invitation flow
- [ ] Define frontend conflict UX after `412`
- [ ] Add search and filtering
- [ ] Add asset previews/thumbnails
- [ ] Add favorites or pinning
- [ ] Add audit history
- [ ] Add admin/reporting features
- [ ] Move bootstrap SQL to a proper migration workflow
- [ ] Expand integration test coverage
