using System.Collections.Generic;
using UnityEngine;

//simple boids implementation to test quadtrees with. Not especially interesting or optimized!
public abstract class BoidBase : MonoBehaviour
{
    public static RectInt bounds;
    public static BoidSettingsTemp sets;

    public int treeIndex;
    public Vector3 pos;
    public Vector3 facing;
    public Vector2 dir;
    public Vector3 avoid;
    public Vector2 align;
    public Vector3 adjoin;
    public List<BoidBase> flock;

    public virtual void Init()
    {
        pos = transform.position;
        dir = Random.insideUnitCircle.normalized;
        facing = dir.ToV3();
    }

    protected virtual void Update()
    {
        pos = transform.position;
        if (!AvoidBounds())
        {
            UnityEngine.Profiling.Profiler.BeginSample("Find Flockmates");
            FindFlockmates();
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("Update boid direction");
            DoBoidThing();
            UnityEngine.Profiling.Profiler.EndSample();
        }
        Steer();
        Move();
    }

    protected virtual void Steer()
    {
        dir += avoid.ToV2() * sets.avoidStrength;
        dir += align.normalized * sets.alignStrength;
        dir += adjoin.ToV2() * sets.adjoinStrength;
        AvoidBounds();
        dir.Normalize();
        facing = Vector3.RotateTowards(facing, dir.ToV3(), sets.turnSpeed * (2 * Mathf.PI) * Time.deltaTime, 0);
        transform.rotation = Quaternion.LookRotation(facing, Vector3.back);
    }

    protected virtual void Move()
    {
        transform.Translate(Vector3.forward * sets.speed * Time.deltaTime);
        pos = transform.position;
        UpdatePosition();
    }

    protected abstract void UpdatePosition();

    protected abstract void FindFlockmates();

    protected virtual void DoBoidThing()
    {
        avoid.Set(0, 0, 0);
        align.Set(0, 0);
        adjoin.Set(0, 0, 0);

        for (int i = flock.Count - 1; i >= 0; --i)
        {
            BoidBase nextBoid = flock[i];

            UnityEngine.Profiling.Profiler.BeginSample("Avoid");
            Vector3 neighbor = pos - nextBoid.pos;
            avoid += neighbor / neighbor.sqrMagnitude;
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("Align");
            align += nextBoid.dir;
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("Adjoin");
            adjoin += nextBoid.pos;
            UnityEngine.Profiling.Profiler.EndSample();
        }

        UnityEngine.Profiling.Profiler.BeginSample("Adjoin");
        if (flock.Count > 0)
        {
            adjoin /= flock.Count;
            adjoin -= pos;
        }
        UnityEngine.Profiling.Profiler.EndSample();
    }
    
    protected virtual bool AvoidBounds()
    {
        if (pos.x < 0 || pos.x > bounds.width || pos.z < 0 || pos.z > bounds.height)
        {
            dir = new Vector2(bounds.height / 2, bounds.width / 2) - pos.ToV2();
            return true;
        }
        else
            return false;
    }
}