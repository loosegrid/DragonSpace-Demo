using UnityEngine;
 
public static class MiscHelp
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
}