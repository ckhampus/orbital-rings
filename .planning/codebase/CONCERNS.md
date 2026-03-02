# Codebase Concerns

**Analysis Date:** 2026-03-02

## Project Stage Assessment

**Current State:** Pre-development/Setup Phase

This is an early-stage Godot 4.6 C# game project with only configuration and game design documentation. No core gameplay code, systems, or features have been implemented. The project consists entirely of:
- Godot project configuration (`project.godot`)
- C# project files (`Orbital Rings.csproj`, `Orbital Rings.sln`)
- Development environment setup (devcontainer, Dockerfile)
- Game design specification (IDEA.md)

**Implication:** Most traditional technical debt concerns do not yet exist. However, the following are architectural and setup concerns to address before implementation begins.

---

## Critical Setup Concerns

**Development Environment Configuration:**
- Files: `.devcontainer/Dockerfile`, `.devcontainer/devcontainer.json`
- Issue: The devcontainer installs .NET 10 SDK but there is no verification that Godot 4.6 has been installed or is accessible in the container. The project references `Godot.NET.Sdk/4.6.1` in the C# project file, but the Dockerfile does not explicitly install Godot.
- Impact: Build failures when trying to compile C# code or run the project from within the devcontainer. Developers may be unable to build locally without manual Godot installation.
- Fix approach: Add explicit Godot 4.6 installation to the Dockerfile (either via package manager or direct download), or document that Godot must be installed separately on the host. Verify the `Godot.NET.Sdk` package is available via NuGet during project restore.

**Missing Claude Code Integration:**
- Files: `.devcontainer/devcontainer.json`
- Issue: Line 55 runs `get-shit-done-cc@latest --claude --global` which appears to be an npm package, but there is no documentation on what this does or whether it conflicts with the Godot/C# development setup. The purpose is unclear.
- Impact: Unknown side effects on the development environment. Risk of broken or unintended tooling configuration.
- Fix approach: Document the purpose of `get-shit-done-cc` in a README or development setup guide. Verify it is compatible with Godot C# development.

---

## Architecture & Design Concerns

**Ring Segment Placement Logic Not Designed:**
- Issue: The game design (IDEA.md) specifies that rooms can occupy 1-3 adjacent segments along a ring, but there is no documented algorithm or architecture for:
  - Detecting available consecutive segments
  - Validating placement legality
  - Handling room removal and segment reclamation
  - Tracking inner vs. outer segment occupancy
- Impact: Implementation may be ad-hoc, leading to bugs in placement validation and difficulty extending the system later.
- Fix approach: Before implementing placement logic, design and document a segment allocation system. Consider using a bitmask or array-based approach for fast queries and validation. Store this design in a separate architecture document.

**Procedural Generation System Not Defined:**
- Issue: IDEA.md describes procedural room interiors (furniture layout, detail variation, color/material variation) but provides no technical specification for:
  - Procedural generation parameters (seed storage, randomization approach)
  - Asset pool structure (what furniture is valid for each room type)
  - Generation constraints (how to ensure layouts fit room dimensions)
  - Caching strategy (whether generated rooms are saved or regenerated on load)
- Impact: Risk of non-deterministic behavior, inconsistent visuals, or performance issues if generation is slow.
- Fix approach: Create a detailed procedural generation spec document. Prototype furniture layout generation early. Decide on seed-based generation for consistency.

**Citizen Simulation Not Architected:**
- Issue: IDEA.md describes citizen behaviors (daily routines, traits, wishes, friendships) but no system design exists for:
  - Citizen state machine (routing along walkway, room occupancy, wish generation)
  - Persistence of citizen data across save/load
  - Wish fulfillment detection (how does the game know a wish was satisfied)
  - Friendship relationship storage and updates
  - Scalability (how many citizens can the simulation handle)
- Impact: Complex interlocking systems will be difficult to implement correctly without upfront design.
- Fix approach: Design citizen behavior systems before implementation. Create state diagrams for citizen AI. Document data structures for traits, wishes, and relationships.

**Game State Serialization Not Considered:**
- Issue: No save/load system is designed, yet the game design implies persistence:
  - Citizens persist across sessions
  - Room placements must be saved
  - Happiness, credits, and progression state must survive
- Impact: Save/load implementation late in development risks architectural changes and data loss bugs.
- Fix approach: Design a save format early (JSON, binary, database). Define what game state must persist. Plan for version migration if the save format changes.

---

## Technical Debt Seeds

**C# Version and .NET Target Mismatch Potential:**
- Files: `Orbital Rings.csproj`
- Issue: The project targets `net8.0` but switches to `net9.0` for Android. However, the Dockerfile installs .NET 10. This version spread could cause platform-specific issues or dependency conflicts.
- Impact: Package restore or runtime failures if dependencies are .NET 10-specific but the project targets net8.0.
- Fix approach: Align .NET versions. Either pin to net8.0 for consistency or document why different targets are needed. Verify all target frameworks are installed in devcontainer.

**Dynamic Loading Enabled:**
- Files: `Orbital Rings.csproj`
- Issue: `<EnableDynamicLoading>true</EnableDynamicLoading>` is set but dynamic loading is not a documented requirement. This flag disables AOT optimization.
- Impact: Potential performance overhead, especially on resource-constrained platforms. Makes the codebase harder to optimize later.
- Fix approach: Assess whether dynamic loading is actually needed (likely yes for Godot plugins). If not needed, disable it. Document why it is enabled if kept.

**No Testing Infrastructure:**
- Issue: No test framework, test configuration files, or test examples exist. The project is setup-only without any testable code yet.
- Impact: As gameplay systems grow, lack of test infrastructure will slow development and reduce confidence in refactoring.
- Fix approach: Plan for testing early. Add xUnit, NUnit, or similar C# test framework to the project setup. Create test project file and CI configuration before core systems are built.

---

## Missing Documentation

**No Development Setup Guide:**
- Issue: No README explaining how to clone, build, and run the project locally or in the devcontainer.
- Impact: New developers or future team members will struggle to get started.
- Fix approach: Create a README with clear steps for environment setup, building, and running the game.

**No Coding Standards Document:**
- Issue: No C# style guide, naming conventions, or architecture guidelines are documented.
- Impact: As developers write code, style will diverge. Refactoring and code review will be slower.
- Fix approach: Create CONVENTIONS.md documenting C# naming conventions, file structure, error handling patterns, and any Godot-specific practices.

**No Godot C# Integration Guide:**
- Issue: No documentation of how to structure C# code in a Godot project, how to access Godot APIs, or how to organize nodes and scripts.
- Impact: Implementation may use anti-patterns that conflict with Godot's design (e.g., tight coupling of logic to nodes).
- Fix approach: Create architecture documentation covering Godot scene-to-C# mapping, signal/event handling, and scene instantiation patterns.

---

## Scalability Concerns (Forward-Looking)

**Citizen Count Scaling:**
- Issue: IDEA.md does not specify expected citizen counts or performance targets. The citizen simulation (routing, wish generation, friendship tracking) could become slow with hundreds of citizens.
- Impact: Unclear if simulation can handle an ambitious player station or if there will be hard caps.
- Fix approach: Early prototyping of citizen simulation. Benchmark with target population counts (e.g., 100, 500, 1000 citizens). Plan optimization strategy (object pooling, quadtree spatial indexing for walkway interactions).

**Ring and Room Complexity:**
- Issue: Vertical ring stacking is allowed, but no limits are specified. A station with 20 rings and 24 segments per ring is 480 segments total. No complexity analysis for rendering or pathfinding at scale.
- Impact: Late-game lag or crashed performance if rendering or room management isn't optimized.
- Fix approach: Define maximum ring count or implement level-of-detail (LOD) rendering early. Prototype rendering performance with large ring stacks.

---

## Security & Permissions

**Secrets File Permissions Defined but Incomplete:**
- Files: `.claude/settings.local.json`
- Issue: Permission restrictions are defined for `.env`, `.pem`, `.key`, and other sensitive files, but there is no `.env.example` or documentation of what environment variables are required.
- Impact: If the project later uses external APIs or database connections, developers won't know which secrets must be configured.
- Fix approach: Create `.env.example` with placeholder environment variables. Document all required external integrations (if any) in a setup guide.

---

## Known Issues

None identified at this stage. This is a configuration-only project without implemented features or runtime behavior.

---

## Recommendations for Phase 1 (Pre-Implementation)

**High Priority:**
1. Fix devcontainer Godot installation issue (verify SDK is available)
2. Design and document segment placement algorithm
3. Create save/load system architecture
4. Add testing framework to project setup

**Medium Priority:**
5. Design citizen simulation state machine
6. Create procedural generation specification
7. Write development setup README
8. Add Godot C# coding conventions document

**Low Priority (Pre-release):**
9. Performance audit for ring/citizen scaling
10. Optimize rendering for large ring stacks
11. Plan accessibility features

---

*Concerns audit: 2026-03-02*
