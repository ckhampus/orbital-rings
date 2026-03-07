---
phase: quick-10
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - .github/workflows/devcontainer-publish.yml
autonomous: true
requirements: [QUICK-10]
must_haves:
  truths:
    - "Pushing to main triggers a workflow that builds the devcontainer image"
    - "The built image is published to ghcr.io under the repository namespace"
    - "Subsequent builds use the previously pushed image as a cache layer"
  artifacts:
    - path: ".github/workflows/devcontainer-publish.yml"
      provides: "GitHub Actions workflow for devcontainer build and publish"
  key_links:
    - from: ".github/workflows/devcontainer-publish.yml"
      to: ".devcontainer/devcontainer.json"
      via: "devcontainers/ci action reads devcontainer config"
      pattern: "devcontainers/ci"
---

<objective>
Create a GitHub Actions workflow that builds the devcontainer image using the
devcontainers/ci action and publishes it to GitHub Container Registry (ghcr.io).

Purpose: Automate devcontainer image builds so contributors can pull a pre-built
image instead of building from scratch, and ensure the image stays current with
Dockerfile changes.

Output: `.github/workflows/devcontainer-publish.yml`
</objective>

<execution_context>
@/home/node/.claude/get-shit-done/workflows/execute-plan.md
@/home/node/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.devcontainer/devcontainer.json
@.devcontainer/Dockerfile
</context>

<tasks>

<task type="auto">
  <name>Task 1: Create devcontainer build and publish workflow</name>
  <files>.github/workflows/devcontainer-publish.yml</files>
  <action>
Create `.github/workflows/devcontainer-publish.yml` with the following specification:

**Trigger:** Push to `main` branch, but ONLY when files under `.devcontainer/` are changed
(use `paths` filter: `.devcontainer/**`). This avoids unnecessary builds on unrelated commits.

**Job: `build-and-push`**
- Runs on: `ubuntu-latest`
- Permissions: `packages: write`, `contents: read` (required for ghcr.io push and checkout)

**Steps:**

1. **Checkout** — `actions/checkout@v4`

2. **Login to GHCR** — `docker/login-action@v3`
   - registry: `ghcr.io`
   - username: `${{ github.repository_owner }}`
   - password: `${{ secrets.GITHUB_TOKEN }}`

3. **Build and push devcontainer** — `devcontainers/ci@v0.3`
   - `imageName`: `ghcr.io/${{ github.repository }}` (produces `ghcr.io/ckhampus/orbital-rings`)
   - `imageTag`: Use a composite tag strategy — both `latest` and the short SHA:
     `latest,${{ github.sha }}`
   - `cacheFrom`: `ghcr.io/${{ github.repository }}` (pulls the previous latest for layer caching)
   - `push`: `always`

Do NOT include a `runCmd` — this workflow is purely for building and publishing
the image, not for running tests inside it.

Use workflow_dispatch as an additional trigger so the workflow can be run manually
from the Actions tab for initial bootstrapping.

The full SHA tag (not short) is fine since GitHub Actions provides `github.sha`
directly. This gives immutable references to specific builds.
  </action>
  <verify>
    <automated>cat .github/workflows/devcontainer-publish.yml && echo "---YAML-LINT---" && python3 -c "import yaml; yaml.safe_load(open('.github/workflows/devcontainer-publish.yml'))" 2>&1 && echo "YAML is valid"</automated>
  </verify>
  <done>
    - Workflow file exists at `.github/workflows/devcontainer-publish.yml`
    - YAML is syntactically valid
    - Triggers on push to main with `.devcontainer/**` path filter
    - Triggers on workflow_dispatch for manual runs
    - Logs into ghcr.io using GITHUB_TOKEN
    - Uses devcontainers/ci@v0.3 to build and push
    - Image tagged with both `latest` and commit SHA
    - Cache-from references the same image for layer reuse
    - Has correct permissions (packages:write, contents:read)
  </done>
</task>

</tasks>

<verification>
- Workflow YAML parses without errors
- All required fields (name, on, jobs, steps) are present
- devcontainers/ci action is configured with imageName, imageTag, cacheFrom, push
- GHCR login step precedes the build step
- Path filter limits builds to devcontainer changes only
</verification>

<success_criteria>
A valid GitHub Actions workflow exists that will, on push to main affecting
`.devcontainer/**`, build the devcontainer image and publish it to
`ghcr.io/ckhampus/orbital-rings` with `latest` and SHA tags.
</success_criteria>

<output>
After completion, create `.planning/quick/10-github-workflow-that-builds-and-publishe/10-SUMMARY.md`
</output>
