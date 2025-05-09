using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static readonly char[] keys_char = Enumerable.Range('a', 26).Select(i => (char) i).ToArray();
    static readonly char[] doors_char = keys_char.Select(char.ToUpper).ToArray();

    static List<List<char>> GetInput()
    {
        var data = new List<List<char>>();
        string line;
        while ((line = Console.ReadLine()) != null && line != "")
        {
            data.Add(line.ToCharArray().ToList());
        }
        return data;
    }

    static Dictionary<(char from, char to), int> ComputeDirectNeighbors(List<List<char>> map)
    {
        var rows = map.Count;
        var cols = map[0].Count;

        var poi_chars = keys_char.Concat(doors_char).Append('@').ToHashSet();

        var poi_positions = new Dictionary<char, (int r, int c)>();
        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                char ch = map[r][c];
                if (poi_chars.Contains(ch))
                    poi_positions[ch] = (r, c);
            }
        }

        var distances = new Dictionary<(char from, char to), int>();

        foreach (var kvp in poi_positions)
        {
            var startChar = kvp.Key;
            var startR = kvp.Value.r;
            var startC = kvp.Value.c;

            var visited = new bool[rows, cols];
            var queue = new Queue<((int r, int c), int dist)>();
            queue.Enqueue(((startR, startC), 0));
            visited[startR, startC] = true;

            while (queue.Count > 0)
            {
                var ((r, c), dist) = queue.Dequeue();

                foreach (var (dr, dc) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
                {
                    var nr = r + dr;
                    var nc = c + dc;
                    if (nr < 0 || nr >= rows || nc < 0 || nc >= cols) continue;
                    if (visited[nr, nc] || map[nr][nc] == '#') continue;

                    visited[nr, nc] = true;
                    char ch = map[nr][nc];

                    if (poi_chars.Contains(ch) && ch != startChar)
                        distances[(startChar, ch)] = dist + 1;
                    else
                        queue.Enqueue(((nr, nc), dist + 1));
                }
            }
        }

        return distances;
    }

    static int SolveSingleRobot(List<List<char>> map, Dependancies dependencies, HashSet<char> allKeys)
    {
        var distances = ComputeDirectNeighbors(map);

        var keys = keys_char.Where(k => map.Any(row => row.Contains(k))).ToHashSet();

        var poi_chars = keys_char.Concat(doors_char).Append('@').ToHashSet();
        var poi_positions = new Dictionary<char, (int r, int c)>();
        for (var r = 0; r < map.Count; r++)
            for (var c = 0; c < map[0].Count; c++)
            {
                char ch = map[r][c];
                if (poi_chars.Contains(ch))
                    poi_positions[ch] = (r, c);
            }

        var visited = new HashSet<(char pos, string keysStr)>();

        var startKeys = new HashSet<char>();

        foreach (var key in allKeys)
        {
            if (keys.Contains(key)) continue;
            
            if (IsIndependent(keys, key, dependencies))
                startKeys.Add(key);
        }

        var queue = new Queue<(char pos, HashSet<char> keys, int steps)>();
        queue.Enqueue(('@', startKeys, 0));
        var res = int.MaxValue;
        while (queue.Count > 0)
        {
            var (pos, curKeys, steps) = queue.Dequeue();
            if (steps > res) continue;
            var keysStr = string.Concat(curKeys.OrderBy(x => x));
            if (visited.Contains((pos, keysStr)))
                continue;
            visited.Add((pos, keysStr));

            if (keys.IsSubsetOf(curKeys))
                res = Math.Min(res, steps);

            foreach (var pair in distances)
            {
                var from = pair.Key.from;
                var to = pair.Key.to;
                var dist = pair.Value;

                if (from != pos) continue;

                if (doors_char.Contains(to) && !curKeys.Contains(char.ToLower(to)))
                    continue;

                var newKeys = new HashSet<char>(curKeys);
                if (keys_char.Contains(to))
                {
                    newKeys.Add(to);

                    if (dependencies.FromKeyDepends.ContainsKey(to))
                    {
                        var stack = new Stack<char>();
                        foreach (var key in dependencies.FromKeyDepends[to])
                            stack.Push(key);
                        while (stack.Count > 0)
                        {
                            var key = stack.Pop();
                            if (!keys.Contains(key) && IsIndependent(keys.Except(newKeys).ToHashSet(), key, dependencies))
                            {
                                newKeys.Add(key);
                                if (dependencies.FromKeyDepends.ContainsKey(key))
                                    foreach (var dependenceKey in dependencies.FromKeyDepends[key])
                                        stack.Push(dependenceKey);
                            }
                        }
                    }
                }

                var newKeysStr = string.Concat(newKeys.OrderBy(x => x));
                if (!visited.Contains((to, newKeysStr)))
                    queue.Enqueue((to, newKeys, steps + dist));
            }
        }

        if (res == int.MaxValue)
            return -1;
        return res;
    }

    public static bool IsIndependent(HashSet<char> keys, char key, Dependancies dependencies)
    {
        var stack = new Stack<char>();
        stack.Push(key);
        while (stack.Count > 0)
        {
            var curKey = stack.Pop();
            foreach (var dependenceKey in dependencies.KeyDependsOn[curKey])
            {
                if (keys.Contains(dependenceKey))
                    return false;

                stack.Push(dependenceKey);
            }
        }

        return true;
    }

    static Dependancies BuildKeyDependencyGraph(List<List<char>> map, List<(int x, int y)> robotStarts)
    {
        var dependencies = new Dictionary<char, HashSet<char>>();
        var reverseDependencies = new Dictionary<char, HashSet<char>>();
        var directions = new[] { (-1, 0), (1, 0), (0, -1), (0, 1) };

        var allKeys = new HashSet<char>();

        for (var i = 0; i < map.Count; i++)
            for (var j = 0; j < map[i].Count; j++)
                if (char.IsLower(map[i][j])) allKeys.Add(map[i][j]);

        foreach (char targetKey in allKeys)
        {
            (var tx, var ty) = FindChar(map, targetKey);

            var queue = new Queue<((int x, int y), HashSet<char>)>();
            var visited = new HashSet<(int, int)>();

            foreach (var start in robotStarts)
            {
                queue.Enqueue((start, new HashSet<char>()));
                visited.Add(start);
            }

            while (queue.Count > 0)
            {
                var (pos, doors) = queue.Dequeue();
                if (pos == (tx, ty))
                {
                    if (!dependencies.ContainsKey(targetKey))
                        dependencies[targetKey] = new HashSet<char>();

                    foreach (var door in doors)
                    {
                        dependencies[targetKey].Add(char.ToLower(door));
                        if (!reverseDependencies.ContainsKey(char.ToLower(door)))
                            reverseDependencies[char.ToLower(door)] = new HashSet<char>();
                        reverseDependencies[char.ToLower(door)].Add(targetKey);
                    }

                    break;
                }

                foreach (var dir in directions)
                {
                    var nx = pos.x + dir.Item1;
                    var ny = pos.y + dir.Item2;

                    if (nx < 0 || ny < 0 || nx >= map.Count || ny >= map[0].Count) continue;
                    if (map[nx][ny] == '#' || visited.Contains((nx, ny))) continue;

                    var cell = map[nx][ny];

                    if (char.IsUpper(cell) && !allKeys.Contains(char.ToLower(cell)))
                        return null;

                    var nextDoors = new HashSet<char>(doors);
                    if (char.IsUpper(cell)) nextDoors.Add(cell);

                    visited.Add((nx, ny));
                    queue.Enqueue(((nx, ny), nextDoors));
                }
            }
        }

        return new Dependancies(dependencies, reverseDependencies);
    }
    public class Dependancies
    { 
        public Dictionary<char, HashSet<char>> KeyDependsOn { get; set; }
        public Dictionary<char, HashSet<char>> FromKeyDepends { get; set; }

        public Dependancies(Dictionary<char, HashSet<char>> keyDependsOn, Dictionary<char, HashSet<char>> fromKeyDepends)
        {
            KeyDependsOn = keyDependsOn;
            FromKeyDepends = fromKeyDepends;
        }
    }

    static (int, int) FindChar(List<List<char>> map, char target)
    {
        for (var i = 0; i < map.Count; i++)
            for (var j = 0; j < map[i].Count; j++)
                if (map[i][j] == target)
                    return (i, j);
        return (-1, -1);
    }

    static List<List<char>> ExtractRegion(List<List<char>> map, int startR, int startC)
    {
        var rows = map.Count;
        var cols = map[0].Count;
        var visited = new bool[rows, cols];
        var queue = new Queue<(int r, int c)>();
        queue.Enqueue((startR, startC));
        visited[startR, startC] = true;

        var regionPoints = new List<(int r, int c)>();

        while (queue.Count > 0)
        {
            var (r, c) = queue.Dequeue();
            regionPoints.Add((r, c));

            foreach (var (dr, dc) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
            {
                var nr = r + dr;
                var nc = c + dc;
                if (nr < 0 || nr >= rows || nc < 0 || nc >= cols) continue;
                if (visited[nr, nc] || map[nr][nc] == '#') continue;

                visited[nr, nc] = true;
                queue.Enqueue((nr, nc));
            }
        }

        var minR = regionPoints.Min(p => p.r);
        var maxR = regionPoints.Max(p => p.r);
        var minC = regionPoints.Min(p => p.c);
        var maxC = regionPoints.Max(p => p.c);

        var region = new List<List<char>>();
        for (var r = minR; r <= maxR; r++)
        {
            var row = new List<char>();
            for (var c = minC; c <= maxC; c++)
                row.Add(visited[r, c] ? map[r][c] : '#');

            region.Add(row);
        }

        return region;
    }

    static int Solve(List<List<char>> map)
    {
        var rows = map.Count;
        var cols = map[0].Count;

        var robotPositions = new List<(int r, int c)>();
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
                if (map[r][c] == '@')
                    robotPositions.Add((r, c));

        var allKeys = keys_char.Where(k => map.Any(row => row.Contains(k))).ToHashSet();
        var dependencies = BuildKeyDependencyGraph(map, robotPositions);

        var totalSteps = 0;

        foreach (var (startR, startC) in robotPositions)
        {
            var region = ExtractRegion(map, startR, startC);
            var steps = SolveSingleRobot(region, dependencies, allKeys);
            if (steps == -1) return -1;
            totalSteps += steps;
        }

        return totalSteps;
    }


    static void Main()
    {
        var data = GetInput();
        int result = Solve(data);

        if (result == -1)
        {
            Console.WriteLine("No solution found");
        }
        else
        {
            Console.WriteLine(result);
        }
    }
}
