using System.Collections.Generic;

namespace Formatextension.Demo;

public class InitializerExamples
{
    // Should be flagged: two elements on same line
    public Dictionary<string, int> SameLine = new()
    {
        ["a"] = 1,
        ["b"] = 2
    };

    // Should be flagged: three elements on same line
    public int[] ArraySameLine = new int[]
    {
        1,
        2,
        3
    };

    // Already multi-line — no flag
    public List<string> AlreadyFormatted = new()
    {
        "alpha",
        "beta",
        "gamma"
    };

    // Mixed: first two on same line — flagged
    public Dictionary<string, int> Mixed = new()
    {
        ["x"] = 10,
        ["y"] = 20,
        ["z"] = 30
    };

    // Nested initializer with inner on same line — flagged
    public Dictionary<string, Dictionary<string, int>> Nested = new()
    {
        ["inner"] = new()
        {
            ["a"] = 1,
            ["b"] = 2
        }
    };

    // Empty initializer — no flag
    public List<int> Empty = new() { };

    // Single element — no flag
    public Dictionary<string, int> Single = new() { ["only"] = 0 };
}
