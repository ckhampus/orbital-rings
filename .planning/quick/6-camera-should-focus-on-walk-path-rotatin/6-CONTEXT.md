# Quick Task 6: Camera should focus on walk path, rotating along the path instead of around a fixed center - Context

**Gathered:** 2026-03-06
**Status:** Ready for planning

<domain>
## Task Boundary

Camera should focus on walk path, rotating along the path instead of around a fixed center

</domain>

<decisions>
## Implementation Decisions

### Focus Point Behavior
- Camera looks at the walkway centerline (radius 4.5) at the current orbit angle
- As user orbits, the look-at point slides along the walkway circle
- No citizen tracking — pure geometric focus on walkway centerline

### Zoom Behavior
- Always walkway-focused at all zoom levels
- Even at max zoom-out, the camera looks at the walkway point — off-center perspective
- No blending back to origin

### Orbit Feel
- Camera rig physically translates to follow the walkway circle (radius 4.5)
- Orbiting moves the camera along the path, like walking the ring
- Camera always looks at the walkway point from its current position on the circle

### Claude's Discretion
- Idle auto-orbit behavior adaptation (should continue working with new pivot)
- Tilt behavior (should work naturally with walkway-centered pivot)

</decisions>

<specifics>
## Specific Ideas

- Walkway centerline radius is 4.5 (midpoint of InnerRowOuter=4.0 and OuterRowInner=5.0)
- Current camera uses spherical coordinates from origin — needs to shift to spherical from walkway point
- CameraRig Node3D position should move to walkway circle, not stay at origin

</specifics>
