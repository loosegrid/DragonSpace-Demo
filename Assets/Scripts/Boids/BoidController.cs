using UnityEngine;
using DragonSpace.Quadtrees;
using DragonSpace.Grids;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class BoidController : MonoBehaviour
{
    public static bool useMenuSettings;
    public BoidSettingsTemp menuSettings;
    public BoidSettingsTemp sets;
    QtTestType type;
    public GameObject boidPrefab;
    public GameObject physicsBoidPrefab;
    public GameObject physics2DBoidPrefab;
    public GameObject physicsXYBoidPrefab;
    public GameObject uGridBoidPrefab;
    public GameObject ldGridBoidPrefab;
    public Text framerateResults;
    public Text framerate;

    [Header("Quadtree config")]
    public bool autoConfig;
    public int elementsPerQuad;
    public int maxDepth;
    [Header("Grid config")]
    public int cellSize;
    public int coarseCellSize;

    int entityCount;
    QuadTreeBase<BoidBase> qt;
    UGrid<UGridBoid> uGrid;
    LooseDoubleGrid ldGrid;

    public RectInt queryTest;

    System.Random r;
    private void Start()
    {
        startFrame = Time.frameCount;
        startTime = Time.time;
        if (useMenuSettings) { sets = menuSettings; }
        BoidBase.bounds = sets.bounds;
        BoidBase.sets = sets;
        type = sets.type;

        if (type == QtTestType.Physics2D || type == QtTestType.PhysicsXY)
        {
            Camera.main.transform.position = new Vector3(sets.bounds.width / 2, sets.bounds.height / 2, -50);
            Camera.main.transform.Rotate(new Vector3(-90, 0, 0));
        }
        else
            Camera.main.transform.position = new Vector3(sets.bounds.width / 2, 50, sets.bounds.height / 2);

        Camera.main.orthographicSize = sets.bounds.height / 1.6f;
        

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
            case QtTestType.Physics2D:
                InitPhysics2D();
                break;
            case QtTestType.PhysicsXY:
                InitPhysicsXY();
                break;
            default:
                break;
        }

        framerateResults.gameObject.SetActive(sets.showFPS);
        framerateResults.text += " (" + frames.ToString() + " frames)";
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene(0);
        }
    }

    private void InitPhysics()
    {
        for (int i = 0; i < sets.testElements; ++i)
        {
            Vector3Int pos = new Vector3Int(r.Next(sets.bounds.width), 0, r.Next(sets.bounds.height));
            Instantiate(physicsBoidPrefab, pos, Quaternion.identity);
            ++entityCount;
        }
    }

    private void InitPhysics2D()
    {
        PhysicsBoid2D.sets = sets;
        PhysicsBoid2D.bounds = sets.bounds;
        for (int i = 0; i < sets.testElements; ++i)
        {
            Vector3Int pos = new Vector3Int(r.Next(sets.bounds.width), r.Next(sets.bounds.height), 0);
            Instantiate(physics2DBoidPrefab, pos, Quaternion.identity);
            ++entityCount;
        }
    }

    private void InitPhysicsXY()
    {
        PhysicsBoidXY.sets = sets;
        PhysicsBoidXY.bounds = sets.bounds;
        for (int i = 0; i < sets.testElements; ++i)
        {
            Vector3Int pos = new Vector3Int(r.Next(sets.bounds.width), r.Next(sets.bounds.height), 0);
            Instantiate(physicsXYBoidPrefab, pos, Quaternion.identity);
            ++entityCount;
        }
    }

    private void InitQuadtree()
    {
        if (autoConfig)
            qt = QuadTree<BoidBase>.NewTree(sets.bounds.width, sets.bounds.height, 2);
        else
            qt = new QuadTree<BoidBase>(sets.bounds.width, sets.bounds.height, elementsPerQuad, maxDepth);

        Boid.qt = qt;

        for (int i = 0; i < sets.testElements; ++i)
        {
            Vector3Int pos = new Vector3Int(r.Next(sets.bounds.width), 0, r.Next(sets.bounds.height));
            GameObject go = Instantiate(boidPrefab, pos, Quaternion.identity);
            go.TryGetComponent(out Boid b);
            b.Init();
            UnityEngine.Profiling.Profiler.BeginSample("Insert boids");
            b.index_TEMP = qt.InsertPoint(b, b.X, b.Y, b.Width, b.Height);
            UnityEngine.Profiling.Profiler.EndSample();
            ++entityCount;
        }
    }

    private void InitLooseQuadtree()
    {
        if (autoConfig)
            qt = LooseQuadTree<BoidBase>.NewTree(sets.bounds.width, sets.bounds.height, 2);
        else
            qt = new LooseQuadTree<BoidBase>(sets.bounds.width, sets.bounds.height, elementsPerQuad, maxDepth);

        Boid.qt = qt;

        for (int i = 0; i < sets.testElements; ++i)
        {
            Vector3Int pos = new Vector3Int(r.Next(sets.bounds.width), 0, r.Next(sets.bounds.height));
            GameObject go = Instantiate(boidPrefab, pos, Quaternion.identity);
            go.TryGetComponent(out Boid b);
            b.Init();
            UnityEngine.Profiling.Profiler.BeginSample("Insert boids");
            b.index_TEMP = ((LooseQuadTree<BoidBase>)qt).BulkInsertPoint(b, b.X, b.Y, b.Width, b.Height);
            UnityEngine.Profiling.Profiler.EndSample();
            ++entityCount;
        }
    }

    private void InitUGrid()
    {
        uGrid = new UGrid<UGridBoid>(2, 2, cellSize, cellSize, sets.bounds.width, sets.bounds.height);

        UGridBoid.bounds = sets.bounds;
        UGridBoid.ugrid = uGrid;
        UGridBoid.sets = sets;

        for (int i = 0; i < sets.testElements; ++i)
        {
            Vector3Int pos = new Vector3Int(r.Next(sets.bounds.width), 0, r.Next(sets.bounds.height));
            GameObject go = Instantiate(uGridBoidPrefab, pos, Quaternion.identity);
            go.TryGetComponent(out UGridBoid b);
            b.ID = entityCount;
            uGrid.Insert(b);
            ++entityCount;
        }
    }

    private void InitLDGrid()
    {
        ldGrid = new LooseDoubleGrid(cellSize, cellSize, coarseCellSize, coarseCellSize, sets.bounds.width, sets.bounds.height);

        GridBoid.bounds = sets.bounds;
        GridBoid.grid = ldGrid;
        GridBoid.sets = sets;

        for (int i = 0; i < sets.testElements; ++i)
        {
            Vector3Int pos = new Vector3Int(r.Next(sets.bounds.width), 0, r.Next(sets.bounds.height));
            GameObject go = Instantiate(ldGridBoidPrefab, pos, Quaternion.identity);
            go.TryGetComponent(out GridBoid b);
            b.ID = entityCount;
            ldGrid.Insert(b);
            ++entityCount;
        }
    }

    public int frames;
    int startFrame;
    float startTime;
    int framesPassed;
    float timePassed;
    private void LateUpdate()
    {
        // note this has to be run at the end of every frame or update
        // it can be called less frequently if the tree isn't modified
        if (type == QtTestType.Quadtree || type == QtTestType.LooseQuadtree)
            qt.Cleanup();
        else if (type == QtTestType.LooseDGrid)
            ldGrid.TightenUp();
        else if (type == QtTestType.Physics || type == QtTestType.PhysicsXY)
            Physics.Simulate(Time.deltaTime);
        else if (type == QtTestType.Physics2D)
            Physics2D.Simulate(Time.deltaTime);

        //this framerate is wildly inaccurate in the editor, 
        //but it's useful for comparing optimization changes
        framesPassed = Time.frameCount - startFrame;
        if (framesPassed == frames)
        {
            framerate.text = (framesPassed / Time.timeSinceLevelLoad).ToString();

            Debug.Log("Rough framerate: " + (framesPassed / Time.timeSinceLevelLoad).ToString());
            Debug.Break();
        }
    }

#if UNITY_EDITOR
    public bool drawTree;
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            sets.bounds.center.ToV3(),
            new Vector3(sets.bounds.width, 0, sets.bounds.height));
        Gizmos.DrawLine(
            new Vector3(0, 1, sets.bounds.height / 2),
            new Vector3(sets.bounds.width, 1, sets.bounds.height / 2));
        Gizmos.DrawLine(
            new Vector3(sets.bounds.width / 2, 1, 0),
            new Vector3(sets.bounds.width / 2, 1, sets.bounds.height));

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(
            queryTest.center.ToV3(),
            queryTest.size.ToV3());

        //TODO: this is stupid
        if (Application.isPlaying)
        {
            List<MonoBehaviour> ugh = new List<MonoBehaviour>(4);
            List<BoidBase> results = new List<BoidBase>(1);
            List<UGridBoid> results2 = new List<UGridBoid>(1);
            List<IGridElt> results3 = new List<IGridElt>(1);

            switch (type)
            {
                case QtTestType.Quadtree:
                    results = qt.Query(queryTest.xMin, queryTest.yMax, queryTest.xMax, queryTest.yMin);
                    break;
                case QtTestType.LooseQuadtree:
                    results = qt.Query(queryTest.xMin, queryTest.yMax, queryTest.xMax, queryTest.yMin);
                    break;
                case QtTestType.UGrid:
                    results2 = uGrid.Query(queryTest.xMin, queryTest.yMin, queryTest.xMax, queryTest.yMax);
                    break;
                case QtTestType.LooseDGrid:
                    results3 = ldGrid.Query(queryTest.xMin, queryTest.yMin, queryTest.xMax, queryTest.yMax);
                    break;
                default:
                    break;
            }
            for (int i = 0; i < results.Count; i++)
            {
                ugh.Add((MonoBehaviour)results[i]);
            }
            for (int i = 0; i < results2.Count; i++)
            {
                ugh.Add((MonoBehaviour)results2[i]);
            }
            for (int i = 0; i < results3.Count; i++)
            {
                ugh.Add((MonoBehaviour)results3[i]);
            }

            Gizmos.color = Color.green;
            for (int i = 0; i < ugh.Count; i++)
            {
                Gizmos.DrawCube(ugh[i].transform.position, new Vector3(3, 3, 3));
            }

        }

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
    Physics2D,
    PhysicsXY,
    Quadtree,
    LooseQuadtree,
    UGrid,
    LooseDGrid
}