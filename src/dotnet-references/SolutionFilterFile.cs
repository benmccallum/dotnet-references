using System.Text.Json.Serialization;

namespace BenMcCallum.DotNet.References;

internal class SolutionFilterFile(Solution solution)
{
    [JsonPropertyName("solution")]
    public Solution Solution { get; } = solution;
}

internal class Solution(string path, string[] projects)
{
    /// <summary>
    /// Path to the solution file.
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; } = path;

    /// <summary>
    /// Paths to projects.
    /// </summary>
    [JsonPropertyName("projects")]
    public string[] Projects { get; } = projects;
}
