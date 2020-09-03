using System.Collections.Generic;
using UnityEngine;
using DragonSpace.Quadtrees;

//simple boids implementation to test quadtrees with. Not especially interesting or optimized!
public class Boid : BoidBase, IQtInsertable
{
    #region QT interface implementation
    public int X => (int)transform.position.x - (Width / 2);
    public int Y => (int)transform.position.z - (Height / 2);
    public int Lft => X;
    public int Top => Y + Height;
    public int Rgt => X + Width;
    public int Btm => Y;
    public int Width { get; set; }
    public int Height { get; set; }
    #endregion

    public static QuadTreeBase<BoidBase> qt;

    private void Awake()
    {
        Init();
    }

    public override void Init()
    {
        base.Init();
        Width = (int)transform.localScale.x;
        Height = (int)transform.localScale.z;
    }

    //update called by base

    protected override void FindFlockmates()
    {
        UnityEngine.Profiling.Profiler.BeginSample("Find Flockmates");
        int r = sets.radius;
        int x = (int)(facing.x * 8);
        int y = (int)(facing.y * 8);
        flock = qt.Query(Lft + x - r, Top + y + r, Rgt + x + r, Btm + y - r, index_TEMP);
        UnityEngine.Profiling.Profiler.EndSample();
    }

    protected override void UpdatePosition()
    {
        qt.MoveIndexToPoint(index_TEMP, (int)pos.x - (Width / 2), (int)pos.y - (Height / 2));
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

        //draw detection "radius"
        Gizmos.color = new Color(1, 0, 1, 0.4f);
        int r = sets.radius;
        int x = (int)(facing.x * 8);
        int y = (int)(facing.y * 8);
        Gizmos.DrawLine(new Vector3(Lft + x - r, 0, Btm + y - r), new Vector3(Lft + x - r, 0, Top + y + r));
        Gizmos.DrawLine(new Vector3(Lft + x - r, 0, Top + y + r), new Vector3(Rgt + x + r, 0, Top + y + r));
        Gizmos.DrawLine(new Vector3(Rgt + x + r, 0, Top + y + r), new Vector3(Rgt + x + r, 0, Btm + y - r));
        Gizmos.DrawLine(new Vector3(Rgt + x + r, 0, Btm + y - r), new Vector3(Lft + x - r, 0, Btm + y - r));

        //show flockmates
        Gizmos.color = Color.yellow;
        flock = qt.Query(Lft + x - r, Top + y + r, Rgt + x + r, Btm + y - r, index_TEMP);
        for (int i = 0; i < flock.Count; ++i)
        {
            Gizmos.DrawLine(pos3, flock[i].pos.ToV3());
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 pos3 = pos.ToV3();
        Gizmos.color = Color.green;
        Gizmos.DrawLine(pos3, pos3 + (dir.ToV3() * 4));
        Gizmos.color = Color.white;
        Gizmos.DrawLine(pos3, pos3 + (facing * 4).ToV3());
    }
}