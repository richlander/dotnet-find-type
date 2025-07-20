# TypeFinder - Type Location Tool for Coding Agents

TypeFinder is a .NET console application designed to help coding agents quickly locate types within a workspace. It provides fast, accurate type discovery across multiple programming languages and file types.

## Overview

This tool is specifically designed for integration with coding agents like Cursor, providing them with the ability to:
- Find exact locations of type definitions
- Search across multiple programming languages
- Get contextual information about type usage
- Filter results by file types and search criteria

## Installation

The tool is located in the `src/TypeFinder` directory. To use it, you need .NET 8.0 or later installed.

### Installing .NET (if not already installed)

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel LTS
export PATH="$HOME/.dotnet:$PATH"
```

## Usage

### Basic Usage

#### Using the wrapper script (recommended for agents):
```bash
./find-type.sh <type-name> [options]
```

#### Using the .NET application directly:
```bash
cd src/TypeFinder
dotnet run -- <workspace-path> <type-name>
```

### Examples

1. **Find a class named "User" in the workspace:**
   ```bash
   ./find-type.sh User
   ```

2. **Find exact type definitions only:**
   ```bash
   ./find-type.sh "MyClass" --exact-match
   ```

3. **Search only in C# and TypeScript files:**
   ```bash
   ./find-type.sh Controller --file-types .cs,.ts
   ```

4. **Case-sensitive search:**
   ```bash
   ./find-type.sh "MyClass" --case-sensitive
   ```

5. **Limit results to 10:**
   ```bash
   ./find-type.sh Service --max-results 10
   ```

6. **Search in a specific workspace:**
   ```bash
   ./find-type.sh User --workspace /path/to/other/workspace
   ```

## Command Line Options

| Option | Description | Default |
|--------|-------------|---------|
| `--exact-match` | Search for exact type name match (definitions only) | false |
| `--case-sensitive` | Use case-sensitive search | false |
| `--file-types` | Comma-separated list of file extensions to search | `.cs,.ts,.js,.py,.java,.go,.rs,.php` |
| `--max-results` | Maximum number of results to return | 50 |
| `--workspace` | Specify workspace path (wrapper script only) | current directory |

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
Context:     public class MyClass
>>>         {
>>>             private string _name;
>>>         }
Type: class
```

- **File**: Full path to the file containing the type
- **Line**: Line number where the type was found
- **Context**: Surrounding code context (2 lines before and after)
- **Type**: Type of the match (class, interface, struct, enum, record, type, typedef, function, or reference)

## Integration with Coding Agents

### For Cursor and Similar Agents

Coding agents can use this tool to:

1. **Locate type definitions** when they need to understand the structure of a type
2. **Find usage patterns** to understand how types are used throughout the codebase
3. **Get contextual information** to make better code suggestions
4. **Discover related types** by searching for similar naming patterns

### Example Agent Usage

```bash
# Agent wants to find the User class definition
./find-type.sh User --exact-match

# Agent wants to see all usages of a specific interface
./find-type.sh IUserService

# Agent wants to find all controller classes
./find-type.sh Controller --file-types .cs,.ts
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
```bash
dotnet publish -c Release -r linux-x64 --self-contained true
```

## Features

- **Multi-language Support**: Searches across multiple programming languages
- **Flexible Search**: Exact match or partial match options
- **Contextual Output**: Shows surrounding code for better understanding
- **Performance Optimized**: Fast recursive file searching
- **Error Handling**: Graceful handling of permission errors and invalid files
- **Configurable**: Customizable file types and result limits

## Error Handling

The tool handles various error conditions gracefully:
- Invalid workspace paths
- Permission denied errors when accessing files
- Unreadable or corrupted files
- Invalid command line arguments

## Performance Considerations

- The tool searches files recursively, so large workspaces may take longer
- Use `--file-types` to limit search scope for better performance
- Use `--max-results` to limit output size
- The tool skips files it cannot read and continues with the search

## Contributing

This tool is designed to be simple and focused. If you need additional features, consider:
- Adding support for more file types
- Implementing more sophisticated type detection patterns
- Adding support for different output formats (JSON, XML, etc.)
- Implementing caching for large workspaces