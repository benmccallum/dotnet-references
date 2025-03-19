using System.Text.Json.Serialization;

namespace BenMcCallum.DotNet.References
{
    internal class SolutionFilterFile
    {
        [JsonPropertyName("solution")]
        public Solution Solution { get; }

        public SolutionFilterFile(Solution solution)
        {
            Solution = solution;
        }
    }

    internal class Solution
    {
        /// <summary>
        /// Path to the solution file.
        /// </summary>
        [JsonPropertyName("path")]
        public string Path { get; }

        /// <summary>
        /// Paths to projects.
        /// </summary>
        [JsonPropertyName("projects")]
        public string[] Projects { get; }

        public Solution(string path, string[] projects)
        {
            Path = path;
            Projects = projects;
        }
    }
}
