using UnityEngine;
using DragonSpace.Structs;

public static class GizmoHelp
{
    public static void DrawAABB(Color c, AABB box)
    {
        DrawAABB(c, box.lft, box.btm, box.rgt, box.top);
    }

    public static void DrawAABB(Color c, float lft, float btm, float rgt, float top)
    {
        Gizmos.color = c;
        Gizmos.DrawLine(
            new Vector3(lft, 0, btm),
            new Vector3(lft, 0, top));
        Gizmos.DrawLine(
            new Vector3(lft, 0, top),
            new Vector3(rgt, 0, top));
        Gizmos.DrawLine(
            new Vector3(rgt, 0, btm),
            new Vector3(rgt, 0, top));
        Gizmos.DrawLine(
            new Vector3(lft, 0, btm),
            new Vector3(rgt, 0, btm));
    }
}
