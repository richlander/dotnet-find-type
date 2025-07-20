using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

if (args.Length < 2)
{
    Console.WriteLine("Usage: TypeFinder <workspace-path> <type-name> [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --exact-match    Search for exact type name match");
    Console.WriteLine("  --case-sensitive Use case-sensitive search");
    Console.WriteLine("  --file-types     Comma-separated list of file extensions to search (default: .cs,.ts,.js,.py,.java,.go,.rs,.php)");
    Console.WriteLine("  --max-results    Maximum number of results to return (default: 50)");
    Console.WriteLine("  --include-references Include type references (not just definitions)");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  TypeFinder /path/to/workspace User");
    Console.WriteLine("  TypeFinder /path/to/workspace \"MyClass\" --exact-match");
    Console.WriteLine("  TypeFinder /path/to/workspace Controller --file-types .cs,.ts");
    return 1;
}

string workspacePath = args[0];
string typeName = args[1];

// Parse options
var options = ParseOptions(args.Skip(2).ToArray());

if (!Directory.Exists(workspacePath))
{
    Console.WriteLine($"Error: Workspace path '{workspacePath}' does not exist.");
    return 1;
}

try
{
    var results = await FindTypeAsync(workspacePath, typeName, options);
    
    if (results.Count == 0)
    {
        Console.WriteLine($"No types found matching '{typeName}' in workspace '{workspacePath}'");
        return 0;
    }

    Console.WriteLine($"Found {results.Count} result(s) for type '{typeName}':");
    Console.WriteLine();

    foreach (var result in results.Take(options.MaxResults))
    {
        Console.WriteLine($"File: {result.FilePath}");
        Console.WriteLine($"Line: {result.LineNumber}");
        Console.WriteLine($"Type: {result.Type}");
        Console.WriteLine($"Kind: {result.Kind}");
        Console.WriteLine($"Context: {result.Context}");
        Console.WriteLine();
    }

    if (results.Count > options.MaxResults)
    {
        Console.WriteLine($"... and {results.Count - options.MaxResults} more results (use --max-results to see more)");
    }

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    return 1;
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
            case "--include-references":
                options.IncludeReferences = true;
                break;
        }
    }

    return options;
}

static async Task<List<TypeResult>> FindTypeAsync(string workspacePath, string typeName, SearchOptions options)
{
    var results = new List<TypeResult>();

    // Try to find solution files first
    var solutionFiles = Directory.GetFiles(workspacePath, "*.sln", SearchOption.AllDirectories);
    
    if (solutionFiles.Length > 0)
    {
        foreach (var solutionFile in solutionFiles)
        {
            try
            {
                var solutionResults = await SearchSolutionAsync(solutionFile, typeName, options);
                results.AddRange(solutionResults);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not process solution {solutionFile}: {ex.Message}");
            }
        }
    }

    // If no solution files or no results, try project files
    if (results.Count == 0)
    {
        var projectFiles = Directory.GetFiles(workspacePath, "*.csproj", SearchOption.AllDirectories);
        
        foreach (var projectFile in projectFiles)
        {
            try
            {
                var projectResults = await SearchProjectAsync(projectFile, typeName, options);
                results.AddRange(projectResults);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not process project {projectFile}: {ex.Message}");
            }
        }
    }

    // If still no results, fall back to file-based search
    if (results.Count == 0)
    {
        var fileResults = await SearchFilesAsync(workspacePath, typeName, options);
        results.AddRange(fileResults);
    }

    return results;
}

static async Task<List<TypeResult>> SearchSolutionAsync(string solutionPath, string typeName, SearchOptions options)
{
    var results = new List<TypeResult>();
    
    using var workspace = MSBuildWorkspace.Create();
    var solution = await workspace.OpenSolutionAsync(solutionPath);

    foreach (var project in solution.Projects)
    {
        try
        {
            var projectResults = await SearchProjectSymbolsAsync(project, typeName, options);
            results.AddRange(projectResults);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not process project {project.Name}: {ex.Message}");
        }
    }

    return results;
}

static async Task<List<TypeResult>> SearchProjectAsync(string projectPath, string typeName, SearchOptions options)
{
    using var workspace = MSBuildWorkspace.Create();
    var project = await workspace.OpenProjectAsync(projectPath);
    return await SearchProjectSymbolsAsync(project, typeName, options);
}

static async Task<List<TypeResult>> SearchProjectSymbolsAsync(Project project, string typeName, SearchOptions options)
{
    var results = new List<TypeResult>();
    var compilation = await project.GetCompilationAsync();
    
    if (compilation == null) return results;

    // Search for type symbols
    var symbols = new List<ISymbol>();

    // Search in global namespace
    var globalNamespace = compilation.GlobalNamespace;
    symbols.AddRange(FindSymbolsInNamespace(globalNamespace, typeName, options));

    // Search in all namespaces
    foreach (var namespaceSymbol in GetAllNamespaces(globalNamespace))
    {
        symbols.AddRange(FindSymbolsInNamespace(namespaceSymbol, typeName, options));
    }

    foreach (var symbol in symbols)
    {
        var locations = symbol.Locations;
        foreach (var location in locations)
        {
            if (location.IsInSource)
            {
                var result = await CreateTypeResultAsync(location, symbol, project, options);
                if (result != null)
                {
                    results.Add(result);
                }
            }
        }

        // If including references, also find all references to this symbol
        if (options.IncludeReferences)
        {
            var references = await SymbolFinder.FindReferencesAsync(symbol, project.Solution);
            foreach (var reference in references)
            {
                foreach (var location in reference.Locations)
                {
                    var result = await CreateTypeResultAsync(location.Location, symbol, project, options);
                    if (result != null)
                    {
                        result.Kind = "reference";
                        results.Add(result);
                    }
                }
            }
        }
    }

    return results;
}

static IEnumerable<ISymbol> FindSymbolsInNamespace(INamespaceSymbol namespaceSymbol, string typeName, SearchOptions options)
{
    var symbols = new List<ISymbol>();

    // Search for types in this namespace
    foreach (var type in namespaceSymbol.GetTypeMembers())
    {
        if (MatchesTypeName(type.Name, typeName, options))
        {
            symbols.Add(type);
        }
    }

    // Search in nested namespaces
    foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
    {
        symbols.AddRange(FindSymbolsInNamespace(nestedNamespace, typeName, options));
    }

    return symbols;
}

static IEnumerable<INamespaceSymbol> GetAllNamespaces(INamespaceSymbol namespaceSymbol)
{
    var namespaces = new List<INamespaceSymbol>();
    
    foreach (var member in namespaceSymbol.GetMembers())
    {
        if (member is INamespaceSymbol nestedNamespace)
        {
            namespaces.Add(nestedNamespace);
            namespaces.AddRange(GetAllNamespaces(nestedNamespace));
        }
    }

    return namespaces;
}

static bool MatchesTypeName(string symbolName, string searchName, SearchOptions options)
{
    if (options.ExactMatch)
    {
        return options.CaseSensitive 
            ? symbolName == searchName 
            : string.Equals(symbolName, searchName, StringComparison.OrdinalIgnoreCase);
    }
    else
    {
        return options.CaseSensitive 
            ? symbolName.Contains(searchName) 
            : symbolName.IndexOf(searchName, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}

static async Task<TypeResult?> CreateTypeResultAsync(Location location, ISymbol symbol, Project project, SearchOptions options)
{
    if (!location.IsInSource) return null;

    var sourceText = await location.SourceTree!.GetTextAsync();
    var lineSpan = sourceText.Lines.GetLinePositionSpan(location.SourceSpan);
    
    var context = GetContextFromSourceText(sourceText, lineSpan.Start.Line);
    var typeKind = GetTypeKind(symbol);

    return new TypeResult
    {
        FilePath = location.SourceTree!.FilePath,
        LineNumber = lineSpan.Start.Line + 1,
        Type = symbol.Name,
        Kind = typeKind,
        Context = context
    };
}

static string GetContextFromSourceText(SourceText sourceText, int lineNumber)
{
    var lines = sourceText.Lines;
    var start = Math.Max(0, lineNumber - 2);
    var end = Math.Min(lines.Count - 1, lineNumber + 2);
    
    var contextLines = new List<string>();
    for (int i = start; i <= end; i++)
    {
        var line = lines[i].ToString();
        var prefix = i == lineNumber ? ">>> " : "    ";
        contextLines.Add($"{prefix}{line}");
    }
    
    return string.Join(Environment.NewLine, contextLines);
}

static string GetTypeKind(ISymbol symbol)
{
    return symbol switch
    {
        ITypeSymbol typeSymbol => typeSymbol.TypeKind.ToString().ToLower(),
        INamespaceSymbol => "namespace",
        IMethodSymbol => "method",
        IPropertySymbol => "property",
        IFieldSymbol => "field",
        IEventSymbol => "event",
        _ => "symbol"
    };
}

static async Task<List<TypeResult>> SearchFilesAsync(string workspacePath, string typeName, SearchOptions options)
{
    var results = new List<TypeResult>();
    var fileExtensions = options.FileTypes ?? new[] { ".cs", ".ts", ".js", ".py", ".java", ".go", ".rs", ".php" };

    foreach (var file in GetFilesRecursively(workspacePath, fileExtensions))
    {
        try
        {
            var fileResults = await SearchFileAsync(file, typeName, options);
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

static async Task<List<TypeResult>> SearchFileAsync(string filePath, string typeName, SearchOptions options)
{
    var results = new List<TypeResult>();
    var lines = await File.ReadAllLinesAsync(filePath);
    var comparison = options.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    for (int i = 0; i < lines.Length; i++)
    {
        var line = lines[i];
        var lineNumber = i + 1;

        if (options.ExactMatch)
        {
            if (IsTypeDefinition(line, typeName, comparison))
            {
                results.Add(new TypeResult
                {
                    FilePath = filePath,
                    LineNumber = lineNumber,
                    Context = GetContextFromLines(lines, i),
                    Type = typeName,
                    Kind = DetermineTypeFromLine(line)
                });
            }
        }
        else
        {
            if (line.IndexOf(typeName, comparison) >= 0)
            {
                results.Add(new TypeResult
                {
                    FilePath = filePath,
                    LineNumber = lineNumber,
                    Context = GetContextFromLines(lines, i),
                    Type = typeName,
                    Kind = DetermineTypeFromLine(line)
                });
            }
        }
    }

    return results;
}

static bool IsTypeDefinition(string line, string typeName, StringComparison comparison)
{
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

static string GetContextFromLines(string[] lines, int lineIndex)
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

class SearchOptions
{
    public bool ExactMatch { get; set; } = false;
    public bool CaseSensitive { get; set; } = false;
    public string[]? FileTypes { get; set; } = null;
    public int MaxResults { get; set; } = 50;
    public bool IncludeReferences { get; set; } = false;
}

class TypeResult
{
    public string FilePath { get; set; } = "";
    public int LineNumber { get; set; }
    public string Type { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Context { get; set; } = "";
}
