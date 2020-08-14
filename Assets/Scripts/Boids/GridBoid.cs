using System.Collections.Generic;
using UnityEngine;
using DragonSpace.Grids;

//simple boids implementation to test quadtrees with. Not especially interesting or optimized!
public class GridBoid : MonoBehaviour, IGridElt
{
    #region Grid element implementation
    public float Xf => pos.x - halfWidth;  //This is a decent argument for using center pt
    public float Yf => pos.z - halfHeight;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public IGridElt NextElt { get; set; } = null;

    Transform tf;
    public int ID { get; set; }
    public float halfWidth;   
    public float halfHeight;
    #endregion

    public static LooseDoubleGrid ugrid;
    public static RectInt bounds;
    public static BoidSettingsTemp sets;
    
    public Vector3 pos;
    public Vector3 facing;
    public Vector2 dir;
    public Vector3 avoid;
    public Vector2 align;
    public Vector3 adjoin;
    List<IGridElt> flock;

    private void Awake()
    {
        tf = transform;
        pos = tf.position;
        dir = Random.insideUnitCircle.normalized;
        facing = dir.ToV3();

        Width = (int)tf.localScale.x;
        Height = (int)tf.localScale.z;
        halfWidth = tf.localScale.x / 2;
        halfHeight = tf.localScale.z / 2;
    }

    void Update()
    {
        if (!AvoidBounds())
        {
            FindFlockmates();
            UnityEngine.Profiling.Profiler.BeginSample("Update boid direction");
            AvoidFlockmates();
            AlignWithFlock();
            MoveTowardFlock();
            Steer();
            UnityEngine.Profiling.Profiler.EndSample();
        }
        Move();
    }

    void Steer()
    {
        dir += avoid.ToV2() * sets.avoidStrength;
        dir += align.normalized * sets.alignStrength;
        dir += adjoin.ToV2() * sets.adjoinStrength;
        dir.Normalize();
        facing = Vector3.RotateTowards(facing, dir.ToV3(), sets.turnSpeed * (2 * Mathf.PI) * Time.deltaTime, 0);
        transform.rotation = Quaternion.LookRotation(facing, Vector3.back);
    }

    void Move()
    {
        UnityEngine.Profiling.Profiler.BeginSample("Move");
        float oldX = Xf;
        float oldY = Yf;
        tf.position += facing * sets.speed * Time.deltaTime;
        pos = tf.position;  //update pos after moving in the grid!
        UnityEngine.Profiling.Profiler.EndSample();
        ugrid.Move((IGridElt)this, oldX, oldY, Xf, Yf);
    }

    void FindFlockmates()
    {
        UnityEngine.Profiling.Profiler.BeginSample("Find Flockmates");
        int r = sets.radius;
        float x = Xf + facing.x * 8;
        float y = Yf + facing.z * 8;
        //adding 2 here so the radius is from the bounding box's edges
        flock = ugrid.Query(x - r, y - r, x + 2 + r, y + 2 + r, ID); //TODO: variable sizes (not 2)
        UnityEngine.Profiling.Profiler.EndSample();
    }

    void AvoidFlockmates()
    {
        avoid.Set(0, 0, 0);
        for (int i = flock.Count - 1; i >= 0; --i)
        {
            Vector3 neighbor = pos - ((GridBoid)flock[i]).pos;
            avoid += neighbor / neighbor.sqrMagnitude;
        }
    }

    void AlignWithFlock()
    {
        align.Set(0, 0);
        for (int i = flock.Count - 1; i >= 0; --i)
        {
            align += ((GridBoid)flock[i]).dir;
        }
    }

    void MoveTowardFlock()
    {
        adjoin.Set(0, 0, 0);
        for (int i = flock.Count - 1; i >= 0; --i)
        {
            adjoin += ((GridBoid)flock[i]).pos;
        }
        if (flock.Count > 0)
        {
            adjoin /= flock.Count;
            adjoin -= pos;
        }
    }
    
    bool AvoidBounds()
    {
        if (pos.x < 0 || pos.x > bounds.width || pos.z < 0 || pos.z > bounds.height)
        {
            dir = new Vector2(bounds.height / 2, bounds.width / 2) - pos.ToV2();
            facing = Vector3.RotateTowards(facing, dir.ToV3(), sets.turnSpeed * (2 * Mathf.PI) * Time.deltaTime, 0);
            transform.rotation = Quaternion.LookRotation(facing, Vector3.back);
            return true;
        }
        else
            return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(pos, pos + avoid * 8);
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(pos, pos + adjoin.normalized * 8);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(pos, pos + align.ToV3().normalized * 8);

        //show flockmates
        Gizmos.color = Color.yellow;
        int r = sets.radius;
        float x = Xf + facing.x * 8;
        float y = Yf + facing.z * 8;
        flock = ugrid.Query(x - r, y - r, x + 2 + r, y + 2 + r, ID); //TODO: variable sizes (not 2)
        for (int i = 0; i < flock.Count; ++i)
        {
            Gizmos.DrawLine(pos, ((GridBoid)flock[i]).transform.position);
        }

        //draw detection "radius"
        Gizmos.color = new Color(1, 0, 1, 1f);
        Gizmos.DrawLine(new Vector3(x - r, 0, y - r), new Vector3(x - r, 0, y + 2 + r)); //left
        Gizmos.DrawLine(new Vector3(x - r, 0, y + 2 + r), new Vector3(x + 2 + r, 0, y + 2 + r)); //top
        Gizmos.DrawLine(new Vector3(x + 2 + r, 0, y + 2 + r), new Vector3(x + 2 + r, 0, y - r)); //right
        Gizmos.DrawLine(new Vector3(x + 2 + r, 0, y - r), new Vector3(x - r, 0, y - r)); //bottom
                
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(pos, pos + (dir.normalized.ToV3() * 4));
        Gizmos.color = Color.white;
        Gizmos.DrawLine(pos, pos + (facing * 4));

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(new Vector3(Xf, 0, Yf), 0.25f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(new Vector3(Xf+2, 0, Yf+2), 0.25f);
    }
}