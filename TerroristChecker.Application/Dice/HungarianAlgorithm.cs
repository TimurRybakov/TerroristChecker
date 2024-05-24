namespace TerroristChecker.Application.Dice;

/// <summary>
/// Hungarian Algorithm.
/// </summary>
public static class HungarianAlgorithm
{
    public enum ExtremumType : byte
    {
        Min,
        Max
    }

    /// <summary>
    /// Finds the optimal assignments for a given matrix of agents and costed tasks such that the total cost is minimized/maximized.
    /// </summary>
    /// <param name="costs">A cost matrix; the element at row <em>i</em> and column <em>j</em> represents the cost of agent <em>i</em> performing task <em>j</em>.</param>
    /// <param name="extremumType">Extremum type to find: minimum or maximum</param>
    /// <returns>A matrix of assignments; the value of element <em>i</em> is the column of the task assigned to agent <em>i</em>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="costs"/> is null.</exception>
    public static int[] FindAssignments(this int[,] costs, ExtremumType extremumType = ExtremumType.Min)
    {
        ArgumentNullException.ThrowIfNull(costs);

        var h = costs.GetLength(0);
        var w = costs.GetLength(1);

        for (var i = 0; i < h; i++)
        {
            var min = int.MaxValue;

            for (var j = 0; j < w; j++)
            {
                if (extremumType == ExtremumType.Max)
                {
                    costs[i, j] = -costs[i, j]; // This inverts minimum to maximum
                }

                min = Math.Min(min, costs[i, j]);
            }

            for (var j = 0; j < w; j++)
            {
                costs[i, j] -= min;
            }
        }

        var masks = new byte[h, w];
        var rowsCovered = new bool[h];
        var colsCovered = new bool[w];

        for (var i = 0; i < h; i++)
        {
            for (var j = 0; j < w; j++)
            {
                if (costs[i, j] == 0 && !rowsCovered[i] && !colsCovered[j])
                {
                    masks[i, j] = 1;
                    rowsCovered[i] = true;
                    colsCovered[j] = true;
                }
            }
        }

        ClearCovers(rowsCovered, colsCovered, w, h);

        var path = new Location[w * h];
        var pathStart = default(Location);
        var step = 1;

        while (step != -1)
        {
            step = step switch
            {
                1 => RunStep1(masks, colsCovered, w, h),
                2 => RunStep2(costs, masks, rowsCovered, colsCovered, w, h, ref pathStart),
                3 => RunStep3(masks, rowsCovered, colsCovered, w, h, path, pathStart),
                4 => RunStep4(costs, rowsCovered, colsCovered, w, h),
                _ => step
            };
        }

        var agentsTasks = new int[h];

        for (var i = 0; i < h; i++)
        {
            for (var j = 0; j < w; j++)
            {
                if (masks[i, j] == 1)
                {
                    agentsTasks[i] = j;
                    break;
                }
            }
        }

        return agentsTasks;
    }

    private static int RunStep1(byte[,] masks, bool[] colsCovered, int w, int h)
    {
        for (var i = 0; i < h; i++)
        {
            for (var j = 0; j < w; j++)
            {
                if (masks[i, j] == 1)
                    colsCovered[j] = true;
            }
        }

        var colsCoveredCount = 0;

        for (var j = 0; j < w; j++)
        {
            if (colsCovered[j])
                colsCoveredCount++;
        }

        if (colsCoveredCount == h)
            return -1;

        return 2;
    }
    private static int RunStep2(int[,] costs, byte[,] masks, bool[] rowsCovered, bool[] colsCovered, int w, int h, ref Location pathStart)
    {
        while (true)
        {
            var loc = FindZero(costs, rowsCovered, colsCovered, w, h);
            if (loc.Row == -1)
                return 4;

            masks[loc.Row, loc.Column] = 2;

            var starCol = FindStarInRow(masks, w, loc.Row);
            if (starCol != -1)
            {
                rowsCovered[loc.Row] = true;
                colsCovered[starCol] = false;
            }
            else
            {
                pathStart = loc;
                return 3;
            }
        }
    }
    private static int RunStep3(byte[,] masks, bool[] rowsCovered, bool[] colsCovered, int w, int h, Location[] path, Location pathStart)
    {
        var pathIndex = 0;
        path[0] = pathStart;

        while (true)
        {
            var row = FindStarInColumn(masks, h, path[pathIndex].Column);
            if (row == -1)
                break;

            pathIndex++;
            path[pathIndex] = new Location(row, path[pathIndex - 1].Column);

            var col = FindPrimeInRow(masks, w, path[pathIndex].Row);

            pathIndex++;
            path[pathIndex] = new Location(path[pathIndex - 1].Row, col);
        }

        ConvertPath(masks, path, pathIndex + 1);
        ClearCovers(rowsCovered, colsCovered, w, h);
        ClearPrimes(masks, w, h);

        return 1;
    }
    private static int RunStep4(int[,] costs, bool[] rowsCovered, bool[] colsCovered, int w, int h)
    {
        var minValue = HungarianAlgorithm.FindMinimum(costs, rowsCovered, colsCovered, w, h);

        for (var i = 0; i < h; i++)
        {
            for (var j = 0; j < w; j++)
            {
                if (rowsCovered[i])
                    costs[i, j] += minValue;
                if (!colsCovered[j])
                    costs[i, j] -= minValue;
            }
        }
        return 2;
    }

    private static int FindMinimum(int[,] costs, bool[] rowsCovered, bool[] colsCovered, int w, int h)
    {
        var minValue = int.MaxValue;

        for (var i = 0; i < h; i++)
        {
            for (var j = 0; j < w; j++)
            {
                if (!rowsCovered[i] && !colsCovered[j])
                    minValue = Math.Min(minValue, costs[i, j]);
            }
        }

        return minValue;
    }
    private static int FindStarInRow(byte[,] masks, int w, int row)
    {
        for (var j = 0; j < w; j++)
        {
            if (masks[row, j] == 1)
                return j;
        }

        return -1;
    }
    private static int FindStarInColumn(byte[,] masks, int h, int col)
    {
        for (var i = 0; i < h; i++)
        {
            if (masks[i, col] == 1)
                return i;
        }

        return -1;
    }
    private static int FindPrimeInRow(byte[,] masks, int w, int row)
    {
        for (var j = 0; j < w; j++)
        {
            if (masks[row, j] == 2)
                return j;
        }

        return -1;
    }
    private static Location FindZero(int[,] costs, bool[] rowsCovered, bool[] colsCovered, int w, int h)
    {
        for (var i = 0; i < h; i++)
        {
            for (var j = 0; j < w; j++)
            {
                if (costs[i, j] == 0 && !rowsCovered[i] && !colsCovered[j])
                    return new Location(i, j);
            }
        }

        return new Location(-1, -1);
    }
    private static void ConvertPath(byte[,] masks, Location[] path, int pathLength)
    {
        for (var i = 0; i < pathLength; i++)
        {
            masks[path[i].Row, path[i].Column] = masks[path[i].Row, path[i].Column] switch
            {
                1 => 0,
                2 => 1,
                _ => masks[path[i].Row, path[i].Column]
            };
        }
    }
    private static void ClearPrimes(byte[,] masks, int w, int h)
    {
        for (var i = 0; i < h; i++)
        {
            for (var j = 0; j < w; j++)
            {
                if (masks[i, j] == 2)
                    masks[i, j] = 0;
            }
        }
    }
    private static void ClearCovers(bool[] rowsCovered, bool[] colsCovered, int w, int h)
    {
        for (var i = 0; i < h; i++)
        {
            rowsCovered[i] = false;
        }

        for (var j = 0; j < w; j++)
        {
            colsCovered[j] = false;
        }
    }

    private struct Location
    {
        internal readonly int Row;
        internal readonly int Column;

        internal Location(int row, int col)
        {
            this.Row = row;
            this.Column = col;
        }
    }
}

