using System.Collections.Generic;
using UnityEngine;

//simple boids implementation to test quadtrees with. Not especially interesting or optimized!
public abstract class BoidBase : MonoBehaviour
{
    public static RectInt bounds;
    public static BoidSettingsTemp sets;

    public int index_TEMP;  //TODO: Is this really temp????
    public Vector2 pos;
    public Vector2 facing;
    public Vector2 dir;
    public Vector2 avoid;
    public Vector2 align;
    public Vector2 adjoin;
    public List<BoidBase> flock;
    public Transform tf;

    public virtual void Init()
    {
        tf = transform;
        pos = tf.position.ToV2();
        dir = Random.insideUnitCircle.normalized;
        facing = dir;
    }

    protected virtual void Update()
    {
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
        dir += avoid * sets.avoidStrength;
        dir += align.normalized * sets.alignStrength;
        dir += adjoin * sets.adjoinStrength;
        dir.Normalize();

        //This is a bit weird, just doing it this way to be consistent with the other code
        float maxTurn = sets.turnSpeed * 360 * Time.deltaTime;

        float d = Mathf.Clamp(Vector2.SignedAngle(facing, dir), -maxTurn, maxTurn);
        facing = facing.Rotate(d);

        tf.forward = facing.ToV3();
    }

    protected virtual void Move()
    {
        UnityEngine.Profiling.Profiler.BeginSample("Move");
        pos += facing * (sets.speed * Time.deltaTime);
        tf.position = pos.ToV3();
        UpdatePosition();
        UnityEngine.Profiling.Profiler.EndSample();
    }

    protected abstract void UpdatePosition();

    protected abstract void FindFlockmates();

    protected virtual void DoBoidThing()
    {
        avoid.Set(0, 0);
        align.Set(0, 0);
        adjoin.Set(0, 0);

        for (int i = flock.Count - 1; i >= 0; --i)
        {
            BoidBase nextBoid = flock[i];
            
            Vector2 neighbor = pos - nextBoid.pos;
            avoid += neighbor / neighbor.sqrMagnitude;
            
            align += nextBoid.dir;
            
            adjoin += nextBoid.pos;
        }
        
        if (flock.Count > 0)
        {
            adjoin /= flock.Count;
            adjoin -= pos;
        }
    }
    
    protected virtual bool AvoidBounds()
    {
        if (pos.x < 0 || pos.x > bounds.width || pos.y < 0 || pos.y > bounds.height)
        {
            dir = new Vector2(bounds.height / 2, bounds.width / 2) - pos;
            return true;
        }
        else
            return false;
    }
}