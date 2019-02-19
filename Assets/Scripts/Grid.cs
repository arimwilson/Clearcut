using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    public static int w = 10;
    public static int h = 20;
    // Transforms are Unity positions.
    public static Transform[,] grid = new Transform[w, h];

    public static Vector2 RoundVec2(Vector2 v)
    {
        return new Vector2(Mathf.Round(v.x), Mathf.Round(v.y));
    }

    public static bool InsideBorder(Vector2 v)
    {
        return v.x >= 0 && v.x < w && v.y >= 0 && v.y <= h;
    }

    public static void DestroyRow(int y)
    {
        for (int x = 0; x < w; x++)
        {
            if (grid[x, y] == null) continue;
            Destroy(grid[x, y].gameObject);
            grid[x, y] = null;
        }
    }

    public static void DestroyAllRows()
    {
        for (int y = 0; y < h; y++)
        {
            DestroyRow(y);
        }
    }

    public static void DecreaseRow(int y)
    {
        for (int x = 0; x < w; x++)
        {
            if (grid[x, y] == null) continue;
            grid[x, y - 1] = grid[x, y];
            grid[x, y] = null;
            // Move block down.
            grid[x, y - 1].position += new Vector3(0, -1, 0);
        }
    }

    public static void DecreaseRowsAbove(int y)
    {
        for (int i = y; i < h; i++)
        {
            DecreaseRow(i);
        }
    }

    public static bool IsRowFull(int y)
    {
        for (int x = 0; x < w; x++)
        {
            if (grid[x, y] == null) return false;
        }
        return true;
    }

    public static void DeleteFullRows()
    {
        for (int y = 0; y < h; y++)
        {
            if (!IsRowFull(y)) continue;
            DestroyRow(y);
            DecreaseRowsAbove(y + 1);
            y--;
        }
    }
}
