using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace TypeFinder
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: TypeFinder <workspace-path> <type-name> [options]");
                Console.WriteLine();
                Console.WriteLine("Options:");
                Console.WriteLine("  --exact-match    Search for exact type name match");
                Console.WriteLine("  --case-sensitive Use case-sensitive search");
                Console.WriteLine("  --file-types     Comma-separated list of file extensions to search (default: .cs,.ts,.js,.py,.java,.go,.rs,.php)");
                Console.WriteLine("  --max-results    Maximum number of results to return (default: 50)");
                Console.WriteLine();
                Console.WriteLine("Examples:");
                Console.WriteLine("  TypeFinder /path/to/workspace User");
                Console.WriteLine("  TypeFinder /path/to/workspace \"MyClass\" --exact-match");
                Console.WriteLine("  TypeFinder /path/to/workspace Controller --file-types .cs,.ts");
                return;
            }

            string workspacePath = args[0];
            string typeName = args[1];

            // Parse options
            var options = ParseOptions(args.Skip(2).ToArray());

            if (!Directory.Exists(workspacePath))
            {
                Console.WriteLine($"Error: Workspace path '{workspacePath}' does not exist.");
                Environment.Exit(1);
            }

            try
            {
                var results = FindType(workspacePath, typeName, options);
                
                if (results.Count == 0)
                {
                    Console.WriteLine($"No types found matching '{typeName}' in workspace '{workspacePath}'");
                    return;
                }

                Console.WriteLine($"Found {results.Count} result(s) for type '{typeName}':");
                Console.WriteLine();

                foreach (var result in results.Take(options.MaxResults))
                {
                    Console.WriteLine($"File: {result.FilePath}");
                    Console.WriteLine($"Line: {result.LineNumber}");
                    Console.WriteLine($"Context: {result.Context}");
                    Console.WriteLine($"Type: {result.Type}");
                    Console.WriteLine();
                }

                if (results.Count > options.MaxResults)
                {
                    Console.WriteLine($"... and {results.Count - options.MaxResults} more results (use --max-results to see more)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static SearchOptions ParseOptions(string[] args)
        {
            var options = new SearchOptions();
            
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--exact-match":
                        options.ExactMatch = true;
                        break;
                    case "--case-sensitive":
                        options.CaseSensitive = true;
                        break;
                    case "--file-types":
                        if (i + 1 < args.Length)
                        {
                            options.FileTypes = args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries);
                        }
                        break;
                    case "--max-results":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int maxResults))
                        {
                            options.MaxResults = maxResults;
                        }
                        break;
                }
            }

            return options;
        }

        static List<TypeResult> FindType(string workspacePath, string typeName, SearchOptions options)
        {
            var results = new List<TypeResult>();
            var fileExtensions = options.FileTypes ?? new[] { ".cs", ".ts", ".js", ".py", ".java", ".go", ".rs", ".php" };

            foreach (var file in GetFilesRecursively(workspacePath, fileExtensions))
            {
                try
                {
                    var fileResults = SearchFile(file, typeName, options);
                    results.AddRange(fileResults);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not search file {file}: {ex.Message}");
                }
            }

            return results;
        }

        static IEnumerable<string> GetFilesRecursively(string directory, string[] extensions)
        {
            try
            {
                return Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
                    .Where(file => extensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));
            }
            catch (UnauthorizedAccessException)
            {
                return Enumerable.Empty<string>();
            }
        }

        static List<TypeResult> SearchFile(string filePath, string typeName, SearchOptions options)
        {
            var results = new List<TypeResult>();
            var lines = File.ReadAllLines(filePath);
            var comparison = options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineNumber = i + 1;

                if (options.ExactMatch)
                {
                    // Look for exact type definitions
                    if (IsTypeDefinition(line, typeName, comparison))
                    {
                        results.Add(new TypeResult
                        {
                            FilePath = filePath,
                            LineNumber = lineNumber,
                            Context = GetContext(lines, i),
                            Type = DetermineTypeFromLine(line)
                        });
                    }
                }
                else
                {
                    // Look for type name mentions
                    if (line.IndexOf(typeName, comparison) >= 0)
                    {
                        results.Add(new TypeResult
                        {
                            FilePath = filePath,
                            LineNumber = lineNumber,
                            Context = GetContext(lines, i),
                            Type = DetermineTypeFromLine(line)
                        });
                    }
                }
            }

            return results;
        }

        static bool IsTypeDefinition(string line, string typeName, StringComparison comparison)
        {
            // Common patterns for type definitions
            var patterns = new[]
            {
                $@"\b(class|interface|struct|enum|record)\s+{Regex.Escape(typeName)}\b",
                $@"\b(type|typedef)\s+{Regex.Escape(typeName)}\b",
                $@"\b{Regex.Escape(typeName)}\s*[:=]",
                $@"\b{Regex.Escape(typeName)}\s*\(",
                $@"\b{Regex.Escape(typeName)}\s*<",
                $@"\b{Regex.Escape(typeName)}\s*\["
            };

            return patterns.Any(pattern => Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase));
        }

        static string GetContext(string[] lines, int lineIndex)
        {
            var start = Math.Max(0, lineIndex - 2);
            var end = Math.Min(lines.Length - 1, lineIndex + 2);
            
            var contextLines = new List<string>();
            for (int i = start; i <= end; i++)
            {
                var prefix = i == lineIndex ? ">>> " : "    ";
                contextLines.Add($"{prefix}{lines[i]}");
            }
            
            return string.Join(Environment.NewLine, contextLines);
        }

        static string DetermineTypeFromLine(string line)
        {
            if (line.Contains("class ")) return "class";
            if (line.Contains("interface ")) return "interface";
            if (line.Contains("struct ")) return "struct";
            if (line.Contains("enum ")) return "enum";
            if (line.Contains("record ")) return "record";
            if (line.Contains("type ")) return "type";
            if (line.Contains("typedef ")) return "typedef";
            if (line.Contains("function ")) return "function";
            if (line.Contains("def ")) return "function";
            if (line.Contains("func ")) return "function";
            return "reference";
        }
    }

    class SearchOptions
    {
        public bool ExactMatch { get; set; } = false;
        public bool CaseSensitive { get; set; } = false;
        public string[]? FileTypes { get; set; } = null;
        public int MaxResults { get; set; } = 50;
    }

    class TypeResult
    {
        public string FilePath { get; set; } = "";
        public int LineNumber { get; set; }
        public string Context { get; set; } = "";
        public string Type { get; set; } = "";
    }
}
