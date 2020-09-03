using UnityEngine;

public class PhysicsBoid : BoidBase
{
    Collider col;
    private void Awake()
    {
        Init();
        col = GetComponent<Collider>();
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
            (pos + facing * 8).ToV3(), sets.radius, hits, 1, QueryTriggerInteraction.Collide);
        flock.Clear();
        for (int i = 0; i < h; i++)
        {
            if(hits[i] == col) { continue; }
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
        Vector3 pos3 = pos.ToV3();
        Gizmos.color = Color.red;
        Gizmos.DrawLine(pos3, pos3 + (avoid * 8).ToV3());
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(pos3, pos3 + (adjoin.normalized * 8).ToV3());
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(pos3, pos3 + (align.normalized * 8).ToV3());

        Gizmos.color = Color.yellow;
        //draw detection radius
        Gizmos.DrawWireSphere(pos3 + (facing * 8).ToV3(), sets.radius);
        //show flockmates
        for (int i = 0; i < flock.Count; i++)
        {
            Gizmos.DrawLine(pos3, flock[i].pos.ToV3());
        }
    }
}