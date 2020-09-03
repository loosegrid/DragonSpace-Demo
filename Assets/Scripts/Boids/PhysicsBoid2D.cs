using System.Collections.Generic;
using UnityEngine;

public class PhysicsBoid2D : MonoBehaviour
{
    public static RectInt bounds;
    public static BoidSettingsTemp sets;

    public Vector2 pos;
    public Vector2 facing;
    public Vector2 dir;
    public Vector2 avoid;
    public Vector2 align;
    public Vector2 adjoin;

    List<PhysicsBoid2D> flock = new List<PhysicsBoid2D>(20);
    Rigidbody2D rb;

    private void Start()
    {
        pos = transform.position;
        dir = Random.insideUnitCircle.normalized;
        facing = dir;
        rb = GetComponent<Rigidbody2D>();
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
            UnityEngine.Profiling.Profiler.EndSample();
        }
        Steer();
        Move();
    }

    void Steer()
    {
        dir += avoid * sets.avoidStrength;
        dir += align.normalized * sets.alignStrength;
        dir += adjoin * sets.adjoinStrength;
        dir.Normalize();

        //This is a bit weird, just doing it this way to be consistent with the other code
        float maxTurn = sets.turnSpeed * 360 * Time.deltaTime;

        float d = Mathf.Clamp(Vector2.SignedAngle(facing, dir), -maxTurn, maxTurn);
        facing = facing.Rotate(d);
        transform.right = facing;
    }

    void Move()
    {
        pos += facing * (sets.speed * Time.deltaTime);
        rb.position = pos;
    }

    //slightly tough to compare "fairly" because the other structs will return as many
    //boids as are found, but OverlapSphereNonAlloc is limited by the array size
    //that said, I don't think anything over 200 will make a noticeable difference,
    //if the flock even reaches that many
    public static Collider2D[] hits = new Collider2D[200];
    protected void FindFlockmates()
    {
        UnityEngine.Profiling.Profiler.BeginSample("Find Flockmates");
        int h = Physics2D.OverlapCircleNonAlloc(
            pos + facing * 8, sets.radius, hits);
        flock.Clear();
        for (int i = 0; i < h; i++)
        {
            if(hits[i].transform == transform) { continue; }
            flock.Add(hits[i].GetComponent<PhysicsBoid2D>());
        }
        UnityEngine.Profiling.Profiler.EndSample();
    }

    void AvoidFlockmates()
    {
        avoid.Set(0, 0);
        for (int i = flock.Count - 1; i >= 0; --i)
        {
            Vector2 neighbor = pos - flock[i].pos;
            avoid += neighbor / neighbor.sqrMagnitude;
        }
    }

    void AlignWithFlock()
    {
        align.Set(0, 0);
        for (int i = flock.Count - 1; i >= 0; --i)
        {
            align += flock[i].dir;
        }
    }

    void MoveTowardFlock()
    {
        adjoin.Set(0, 0);
        for (int i = flock.Count - 1; i >= 0; --i)
        {
            adjoin += flock[i].pos;
        }
        if (flock.Count > 0)
        {
            adjoin /= flock.Count;
            adjoin -= pos;
        }
    }
    
    bool AvoidBounds()
    {
        if (pos.x < 0 || pos.x > bounds.width || pos.y < 0 || pos.y > bounds.height)
        {
            dir = new Vector2(bounds.height / 2, bounds.width / 2) - pos;
            return true;
        }
        else
            return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawLine(pos, pos + facing * 8);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(pos, pos + dir * 8);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(pos, pos + avoid * 8);
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(pos, pos + adjoin.normalized * 8);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(pos, pos + align.normalized * 8);

        Gizmos.color = Color.yellow;
        //draw detection radius
        Gizmos.DrawWireSphere(pos + facing * 8, sets.radius);
        //show flockmates
        for (int i = 0; i < flock.Count; i++)
        {
            Gizmos.DrawLine(pos, flock[i].pos);
        }
    }
}