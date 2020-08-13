using UnityEngine;

//This should really be a ScriptableObject
public class BoidSettingsTemp : MonoBehaviour
{
    [Range(0,100)]
    public int speed;
    [Range(0, 100)]
    public int radius;
    [Range(0.1f, 1f)]
    public float turnSpeed;

    [Range(0f, 1f)]
    public float avoidStrength;
    [Range(0f, 1f)]
    public float alignStrength;
    [Range(0f, 0.2f)]
    public float adjoinStrength;
}
