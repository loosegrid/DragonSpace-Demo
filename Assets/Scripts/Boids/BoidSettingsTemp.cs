using UnityEngine;

//I guess it wasn't temp after all
[CreateAssetMenu(fileName = "BoidSettings", menuName = "BoidSettings")]
public class BoidSettingsTemp : ScriptableObject
{
    [Header("Test config")]
    public bool showFPS;
    public QtTestType type;
    public RectInt bounds;
    public int testElements;
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
