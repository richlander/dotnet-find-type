# TypeFinder - Advanced Type Location Tool for Coding Agents

TypeFinder is a .NET console application designed to help coding agents quickly locate types within a workspace. It leverages the Roslyn compiler APIs to provide fast, accurate, and comprehensive type discovery across multiple programming languages and file types.

## Overview

This tool is specifically designed for integration with coding agents like Cursor, providing them with the ability to:
- Find exact locations of type definitions using Roslyn compiler APIs
- Discover all references to types throughout the codebase
- Search across multiple programming languages with fallback to file-based search
- Get contextual information about type usage with precise line numbers
- Filter results by file types and search criteria
- Understand type relationships and dependencies

## Installation

The tool is located in the `src/TypeFinder` directory. To use it, you need .NET 8.0 or later installed.

### Quick Installation

Use the provided installation script to publish and install the tool:

```bash
./install-typefinder.sh --install-dir ~/.local/bin
```

This will:
1. Publish the TypeFinder tool (using Native AOT if dependencies are available)
2. Create a symlink named `find-type` in the specified directory
3. Make the tool available for use



### Installing .NET (if not already installed)

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel LTS
export PATH="$HOME/.dotnet:$PATH"
```

## Usage

### Basic Usage

#### Using the installed tool (recommended):
```bash
find-type <workspace-path> <type-name> [options]
```

#### Using the .NET application directly:
```bash
cd src/TypeFinder
dotnet run -- <workspace-path> <type-name>
```

### Examples

1. **Find a class named "User" in the workspace:**
   ```bash
   find-type . User
   ```

2. **Find exact type definitions only:**
   ```bash
   find-type . "MyClass" --exact-match
   ```

3. **Search only in C# and TypeScript files:**
   ```bash
   find-type . Controller --file-types .cs,.ts
   ```

4. **Case-sensitive search:**
   ```bash
   find-type . "MyClass" --case-sensitive
   ```

5. **Limit results to 10:**
   ```bash
   find-type . Service --max-results 10
   ```

6. **Search in a specific workspace:**
   ```bash
   find-type /path/to/other/workspace User
   ```

7. **Find all references to a type:**
   ```bash
   find-type . MyClass --include-references
   ```

## Command Line Options

| Option | Description | Default |
|--------|-------------|---------|
| `--exact-match` | Search for exact type name match (definitions only) | false |
| `--case-sensitive` | Use case-sensitive search | false |
| `--file-types` | Comma-separated list of file extensions to search | `.cs,.ts,.js,.py,.java,.go,.rs,.php` |
| `--max-results` | Maximum number of results to return | 50 |
| `--include-references` | Include type references (not just definitions) | false |

## Supported File Types

By default, TypeFinder searches in the following file types:
- `.cs` - C# files
- `.ts` - TypeScript files
- `.js` - JavaScript files
- `.py` - Python files
- `.java` - Java files
- `.go` - Go files
- `.rs` - Rust files
- `.php` - PHP files

You can customize this list using the `--file-types` option.

## Output Format

The tool provides structured output with the following information for each match:

```
File: /path/to/file.cs
Line: 42
Type: MyClass
Kind: class
Context:     public class MyClass
>>>         {
>>>             private string _name;
>>>         }
```

- **File**: Full path to the file containing the type
- **Line**: Line number where the type was found
- **Type**: The actual type name that was found
- **Kind**: Type of the match (class, interface, struct, enum, record, namespace, method, property, field, event, reference, or symbol)
- **Context**: Surrounding code context (2 lines before and after)

## Integration with Coding Agents

### For Cursor and Similar Agents

Coding agents can use this tool to:

1. **Locate type definitions** when they need to understand the structure of a type
2. **Find all references** to understand how types are used throughout the codebase
3. **Get contextual information** to make better code suggestions
4. **Discover related types** by searching for similar naming patterns
5. **Understand type relationships** by analyzing dependencies and usage patterns

### Example Agent Usage

```bash
# Agent wants to find the User class definition
find-type . User --exact-match

# Agent wants to see all usages of a specific interface
find-type . IUserService --include-references

# Agent wants to find all controller classes
find-type . Controller --file-types .cs,.ts

# Agent wants to understand all references to a type
find-type . MyClass --include-references --max-results 100
```

## Building and Running

### Build the Application
```bash
cd src/TypeFinder
dotnet build
```

### Run the Application
```bash
dotnet run -- <arguments>
```

### Create a Standalone Executable

To publish an app in the default mode, use `dotnet publish`. That produces a release binary for the current machine:

```bash
dotnet publish -c Release -r linux-x64 --self-contained true
```



## Features

- **Roslyn Integration**: Uses Microsoft's Roslyn compiler APIs for accurate type analysis
- **Multi-language Support**: Searches across multiple programming languages with fallback to file-based search
- **Comprehensive Discovery**: Finds both type definitions and all references throughout the codebase
- **Flexible Search**: Exact match or partial match options with case sensitivity control
- **Contextual Output**: Shows surrounding code for better understanding
- **Performance Optimized**: Fast compilation-based searching with configurable limits
- **Error Handling**: Graceful handling of compilation errors and invalid files
- **Configurable**: Customizable file types and result limits

## Error Handling

The tool handles various error conditions gracefully:
- Invalid workspace paths
- Permission denied errors when accessing files
- Unreadable or corrupted files
- Compilation errors in projects
- Invalid command line arguments

## Performance Considerations

- The tool uses Roslyn compilation for accurate analysis, which may take longer for large projects
- Use `--file-types` to limit search scope for better performance
- Use `--max-results` to limit output size
- The tool falls back to file-based search if compilation fails
- The tool skips files it cannot read and continues with the search

## Contributing

This tool is designed to be simple and focused. If you need additional features, consider:
- Adding support for more file types
- Implementing more sophisticated type detection patterns
- Adding support for different output formats (JSON, XML, etc.)
- Implementing caching for large workspaces