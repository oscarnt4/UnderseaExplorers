using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] bool generateNewLevel = false;

    [SerializeField] string seed;
    [SerializeField] bool useRandomSeed;

    [SerializeField] int width;
    [SerializeField] int height;

    [Range(0, 100)]
    [SerializeField] int fillPercentage;
    [SerializeField] int filledRegionMinSize;
    [SerializeField] int emptyRegionMinSize;
    [SerializeField] int smoothingIterations;
    [SerializeField] int smoothingThreshold;
    [SerializeField] int branchingIterations;

    [SerializeField] GameObject[] tiles;

    [SerializeField] int diverCount;
    [SerializeField] GameObject diverPrefab;
    [SerializeField] int mermaidCount;
    [SerializeField] GameObject mermaidPrefab;

    private int[,] map;
    private System.Random rnd;

    private List<GameObject> levelTiles;
    private List<GameObject> divers;
    private List<GameObject> mermaids;

    private void Awake()
    {
        if (useRandomSeed) seed = DateTime.Now.Ticks.ToString();
        levelTiles = new List<GameObject>();
        divers = new List<GameObject>();
        mermaids = new List<GameObject>();
    }

    void Start()
    {
        GenerateNewLevel();
        SpawnAgents();
    }

    void Update()
    {
        if (generateNewLevel)
        {
            ClearAgents();
            ClearTiles();
            if (useRandomSeed) seed = DateTime.Now.Ticks.ToString();
            GenerateNewLevel();
            generateNewLevel = false;
            SpawnAgents();
        }
        CheckDestroyedDivers();
    }

    void GenerateNewLevel()
    {
        GenerateRandomGrid();
        ApplyCellularAutomata();
        PostProcessing();
    }

    void GenerateRandomGrid()
    {
        rnd = new System.Random(seed.GetHashCode());
        map = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1) map[x, y] = 1;
                else map[x, y] = rnd.Next(0, 100) < fillPercentage ? 1 : 0;
            }
        }

    }

    //debugging
    /*void OnDrawGizmos()
    {
        if (map != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Gizmos.color = map[x, y] == 1 ? Color.red : Color.green;
                    Vector3 location = new Vector3(x - width / 2, y - height / 2, 0);
                    Gizmos.DrawCube(location, Vector3.one);
                }
            }
        }
    }*/

    void ApplyCellularAutomata()
    {
        for (int i = 0; i < smoothingIterations; i++) ApplySmoothing();
        for (int i = 0; i < branchingIterations; i++) ApplyBranching();
        CleanCells();
    }

    void ApplySmoothing()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int surroundingFilledCells = GetSurroundingFilledCells(x, y);

                if (surroundingFilledCells > smoothingThreshold)
                {
                    map[x, y] = 1;
                }
                else if (surroundingFilledCells < smoothingThreshold)
                {
                    map[x, y] = 0;
                }
            }
        }
    }

    int GetSurroundingFilledCells(int coreX, int coreY)
    {
        return GetSurroundingFilledCells(coreX, coreY, 1);
    }

    int GetSurroundingFilledCells(int coreX, int coreY, int range)
    {
        int filledCells = 0;
        for (int surroundingX = coreX - range; surroundingX <= coreX + range; surroundingX++)
        {
            for (int surroundingY = coreY - range; surroundingY <= coreY + range; surroundingY++)
            {
                if (surroundingX >= 0 && surroundingX < width && surroundingY >= 0 && surroundingY < height)
                {
                    if (surroundingX != coreX || surroundingY != coreY)
                    {
                        filledCells += map[surroundingX, surroundingY];
                    }
                }
                else
                {
                    filledCells++;
                }
            }
        }

        return filledCells;
    }

    void ApplyBranching()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                //branch in the direction of empty space
                bool filledCellsOnOneSide = AreFilledCellsOnOneSide(x, y);
                int surroundingFilledCells = GetSurroundingFilledCells(x, y, 2);
                if (filledCellsOnOneSide && surroundingFilledCells <= 4 && surroundingFilledCells >= 3 && map[x, y] != 1)
                {
                    map[x, y] = rnd.Next(0, 3) == 0 ? 0 : 1;
                }
                //thicken branches
                surroundingFilledCells = GetSurroundingFilledCells(x, y);
                if (surroundingFilledCells == 4 && map[x, y] == 0)
                {
                    map[x, y] = 1;
                }
            }
        }
    }

    bool AreFilledCellsOnOneSide(int coreX, int coreY)
    {
        int filledX = -1;
        int filledY = -1;
        for (int surroundingX = coreX - 1; surroundingX <= coreX + 1; surroundingX++)
        {
            for (int surroundingY = coreY - 1; surroundingY <= coreY + 1; surroundingY++)
            {
                if (surroundingX >= 0 && surroundingX < width && surroundingY >= 0 && surroundingY < height)
                {
                    if (map[surroundingX, surroundingY] == 1)
                    {
                        if (surroundingX == filledX)
                        {
                            filledY = -1;
                        }
                        if (surroundingY == filledY)
                        {
                            filledX = -1;
                        }
                        if (surroundingX != filledX && surroundingY != filledY)
                        {
                            filledX = -1;
                            filledY = -1;
                        }
                        //for first filled square found
                        if (filledX == -1 && filledY == -1)
                        {
                            filledX = surroundingX;
                            filledY = surroundingY;
                        }
                    }
                }
            }
        }
        return !(filledX == -1 && filledY == -1);
    }

    void CleanCells()
    {
        List<List<Vector2Int>> filledRegions = GetRegions(1);

        foreach (List<Vector2Int> region in filledRegions)
        {
            if (region.Count < filledRegionMinSize)
            {
                foreach (Vector2Int cell in region)
                {
                    map[cell.x, cell.y] = 0;
                }
            }
        }

        List<List<Vector2Int>> emptyRegions = GetRegions(0);

        foreach (List<Vector2Int> region in emptyRegions)
        {
            if (region.Count < emptyRegionMinSize)
            {
                foreach (Vector2Int cell in region)
                {
                    map[cell.x, cell.y] = 1;
                }
            }
        }
    }

    List<List<Vector2Int>> GetRegions(int cellValue)
    {
        List<List<Vector2Int>> regions = new List<List<Vector2Int>>();
        bool[,] cellChecked = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (cellChecked[x, y] == false && map[x, y] == cellValue)
                {
                    List<Vector2Int> region = GetRegion(x, y);
                    regions.Add(region);

                    foreach (Vector2Int cell in region)
                    {
                        cellChecked[cell.x, cell.y] = true;
                    }
                }
            }
        }
        return regions;
    }

    List<Vector2Int> GetRegion(int startX, int startY)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        bool[,] cellChecked = new bool[width, height];
        int cellValue = map[startX, startY];

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));
        cellChecked[startX, startY] = true;

        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();

            cells.Add(cell);
            for (int x = cell.x - 1; x <= cell.x + 1; x++)
            {
                for (int y = cell.y - 1; y <= cell.y + 1; y++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height && (x == cell.x || y == cell.y))
                    {
                        if (!cellChecked[x, y] && map[x, y] == cellValue)
                        {
                            cellChecked[x, y] = true;
                            queue.Enqueue(new Vector2Int(x, y));
                        }
                    }
                }
            }
        }
        return cells;
    }

    void PostProcessing()
    {
        GenerateTiles();
    }

    void GenerateTiles()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int tileType = GetTileType(x, y);
                int tileRotation = GetTileOrientation(x, y, tileType);
                Vector2 position = new Vector2(x - width / 2, y - height / 2);
                Quaternion rotation = Quaternion.Euler(0, 0, 90 * tileRotation);
                GameObject tile = Instantiate(tiles[tileType], position, rotation);
                tile.transform.parent = transform;
                levelTiles.Add(tile);
            }
        }
    }

    int GetTileType(int x, int y)
    {
        int northTile = y + 1 < height ? map[x, y + 1] : 1;
        int southTile = y - 1 >= 0 ? map[x, y - 1] : 1;
        int eastTile = x + 1 < width ? map[x + 1, y] : 1;
        int westTile = x - 1 >= 0 ? map[x - 1, y] : 1;
        // ocean tile
        if (map[x, y] == 0) return 0;
        // center tile
        if (northTile == 1 && southTile == 1 && eastTile == 1 && westTile == 1) return 1;
        // floating tile
        if (northTile == 0 && southTile == 0 && eastTile == 0 && westTile == 0) return 2;
        // edge tile
        if ((northTile == 0 && southTile == 1 && eastTile == 1 && westTile == 1)
            || (northTile == 1 && southTile == 0 && eastTile == 1 && westTile == 1)
            || (northTile == 1 && southTile == 1 && eastTile == 0 && westTile == 1)
            || (northTile == 1 && southTile == 1 && eastTile == 1 && westTile == 0))
            return 3;
        // collumn tile
        if ((northTile == 1 && southTile == 1 && eastTile == 0 && westTile == 0)
            || (northTile == 0 && southTile == 0 && eastTile == 1 && westTile == 1))
            return 4;
        // end tile
        if ((northTile == 1 && southTile == 0 && eastTile == 0 && westTile == 0)
            || (northTile == 0 && southTile == 1 && eastTile == 0 && westTile == 0)
            || (northTile == 0 && southTile == 0 && eastTile == 1 && westTile == 0)
            || (northTile == 0 && southTile == 0 && eastTile == 0 && westTile == 1))
            return 5;
        // corner tile
        if ((northTile == 1 && southTile == 0 && eastTile == 1 && westTile == 0)
            || (northTile == 0 && southTile == 1 && eastTile == 0 && westTile == 1)
            || (northTile == 0 && southTile == 1 && eastTile == 1 && westTile == 0)
            || (northTile == 1 && southTile == 0 && eastTile == 0 && westTile == 1))
            return 6;
        // failed
        return -1;
    }

    int GetTileOrientation(int x, int y, int tileType)
    {
        // ocean tile
        if (tileType == 0) return 0;
        // center/floating tile
        if (tileType == 1 || tileType == 2) return rnd.Next(0, 4);
        // edge tile
        if (tileType == 3)
        {
            if ((y + 1 < height ? map[x, y + 1] : 1) == 0) return 0;
            if ((x + 1 < width ? map[x + 1, y] : 1) == 0) return 3;
            if ((y - 1 >= 0 ? map[x, y - 1] : 1) == 0) return 2;
            if ((x - 1 >= 0 ? map[x - 1, y] : 1) == 0) return 1;
        }
        // collumn tile
        if (tileType == 4)
        {
            if ((y + 1 < height ? map[x, y + 1] : 1) == 1) return 2 * rnd.Next(0, 2);
            if ((x + 1 < width ? map[x + 1, y] : 1) == 1) return 2 * rnd.Next(0, 2) + 1;
        }
        // end tile
        if (tileType == 5)
        {
            if ((y + 1 < height ? map[x, y + 1] : 1) == 1) return 2;
            if ((x + 1 < width ? map[x + 1, y] : 1) == 1) return 1;
            if ((y - 1 >= 0 ? map[x, y - 1] : 1) == 1) return 0;
            if ((x - 1 >= 0 ? map[x - 1, y] : 1) == 1) return 3;
        }
        // corner tile
        if (tileType == 6)
        {
            if ((y + 1 < height ? map[x, y + 1] : 1) == 0
                && (x - 1 >= 0 ? map[x - 1, y] : 1) == 0)
                return 0;
            if ((y + 1 < height ? map[x, y + 1] : 1) == 0
                && (x - 1 >= 0 ? map[x - 1, y] : 1) == 1)
                return 3;
            if ((y + 1 < height ? map[x, y + 1] : 1) == 1
                && (x - 1 >= 0 ? map[x - 1, y] : 1) == 1)
                return 2;
            if ((y + 1 < height ? map[x, y + 1] : 1) == 1
                && (x - 1 >= 0 ? map[x - 1, y] : 1) == 0)
                return 1;

        }
        return 0;
    }

    public int[,] GetMap()
    {
        return map;
    }

    void ClearTiles()
    {
        foreach (GameObject tile in levelTiles)
        {
            Destroy(tile);
        }
        levelTiles.Clear();
    }

    void SpawnAgents()
    {
        List<List<Vector2Int>> emptyRegions = GetRegions(0);
        List<Vector2Int> emptyRegion = emptyRegions[rnd.Next(0, emptyRegions.Count)];

        for (int i = 0; i < diverCount; i++)
        {
            Vector2Int spawnTile = emptyRegion[rnd.Next(0, emptyRegion.Count)];
            emptyRegion.Remove(spawnTile);
            Vector2 spawnLocation = new Vector2(spawnTile.x - width / 2, spawnTile.y - height / 2);
            Quaternion spawnRotation = Quaternion.Euler(0, 0, rnd.Next(0, 360));
            GameObject diver = Instantiate(diverPrefab, new Vector3(spawnLocation.x, spawnLocation.y, 0f), spawnRotation);
            divers.Add(diver);
        }
        for (int i = 0; i < mermaidCount; i++)
        {
            bool tooClose = true;
            Vector2 spawnLocation = Vector2.zero;
            while (tooClose && emptyRegion.Count > 0)
            {
                Vector2Int spawnTile = emptyRegion[rnd.Next(0, emptyRegion.Count)];
                emptyRegion.Remove(spawnTile);
                spawnLocation = new Vector2(spawnTile.x - width / 2, spawnTile.y - height / 2);
                tooClose = false;
                foreach (GameObject diver in divers) if (Vector2.Distance(spawnLocation, diver.transform.position) <= 20f) tooClose = true;
                foreach (GameObject _mermaid in mermaids) if (Vector2.Distance(spawnLocation, _mermaid.transform.position) <= 20f) tooClose = true;
            }
            if (!tooClose)
            {
                Quaternion spawnRotation = Quaternion.Euler(0, 0, rnd.Next(0, 360));
                GameObject mermaid = Instantiate(mermaidPrefab, new Vector3(spawnLocation.x, spawnLocation.y, 0f), spawnRotation);
                mermaids.Add(mermaid);
            }
        }

        foreach (GameObject diver in divers)
        {
            foreach (GameObject mermaid in mermaids)
            {
                diver.GetComponent<DiverBehaviourTree>().AddEnemy(mermaid);
                mermaid.GetComponent<MermaidBehaviourTree>().AddTarget(diver);
            }
        }
    }

    void ClearAgents()
    {
        for (int i = 0; i < divers.Count; i++)
        {
            if (divers[i] != null && divers[i].activeSelf) Destroy(divers[i]);
        }
        for (int i = 0; i < mermaids.Count; i++)
        {
            if (mermaids[i] != null && mermaids[i].activeSelf) Destroy(mermaids[i]);
        }
        divers.Clear();
        mermaids.Clear();
    }

    public List<GameObject> GetGameObjects()
    {
        List<GameObject> list = new List<GameObject>();
        list.AddRange(divers);
        list.AddRange(mermaids);
        return list;
    }

    void CheckDestroyedDivers()
    {
        int diverDestroyedIdx = -1;
        for (int i = 0; i < divers.Count; i++)
        {
            if (divers[i] == null || !divers[i].activeSelf) diverDestroyedIdx = i;
        }
        if (diverDestroyedIdx != -1) divers.RemoveAt(diverDestroyedIdx);
    }
}
