# Drive.Api (parked)

Parked host for content and collaboration routes moved out of Hub:

- assets / asset-groups / asset content
- scenes / scene-groups / scene-assets
- flows / flow-groups / flow-scenes
- sessions (workspace, public, internal)
- group library listings (`/groups/{id}/assets|scenes|flows`)

Hub.Api keeps auth, users, workspaces, invites, permissions, group membership, and admin.

Application/Infrastructure layers are still shared (`Space.*`). This project is compilable and runnable, but not the primary deploy target yet.
