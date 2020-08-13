using UnityEngine;

public class PhysicsBoid : BoidBase
{
    private void Awake()
    {
        Init();
    }

    public override void Init()
    {
        base.Init();
    }

    //update called by base

    //slightly tough to compare "fairly" because the other structs will return as many
    //boids as are found, but OverlapSphereNonAlloc is limited by the array size
    //that said, I don't think anything over 200 will make a noticeable difference,
    //if the flock even reaches that many
    public static Collider[] hits = new Collider[200];
    protected override void FindFlockmates()
    {
        UnityEngine.Profiling.Profiler.BeginSample("Find Flockmates");
        int h = Physics.OverlapSphereNonAlloc(
            pos + facing * 8, sets.radius, hits, 1, QueryTriggerInteraction.Collide);
        flock.Clear();
        for (int i = 0; i < h; i++)
        {
            if(hits[i].transform == transform) { continue; }
            flock.Add(hits[i].GetComponent<PhysicsBoid>());
        }
        UnityEngine.Profiling.Profiler.EndSample();
    }

    protected override void UpdatePosition()
    {
        //don't need to do anything for physics
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(pos, pos + avoid * 8);
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(pos, pos + adjoin.normalized * 8);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(pos, pos + align.ToV3().normalized * 8);

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