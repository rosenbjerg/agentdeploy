using System.Linq;

namespace AgentDeploy.Services
{
    public static class PathUtils
    {
        public static string EscapeWhitespaceInPath(string path, char escapeChar = '"')
        {
            if (path.Contains(" "))
                return $"{escapeChar}{path}{escapeChar}";
            return path;
        }
        
        public static string Combine(char separator, params string[] pathParts)
        {
            var combined = string.Join(separator, pathParts.Select(p => p.Trim(separator)));
            if (pathParts.FirstOrDefault()?.StartsWith(separator) == true) 
                combined = separator + combined;
            if (pathParts.LastOrDefault()?.EndsWith(separator) == true) 
                combined = combined + separator;

            return combined;
        }
    }
}