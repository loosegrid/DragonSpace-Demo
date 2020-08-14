using UnityEngine;
using DragonSpace.Quadtrees;
using DragonSpace.Grids;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class BoidController : MonoBehaviour
{
    public QtTestType type;
    public RectInt bounds;
    public GameObject boidPrefab;
    public GameObject physicsBoidPrefab;
    public GameObject uGridBoidPrefab;
    public GameObject ldGridBoidPrefab;

    [Header("Quadtree config")]
    public bool autoConfig;
    public int elementsPerQuad;
    public int maxDepth;
    [Header("Grid config")]
    public int cellSize;
    public int coarseCellSize;

    [Header("Test config")]
    public int testElements;

    int entityCount;
    QuadTreeBase<BoidBase> qt;
    UGrid<UGridBoid> uGrid;
    LooseDoubleGrid ldGrid;

    System.Random r;
    private void Start()
    {
        Camera.main.transform.position = new Vector3(bounds.width / 2, 50, bounds.height / 2);
        Camera.main.orthographicSize = bounds.height / 1.6f;

        BoidBase.bounds = bounds;
        BoidBase.sets = GetComponent<BoidSettingsTemp>();

        r = new System.Random(12345);
        //insert boids
        switch (type)
        {
            case QtTestType.Physics:
                InitPhysics();
                break;
            case QtTestType.Quadtree:
                InitQuadtree();
                break;
            case QtTestType.LooseQuadtree:
                InitLooseQuadtree();
                break;
            case QtTestType.UGrid:
                InitUGrid();
                break;
            case QtTestType.LooseDGrid:
                InitLDGrid();
                break;
            default:
                break;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    private void InitPhysics()
    {
        for (int i = 0; i < testElements; ++i)
        {
            Vector3Int pos = new Vector3Int(r.Next(bounds.width), 0, r.Next(bounds.height));
            GameObject go = Instantiate(physicsBoidPrefab);
            go.transform.position = pos;
            ++entityCount;
        }
    }

    private void InitQuadtree()
    {
        if (autoConfig)
            qt = QuadTree<BoidBase>.NewTree(bounds.width, bounds.height, 2);
        else
            qt = new QuadTree<BoidBase>(bounds.width, bounds.height, elementsPerQuad, maxDepth);

        Boid.qt = qt;

        for (int i = 0; i < testElements; ++i)
        {
            Vector3Int pos = new Vector3Int(r.Next(bounds.width), 0, r.Next(bounds.height));
            GameObject go = Instantiate(boidPrefab);
            go.transform.position = pos;
            go.TryGetComponent(out Boid b);
            b.Init();
            UnityEngine.Profiling.Profiler.BeginSample("Insert boids");
            b.treeIndex = qt.InsertPoint(b, b.X, b.Y, b.Width, b.Height);
            UnityEngine.Profiling.Profiler.EndSample();
            ++entityCount;
        }
    }

    private void InitLooseQuadtree()
    {
        if (autoConfig)
            qt = LooseQuadTree<BoidBase>.NewTree(bounds.width, bounds.height, 2);
        else
            qt = new LooseQuadTree<BoidBase>(bounds.width, bounds.height, elementsPerQuad, maxDepth);

        Boid.qt = qt;

        for (int i = 0; i < testElements; ++i)
        {
            Vector3Int pos = new Vector3Int(r.Next(bounds.width), 0, r.Next(bounds.height));
            GameObject go = Instantiate(boidPrefab);
            go.transform.position = pos;
            go.TryGetComponent(out Boid b);
            b.Init();
            UnityEngine.Profiling.Profiler.BeginSample("Insert boids");
            b.treeIndex = ((LooseQuadTree<BoidBase>)qt).BulkInsertPoint(b, b.X, b.Y, b.Width, b.Height);
            UnityEngine.Profiling.Profiler.EndSample();
            ++entityCount;
        }
    }

    private void InitUGrid()
    {
        uGrid = new UGrid<UGridBoid>(2, 2, cellSize, cellSize, bounds.width, bounds.height);

        UGridBoid.bounds = bounds;
        UGridBoid.ugrid = uGrid;
        UGridBoid.sets = GetComponent<BoidSettingsTemp>();

        for (int i = 0; i < testElements; ++i)
        {
            Vector3Int pos = new Vector3Int(r.Next(bounds.width), 0, r.Next(bounds.height));
            GameObject go = Instantiate(uGridBoidPrefab);
            go.transform.position = pos;
            go.TryGetComponent(out UGridBoid b);
            b.id = entityCount;
            b.pos = b.transform.position;
            uGrid.Insert(b);
            ++entityCount;
        }
    }

    private void InitLDGrid()
    {
        ldGrid = new LooseDoubleGrid(cellSize, cellSize, coarseCellSize, coarseCellSize, bounds.width, bounds.height);

        GridBoid.bounds = bounds;
        GridBoid.ugrid = ldGrid;
        GridBoid.sets = GetComponent<BoidSettingsTemp>();

        for (int i = 0; i < testElements; ++i)
        {
            Vector3Int pos = new Vector3Int(r.Next(bounds.width), 0, r.Next(bounds.height));
            GameObject go = Instantiate(ldGridBoidPrefab);
            go.transform.position = pos;
            go.TryGetComponent(out GridBoid b);
            b.ID = entityCount;
            b.pos = b.transform.position;
            ldGrid.Insert(b);
            ++entityCount;
        }
    }

    public int frames;
    private void LateUpdate()
    {
        // note this has to be run at the end of every frame or update
        // it can be called less frequently if the tree isn't modified
        if (type == QtTestType.Quadtree || type == QtTestType.LooseQuadtree)
            qt.Cleanup();
        if (type == QtTestType.LooseDGrid)
            ldGrid.TightenUp();

        //this framerate is wildly inaccurate in the editor, 
        //but it's useful for comparing optimization changes
        if(Time.frameCount == frames)
        {
            Debug.Log("Rough framerate: " + (Time.frameCount / Time.time).ToString());

            Debug.Break();
        }
    }

#if UNITY_EDITOR
    public bool drawTree;
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            bounds.center.ToV3(),
            new Vector3(bounds.width, 0, bounds.height));
        Gizmos.DrawLine(
            new Vector3(0, 1, bounds.height / 2),
            new Vector3(bounds.width, 1, bounds.height / 2));
        Gizmos.DrawLine(
            new Vector3(bounds.width / 2, 1, 0),
            new Vector3(bounds.width / 2, 1, bounds.height));

        if (Application.isPlaying && drawTree)
        {
            if (type == QtTestType.Quadtree || type == QtTestType.LooseQuadtree)
                qt.Traverse(QtGizmo.Draw);
            else if (type == QtTestType.UGrid)
                uGrid.Traverse(UGridGizmo.Draw);
            else if (type == QtTestType.LooseDGrid)
                ldGrid.Traverse(LooseGridGizmo.Draw);
        }
    }
#endif
}

public enum QtTestType
{
    Physics,
    Quadtree,
    LooseQuadtree,
    UGrid,
    LooseDGrid
}