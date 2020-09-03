using UnityEngine;
using System.Collections.Generic;
 
public static class HelperJunk
{
    public static bool CheckInRange(this GameObject self, MonoBehaviour target, float range)
    {
        return self.transform.position.CheckInRange(target.transform.position, range);
    }

    public static bool CheckInRange(this Vector3 self, Vector3 target, float range)
    {
        return self.DistanceSquared(target) <= range * range;
    }

    public static float DistanceSquared(this Vector3 self, Vector3 target)
    {
        return (self - target).sqrMagnitude;
    }

    /// <summary>
    /// Returns a Vector2 with the y component set to the Vector3's z component
    /// </summary>
    public static Vector2 ToV2(this Vector3 v)
    {
        return new Vector2(v.x, v.z);
    }

    /// <summary>
    /// Returns a Vector3 with the z component set to the Vector2's y component
    /// </summary>
    public static Vector3 ToV3(this Vector2 v)
    {
        return new Vector3(v.x, 0, v.y);
    }

    /// <summary>
    /// Returns a Vector3 with the z component set to the Vector2's y component
    /// </summary>
    public static Vector3 ToV3(this Vector2Int v)
    {
        return new Vector3(v.x, 0, v.y);
    }

    public static RectInt ToRectInt(this Transform t)
    {
        return new RectInt(
            (int)(t.position.x - (t.localScale.x / 2f)),
            (int)(t.position.z - (t.localScale.z / 2f)),
            Mathf.CeilToInt(t.localScale.x),
            Mathf.CeilToInt(t.localScale.z));
    }

    public static Bounds ToBounds(this Transform t)
    {
        return new Bounds(t.position, t.localScale);
    }

    public static Vector2 RotateRad(this Vector2 vector, float radians)
    {
        var sin = Mathf.Sin(radians);
        var cos = Mathf.Cos(radians);
        vector.Set(cos * vector.x - sin * vector.y, sin * vector.x + cos * vector.y);
        return vector;
    }

    public static Vector2 Rotate(this Vector2 vector, float degrees)
    {
        return RotateRad(vector, degrees * Mathf.Deg2Rad);
    }

    public static Vector2 RotateRad(this Vector2 vector, float radians, Vector2 pivot)
    {
        var sin = Mathf.Sin(radians);
        var cos = Mathf.Cos(radians);
        vector -= pivot;
        vector.Set(cos * vector.x - sin * vector.y, sin * vector.x + cos * vector.y);
        vector += pivot;
        return vector;
    }

    public static Vector2 Rotate(this Vector2 vector, float degrees, Vector2 pivot)
    {
        return RotateRad(vector, degrees * Mathf.Deg2Rad, pivot);
    }
}