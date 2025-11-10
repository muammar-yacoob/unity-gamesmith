namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// MCP Server Configuration - Use @spark-apps/unity-mcp
    /// </summary>
    public static class MCPServerConfig
    {
        /// <summary>
        /// MCP server package to use
        /// </summary>
        public static string ServerPackage => "@spark-apps/unity-mcp";

        /// <summary>
        /// Command to run the MCP server
        /// </summary>
        public static string ServerCommand => "npx";

        /// <summary>
        /// Additional arguments
        /// </summary>
        public static string[] AdditionalArgs => new string[] { "-y" };

        /// <summary>
        /// Get full command arguments
        /// </summary>
        public static string[] GetServerArgs()
        {
            var args = new System.Collections.Generic.List<string>();
            args.Add(ServerPackage);
            if (AdditionalArgs != null && AdditionalArgs.Length > 0)
            {
                args.AddRange(AdditionalArgs);
            }
            return args.ToArray();
        }

        /// <summary>
        /// Get the command to run (for compatibility with existing code)
        /// </summary>
        public static string GetCommand()
        {
            return ServerCommand;
        }

        /// <summary>
        /// Display name for logging
        /// </summary>
        public static string DisplayName => "unity-mcp";
    }
}
