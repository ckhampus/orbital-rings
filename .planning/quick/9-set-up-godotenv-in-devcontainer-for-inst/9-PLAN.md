---
phase: quick-9
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - .devcontainer/Dockerfile
  - .devcontainer/devcontainer.json
autonomous: true
requirements: [QUICK-9]
must_haves:
  truths:
    - "GodotEnv is installed as a dotnet global tool in the devcontainer"
    - "GodotEnv installs and manages the Godot version instead of manual wget/unzip"
    - "The godot binary is accessible on PATH via GodotEnv symlink"
    - "The GODOT_VERSION build arg still controls which version gets installed"
    - "dotnet build succeeds using the GodotEnv-managed Godot installation"
  artifacts:
    - path: ".devcontainer/Dockerfile"
      provides: "GodotEnv-based Godot installation replacing manual download"
    - path: ".devcontainer/devcontainer.json"
      provides: "GODOT env var pointing to GodotEnv-managed binary"
  key_links:
    - from: ".devcontainer/Dockerfile"
      to: "GodotEnv CLI"
      via: "dotnet tool install --global Chickensoft.GodotEnv"
      pattern: "godotenv godot install"
---

<objective>
Replace the manual Godot download/extraction in the devcontainer Dockerfile with GodotEnv
(chickensoft-games/GodotEnv), a dotnet CLI tool that manages Godot versions via symlinks.

Purpose: Enable easy Godot version switching inside the devcontainer without rebuilding.
GodotEnv stores multiple versions and switches between them instantly via symlink updates.

Output: Updated Dockerfile and devcontainer.json that use GodotEnv to install and manage Godot.
</objective>

<execution_context>
@/home/node/.claude/get-shit-done/workflows/execute-plan.md
@/home/node/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.devcontainer/Dockerfile
@.devcontainer/devcontainer.json
@CLAUDE.md

<interfaces>
<!-- GodotEnv CLI commands (from https://github.com/chickensoft-games/GodotEnv): -->

Install GodotEnv:
  dotnet tool install --global Chickensoft.GodotEnv

Install a Godot version (downloads .NET-enabled by default):
  godotenv godot install <version>

Set active version (updates symlink):
  godotenv godot use <version>

Configure GODOT environment variable pointing to symlink:
  godotenv godot env setup

List installed versions:
  godotenv godot list

GodotEnv stores data under ~/.config/GodotEnv/godot/:
  versions/   -- installed Godot versions
  bin/        -- symlink to active version binary
  cache/      -- download cache

On Linux, `godotenv godot env setup` adds GODOT env var to shell config file.
</interfaces>
</context>

<tasks>

<task type="auto">
  <name>Task 1: Replace manual Godot download with GodotEnv in Dockerfile</name>
  <files>.devcontainer/Dockerfile, .devcontainer/devcontainer.json</files>
  <action>
Modify `.devcontainer/Dockerfile`:

1. REMOVE the entire manual Godot installation block (lines 68-77):
   ```
   ARG GODOT_VERSION=4.6.1
   RUN ARCH=$(dpkg --print-architecture) && \
     if [ "$ARCH" = "amd64" ]; then GODOT_ARCH="x86_64"; ...
   ```
   This manual wget/unzip/mv approach is replaced by GodotEnv.

2. KEEP the `GODOT_VERSION` build arg in devcontainer.json (already `"GODOT_VERSION": "4.6.1"`).
   Add a new `ARG GODOT_VERSION=4.6.1` line where the old one was.

3. BEFORE the `USER node` line (line 84), add GodotEnv installation as a shared tool:
   ```dockerfile
   # Install GodotEnv for Godot version management
   RUN dotnet tool install --tool-path /usr/local/share/dotnet-global-tools Chickensoft.GodotEnv
   ```
   Use `--tool-path` (same pattern as the existing csharp-ls install on line 80) instead of
   `--global` so it is available system-wide and not tied to a specific user's home directory.
   The `/usr/local/share/dotnet-global-tools` directory is already on PATH (line 81).

4. AFTER the `USER node` line (switching to non-root), add GodotEnv Godot installation:
   ```dockerfile
   # Install Godot via GodotEnv (manages versions via symlinks)
   ARG GODOT_VERSION=4.6.1
   RUN godotenv godot install ${GODOT_VERSION}
   ```
   This runs as the `node` user so GodotEnv stores versions under the user's home config.
   GodotEnv downloads the .NET-enabled Godot build by default.

5. Add the GodotEnv bin directory to PATH so the symlinked `godot` binary is found.
   GodotEnv on Linux stores its symlink at `~/.config/GodotEnv/godot/bin/godot` (or similar
   XDG-based path). After the install command, add:
   ```dockerfile
   # Add GodotEnv's Godot symlink to PATH and set GODOT env var
   ENV GODOT_BIN="/home/node/.config/GodotEnv/godot/bin"
   ENV PATH="$PATH:$GODOT_BIN"
   ENV GODOT="$GODOT_BIN/godot"
   ```
   Note: The exact path GodotEnv uses needs to be verified. After the `godotenv godot install`
   step, run `godotenv godot env path` or check `~/.config/GodotEnv/` to find the actual
   binary path. Adjust the ENV lines accordingly.

   IMPORTANT: After install, verify the binary path by adding a verification step:
   ```dockerfile
   RUN godotenv godot install ${GODOT_VERSION} && \
       GODOT_SYMLINK=$(find /home/node -name "godot" -path "*/GodotEnv/*" -type l -o -name "godot" -path "*/GodotEnv/*" -type f 2>/dev/null | head -1) && \
       echo "GodotEnv binary at: $GODOT_SYMLINK"
   ```
   Use the discovered path for the ENV lines.

6. REMOVE the old `/usr/local/bin/godot` and `/usr/local/bin/GodotSharp` references. The
   GodotSharp directory is handled by GodotEnv automatically alongside each Godot version.

Modify `.devcontainer/devcontainer.json`:

7. Add `GODOT` to the `containerEnv` section so it is available in all terminal sessions:
   ```json
   "GODOT": "/home/node/.config/GodotEnv/godot/bin/godot"
   ```
   (Adjust path based on what was discovered in step 5.)

The key benefit: to switch Godot versions in the future, a developer can simply run
`godotenv godot install X.Y.Z && godotenv godot use X.Y.Z` without rebuilding the container.
  </action>
  <verify>
    <automated>docker build -f .devcontainer/Dockerfile -t godotenv-test . 2>&1 | tail -20 && docker run --rm godotenv-test godotenv godot list && docker run --rm godotenv-test godot --version --headless && docker rmi godotenv-test</automated>
  </verify>
  <done>
  - GodotEnv is installed and functional in the devcontainer image
  - `godotenv godot list` shows Godot 4.6.1 installed
  - `godot --version --headless` returns the correct version string
  - No manual wget/unzip Godot download block remains in Dockerfile
  - The GODOT environment variable points to the GodotEnv-managed binary
  </done>
</task>

</tasks>

<verification>
- Build the Docker image and verify GodotEnv is installed: `godotenv --version`
- Verify Godot is installed via GodotEnv: `godotenv godot list` shows 4.6.1
- Verify `godot` is on PATH and runs: `godot --version --headless`
- Verify GODOT env var is set correctly: `echo $GODOT`
- Verify `dotnet build` still succeeds against the GodotEnv-managed Godot
</verification>

<success_criteria>
- The devcontainer uses GodotEnv to install and manage Godot instead of manual download
- Godot 4.6.1 (.NET build) is installed via GodotEnv and accessible on PATH
- The GODOT environment variable points to the GodotEnv-managed binary
- Future version switches possible via `godotenv godot install/use` without container rebuild
</success_criteria>

<output>
After completion, create `.planning/quick/9-set-up-godotenv-in-devcontainer-for-inst/9-SUMMARY.md`
</output>
