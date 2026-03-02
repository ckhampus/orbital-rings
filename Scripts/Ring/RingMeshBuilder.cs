using Godot;

namespace OrbitalRings.Ring;

/// <summary>
/// Static helper that generates annular sector (ring segment) meshes via SurfaceTool.
/// Each mesh is a closed 3D shape with top, bottom, outer, inner, and two radial side faces.
/// </summary>
public static class RingMeshBuilder
{
    /// <summary>
    /// Creates a closed annular sector mesh (a "pizza slice of a donut").
    /// </summary>
    /// <param name="innerR">Inner radius of the sector.</param>
    /// <param name="outerR">Outer radius of the sector.</param>
    /// <param name="startAngle">Start angle in radians.</param>
    /// <param name="endAngle">End angle in radians.</param>
    /// <param name="height">Total height of the sector.</param>
    /// <param name="subdivisions">Number of arc subdivisions for smoothness.</param>
    /// <returns>A complete ArrayMesh with normals set for all faces.</returns>
    public static ArrayMesh CreateAnnularSector(
        float innerR, float outerR,
        float startAngle, float endAngle,
        float height, int subdivisions = 4)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        float halfH = height * 0.5f;

        for (int i = 0; i < subdivisions; i++)
        {
            float t0 = (float)i / subdivisions;
            float t1 = (float)(i + 1) / subdivisions;
            float a0 = Mathf.Lerp(startAngle, endAngle, t0);
            float a1 = Mathf.Lerp(startAngle, endAngle, t1);

            float cosA0 = Mathf.Cos(a0);
            float sinA0 = Mathf.Sin(a0);
            float cosA1 = Mathf.Cos(a1);
            float sinA1 = Mathf.Sin(a1);

            // --- Top face (Y = +halfH), CCW winding viewed from above ---
            Vector3 tOA = new(outerR * cosA0, halfH, outerR * sinA0);
            Vector3 tOB = new(outerR * cosA1, halfH, outerR * sinA1);
            Vector3 tIA = new(innerR * cosA0, halfH, innerR * sinA0);
            Vector3 tIB = new(innerR * cosA1, halfH, innerR * sinA1);

            AddQuad(st, tIA, tOA, tOB, tIB, Vector3.Up);

            // --- Bottom face (Y = -halfH), reversed winding ---
            Vector3 bOA = new(outerR * cosA0, -halfH, outerR * sinA0);
            Vector3 bOB = new(outerR * cosA1, -halfH, outerR * sinA1);
            Vector3 bIA = new(innerR * cosA0, -halfH, innerR * sinA0);
            Vector3 bIB = new(innerR * cosA1, -halfH, innerR * sinA1);

            AddQuad(st, bOA, bIA, bIB, bOB, Vector3.Down);

            // --- Outer curved face (facing outward radially) ---
            Vector3 outerNormalA = new(cosA0, 0, sinA0);
            Vector3 outerNormalB = new(cosA1, 0, sinA1);
            Vector3 outerNormalMid = ((outerNormalA + outerNormalB) * 0.5f).Normalized();

            AddQuad(st, tOA, tOB, bOB, bOA, outerNormalMid);

            // --- Inner curved face (facing inward radially) ---
            Vector3 innerNormalA = new(-cosA0, 0, -sinA0);
            Vector3 innerNormalB = new(-cosA1, 0, -sinA1);
            Vector3 innerNormalMid = ((innerNormalA + innerNormalB) * 0.5f).Normalized();

            AddQuad(st, tIB, tIA, bIA, bIB, innerNormalMid);
        }

        // --- Start radial side face (at startAngle) ---
        {
            float cosS = Mathf.Cos(startAngle);
            float sinS = Mathf.Sin(startAngle);
            // Normal perpendicular to the radial edge, pointing "backward" (CCW)
            Vector3 sideNormal = new(-sinS, 0, cosS);

            Vector3 sTO = new(outerR * cosS, halfH, outerR * sinS);
            Vector3 sTI = new(innerR * cosS, halfH, innerR * sinS);
            Vector3 sBO = new(outerR * cosS, -halfH, outerR * sinS);
            Vector3 sBI = new(innerR * cosS, -halfH, innerR * sinS);

            AddQuad(st, sTI, sTO, sBO, sBI, -sideNormal);
        }

        // --- End radial side face (at endAngle) ---
        {
            float cosE = Mathf.Cos(endAngle);
            float sinE = Mathf.Sin(endAngle);
            // Normal perpendicular to the radial edge, pointing "forward" (CCW)
            Vector3 sideNormal = new(-sinE, 0, cosE);

            Vector3 eTO = new(outerR * cosE, halfH, outerR * sinE);
            Vector3 eTI = new(innerR * cosE, halfH, innerR * sinE);
            Vector3 eBO = new(outerR * cosE, -halfH, outerR * sinE);
            Vector3 eBI = new(innerR * cosE, -halfH, innerR * sinE);

            AddQuad(st, eTO, eTI, eBI, eBO, sideNormal);
        }

        return st.Commit();
    }

    /// <summary>
    /// Adds a quad (two triangles) to the SurfaceTool.
    /// Vertices a, b, c, d should be in CCW order when viewed from the normal direction.
    /// Triangle 1: A, B, C. Triangle 2: A, C, D.
    /// </summary>
    private static void AddQuad(SurfaceTool st, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
    {
        // Triangle 1: A, B, C
        st.SetNormal(normal);
        st.AddVertex(a);
        st.SetNormal(normal);
        st.AddVertex(b);
        st.SetNormal(normal);
        st.AddVertex(c);

        // Triangle 2: A, C, D
        st.SetNormal(normal);
        st.AddVertex(a);
        st.SetNormal(normal);
        st.AddVertex(c);
        st.SetNormal(normal);
        st.AddVertex(d);
    }
}
