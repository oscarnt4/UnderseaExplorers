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

    private int[,] map;
    private System.Random rnd;

    private void Awake()
    {
        if (useRandomSeed) seed = DateTime.Now.Ticks.ToString();
    }

    void Start()
    {
        GenerateNewLevel();
    }

    void Update()
    {
        if (generateNewLevel)
        {
            if (useRandomSeed) seed = DateTime.Now.Ticks.ToString();
            GenerateNewLevel();
            generateNewLevel = false;
        }
    }

    public void GenerateNewLevel()
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
    void OnDrawGizmos()
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
    }

    void ApplyCellularAutomata()
    {
        for (int i = 0; i < smoothingIterations; i++) ApplySmoothing();
        for (int i = 0; i < branchingIterations; i++) ApplyBranching();
        CleanCells();
        //ApplySmoothing();
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
                foreach(Vector2Int cell in region)
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
    }
}
