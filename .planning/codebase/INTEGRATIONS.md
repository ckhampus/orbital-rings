# External Integrations

**Analysis Date:** 2026-03-02

## APIs & External Services

**Third-Party APIs:**
- None detected - Project is a single-player game with no external API integration

**Distribution:**
- itch.io - Target platform for initial distribution (per IDEA.md)
  - Not yet integrated; planned for future release phase

## Data Storage

**Databases:**
- None - Game uses local save files only
- Godot's built-in ResourceSaver for game state persistence

**File Storage:**
- Local filesystem only - Player save games stored in Godot's user data directory
- Icon assets: `icon.svg` (colocated in project root)

**Caching:**
- None - Single-player game with no external caching layer

## Authentication & Identity

**Auth Provider:**
- Custom/Local - No authentication system
- Single-player experience with no user accounts or login required
- Game state is player-specific but locally stored

## Monitoring & Observability

**Error Tracking:**
- None - Project uses standard .NET exception handling
- Godot's built-in debug system for development

**Logs:**
- Standard console output via Godot's print() system
- Debug logs in editor (via Godot Output panel)
- Development environment: No production logging configured

## CI/CD & Deployment

**Hosting:**
- None at present - Desktop and Android game
- Target distribution: itch.io (planned, not yet integrated)

**CI Pipeline:**
- None detected - Project uses manual build via MSBuild/Godot Editor

**Build Process:**
- Godot Editor export system - Primary export method
- MSBuild for C# compilation
- Multi-target: Windows (.exe), Linux, Android (.apk)

## Environment Configuration

**Environment Variables:**
- None required for gameplay
- Development environment variables (in `.devcontainer/devcontainer.json`):
  - `TZ` - Timezone (default: America/Los_Angeles)
  - `NODE_OPTIONS` - Node.js memory config (--max-old-space-size=4096)
  - `CLAUDE_CONFIG_DIR` - Development tool config
  - `POWERLEVEL9K_DISABLE_GITSTATUS` - Terminal theme setting
  - `DOTNET_ROOT` - .NET SDK location
  - `DOTNET_CLI_TELEMETRY_OPTOUT` - Disable .NET telemetry
  - `EDITOR` / `VISUAL` - Default editor (nano)
  - `SHELL` - Shell preference (zsh)

**Secrets Location:**
- None currently in use
- Permission restrictions configured in `.claude/settings.local.json`:
  - Blocks read access to `.env*` files
  - Blocks read access to `**/secrets/*` directories
  - Blocks credential files (`**/*credential*`)
  - Blocks cryptographic keys (`.pem`, `.key`)

## Webhooks & Callbacks

**Incoming:**
- None - Game has no server component

**Outgoing:**
- None - Game has no external API calls

## Development Infrastructure

**Version Control:**
- Git - Repository hosting at workspace root
- Remote: None configured (local development only at present)

**Development Container:**
- Docker-based development environment (`.devcontainer/Dockerfile`)
- Base image: Node.js 24 (provides ecosystem compatibility)
- Includes: .NET 10 SDK, Godot tooling, C# language server
- Firewall configuration for development: iptables/ipset support

**Package Managers:**
- No package managers required for game runtime
- NPM - Used for development tools only (via `postCreateCommand`)
  - get-shit-done-cc (Claude Code development framework)

---

*Integration audit: 2026-03-02*
