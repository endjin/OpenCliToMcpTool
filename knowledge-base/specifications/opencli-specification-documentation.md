# OpenCLI Specification Documentation

## Overview

OpenCLI (Open Command Line Interface) is a platform and language-agnostic specification for defining CLI tool interfaces. Similar to how OpenAPI standardizes REST API documentation, OpenCLI aims to standardize CLI tool documentation and enable programmatic interactions.

### Key Benefits
- **Documentation Generation**: Automatically generate comprehensive CLI documentation
- **Client Interface Generation**: Create strongly-typed wrappers for CLI tools
- **Tool Automation**: Enable programmatic interaction with CLI applications
- **Change Detection**: Monitor CLI API changes between versions
- **Auto-completion**: Generate shell completion scripts

## Core Concepts

### Problem Statement
Currently, understanding how to use a CLI tool requires:
- Reading documentation (if available)
- Examining source code
- Trial and error with commands

OpenCLI solves this by providing a machine-readable specification that describes:
- Available commands and subcommands
- Required and optional arguments
- Command options and flags
- Exit codes and their meanings
- Usage examples

## Specification Structure

### Document Format
- Uses JSON or YAML format
- Follows semantic versioning (major.minor.patch)
- Current version: 0.1 (Draft)

### Root Object
```json
{
  "opencli": "0.1",
  "info": {
    "title": "Tool Name",
    "description": "Tool description",
    "version": "1.0.0"
  },
  "commands": [],
  "options": [],
  "arguments": [],
  "exitCodes": [],
  "examples": []
}
```

### Key Objects

#### 1. CliInfo Object
Contains metadata about the CLI application:
- `title`: Application name
- `description`: Brief description
- `version`: Application version
- `contact`: Contact information
- `license`: License details

#### 2. Command Object
Defines individual commands:
- `name`: Command name
- `description`: Command purpose
- `aliases`: Alternative names
- `arguments`: Positional arguments
- `options`: Command flags/options
- `commands`: Subcommands
- `exitCodes`: Command-specific exit codes

#### 3. Argument Object
Describes positional arguments:
- `name`: Argument identifier
- `description`: Argument purpose
- `required`: Whether mandatory
- `ordinal`: Position in command
- `arity`: Number of values accepted
- `acceptedValues`: Valid values

#### 4. Option Object
Defines command options/flags:
- `name`: Option name
- `aliases`: Short forms (e.g., -h for --help)
- `description`: Option purpose
- `arguments`: Option parameters
- `group`: Logical grouping
- `required`: Whether mandatory

#### 5. ExitCode Object
Documents exit codes:
- `code`: Numeric exit code
- `description`: What the code means

### Conventions Object
Defines CLI behavior patterns:
- `optionGrouping`: How options are grouped
- `optionSeparator`: Character separating options from values

## Implementation Example

### Basic CLI Definition
```yaml
opencli: "0.1"
info:
  title: "MyTool"
  version: "1.0.0"
  description: "A sample CLI tool"
commands:
  - name: "init"
    description: "Initialize a new project"
    options:
      - name: "template"
        aliases: ["t"]
        description: "Project template to use"
        arguments:
          - name: "template-name"
            required: true
    arguments:
      - name: "project-name"
        description: "Name of the project"
        required: true
        ordinal: 0
```

## Use Cases

### 1. Documentation Generation
- Generate man pages
- Create web documentation
- Build interactive help systems

### 2. Tool Integration
- IDE integrations with auto-completion
- CI/CD pipeline automation
- Cross-platform tool wrappers

### 3. Testing and Validation
- Validate CLI inputs
- Generate test cases
- Ensure backward compatibility

## Future Development Considerations

### Integration Opportunities
1. **Model Context Protocol (MCP)**: Align with MCP for AI-assisted CLI interactions
2. **Language-Specific Generators**: Create generators for various programming languages
3. **Shell Integration**: Native shell support for OpenCLI specs

### Potential Extensions
1. **Interactive Mode Support**: Define interactive CLI behaviors
2. **Environment Variables**: Specify environment variable dependencies
3. **Configuration Files**: Document configuration file formats
4. **Plugin Systems**: Describe extensible CLI architectures

### Tooling Ecosystem
1. **Validators**: Tools to validate OpenCLI specifications
2. **Converters**: Convert existing CLI tools to OpenCLI format
3. **Linters**: Ensure specification best practices
4. **Diff Tools**: Compare CLI versions

## Getting Started

### Creating an OpenCLI Specification
1. Choose JSON or YAML format
2. Start with minimal required fields (opencli version and info)
3. Define your commands hierarchically
4. Add arguments and options
5. Document exit codes
6. Provide usage examples

### Best Practices
- Use clear, consistent naming conventions
- Provide comprehensive descriptions
- Group related options logically
- Document all exit codes
- Include practical examples
- Version your specifications

## Community and Contribution

- **GitHub**: https://github.com/spectreconsole/open-cli
- **Website**: https://opencli.org
- **Status**: Draft specification seeking feedback
- **License**: MIT

The specification is actively seeking community input to refine and improve the standard before its first stable release.

## Complete JSON Schema Definition

The following is the complete JSON Schema for OpenCLI v0.1 that can be used for code generation:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "https://opencli.org/schemas/v0.1/opencli.json",
  "title": "OpenCLI Specification",
  "description": "OpenCLI specification for describing command-line interfaces",
  "type": "object",
  "required": ["opencli", "info"],
  "properties": {
    "opencli": {
      "type": "string",
      "description": "The OpenCLI specification version",
      "pattern": "^[0-9]+\\.[0-9]+$"
    },
    "info": {
      "$ref": "#/definitions/CliInfo"
    },
    "conventions": {
      "$ref": "#/definitions/Conventions"
    },
    "arguments": {
      "type": "array",
      "items": {
        "$ref": "#/definitions/Argument"
      }
    },
    "options": {
      "type": "array",
      "items": {
        "$ref": "#/definitions/Option"
      }
    },
    "commands": {
      "type": "array",
      "items": {
        "$ref": "#/definitions/Command"
      }
    },
    "exitCodes": {
      "type": "array",
      "items": {
        "$ref": "#/definitions/ExitCode"
      }
    },
    "examples": {
      "type": "array",
      "items": {
        "$ref": "#/definitions/Example"
      }
    },
    "metadata": {
      "type": "object",
      "description": "Custom metadata extensions"
    }
  },
  "definitions": {
    "CliInfo": {
      "type": "object",
      "required": ["title", "version"],
      "properties": {
        "title": {
          "type": "string",
          "description": "The title of the application"
        },
        "description": {
          "type": "string",
          "description": "A short description of the application"
        },
        "version": {
          "type": "string",
          "description": "The version of the application"
        },
        "contact": {
          "$ref": "#/definitions/Contact"
        },
        "license": {
          "$ref": "#/definitions/License"
        }
      },
      "unevaluatedProperties": true
    },
    "Contact": {
      "type": "object",
      "properties": {
        "name": {
          "type": "string"
        },
        "url": {
          "type": "string",
          "format": "uri"
        },
        "email": {
          "type": "string",
          "format": "email"
        }
      },
      "unevaluatedProperties": true
    },
    "License": {
      "type": "object",
      "required": ["name"],
      "properties": {
        "name": {
          "type": "string"
        },
        "url": {
          "type": "string",
          "format": "uri"
        }
      },
      "unevaluatedProperties": true
    },
    "Conventions": {
      "type": "object",
      "properties": {
        "optionGrouping": {
          "type": "boolean",
          "description": "Whether multiple short options can be grouped"
        },
        "optionSeparator": {
          "type": "string",
          "description": "Character that separates option from its value"
        }
      },
      "unevaluatedProperties": true
    },
    "Command": {
      "type": "object",
      "required": ["name"],
      "properties": {
        "name": {
          "type": "string",
          "description": "The name of the command"
        },
        "description": {
          "type": "string",
          "description": "A short description of the command"
        },
        "aliases": {
          "type": "array",
          "items": {
            "type": "string"
          },
          "description": "Alternative names for the command"
        },
        "arguments": {
          "type": "array",
          "items": {
            "$ref": "#/definitions/Argument"
          }
        },
        "options": {
          "type": "array",
          "items": {
            "$ref": "#/definitions/Option"
          }
        },
        "commands": {
          "type": "array",
          "items": {
            "$ref": "#/definitions/Command"
          },
          "description": "Sub-commands"
        },
        "exitCodes": {
          "type": "array",
          "items": {
            "$ref": "#/definitions/ExitCode"
          }
        },
        "examples": {
          "type": "array",
          "items": {
            "$ref": "#/definitions/Example"
          }
        },
        "metadata": {
          "type": "object"
        }
      },
      "unevaluatedProperties": true
    },
    "Argument": {
      "type": "object",
      "required": ["name"],
      "properties": {
        "name": {
          "type": "string",
          "description": "The name of the argument"
        },
        "description": {
          "type": "string",
          "description": "A short description of the argument"
        },
        "required": {
          "type": "boolean",
          "description": "Whether the argument is required"
        },
        "ordinal": {
          "type": "integer",
          "description": "The position of the argument"
        },
        "arity": {
          "type": "string",
          "enum": ["exactly-one", "zero-or-one", "zero-or-more", "one-or-more"],
          "description": "How many values the argument accepts"
        },
        "acceptedValues": {
          "type": "array",
          "items": {
            "type": "string"
          },
          "description": "List of accepted values"
        },
        "metadata": {
          "type": "object"
        }
      },
      "unevaluatedProperties": true
    },
    "Option": {
      "type": "object",
      "required": ["name"],
      "properties": {
        "name": {
          "type": "string",
          "description": "The name of the option"
        },
        "aliases": {
          "type": "array",
          "items": {
            "type": "string"
          },
          "description": "Alternative names for the option"
        },
        "description": {
          "type": "string",
          "description": "A short description of the option"
        },
        "arguments": {
          "type": "array",
          "items": {
            "$ref": "#/definitions/Argument"
          },
          "description": "Arguments for the option"
        },
        "group": {
          "type": "string",
          "description": "Logical grouping for the option"
        },
        "required": {
          "type": "boolean",
          "description": "Whether the option is required"
        },
        "metadata": {
          "type": "object"
        }
      },
      "unevaluatedProperties": true
    },
    "ExitCode": {
      "type": "object",
      "required": ["code"],
      "properties": {
        "code": {
          "type": "integer",
          "description": "The numeric exit code"
        },
        "description": {
          "type": "string",
          "description": "Description of what the exit code means"
        },
        "metadata": {
          "type": "object"
        }
      },
      "unevaluatedProperties": true
    },
    "Example": {
      "type": "object",
      "required": ["command"],
      "properties": {
        "command": {
          "type": "string",
          "description": "The example command"
        },
        "description": {
          "type": "string",
          "description": "Description of what the example does"
        },
        "metadata": {
          "type": "object"
        }
      },
      "unevaluatedProperties": true
    }
  }
}
```

## Using the Schema for Code Generation

### Code Generation Approaches

1. **TypeScript/JavaScript**
   - Use tools like `json-schema-to-typescript` to generate TypeScript interfaces
   - Example: `npx json-schema-to-typescript opencli-schema.json > opencli-types.ts`

2. **Python**
   - Use `datamodel-code-generator` to generate Pydantic models
   - Example: `datamodel-codegen --input opencli-schema.json --output opencli_models.py`

3. **Go**
   - Use `go-jsonschema` to generate Go structs
   - Example: `go-jsonschema -p opencli opencli-schema.json`

4. **Java**
   - Use `jsonschema2pojo` to generate Java POJOs
   - Maven/Gradle plugins available for build integration

5. **C#**
   - Use `Corvus.JsonSchema` to generate C# classes
   - Available as NuGet package with comprehensive JSON Schema support
   - Example: `dotnet tool install -g Corvus.Json.JsonSchema.TypeGeneratorTool`
   - Command: `generatejsonschematypes -s opencli-schema.json -o OpenCli.Models -n OpenCli.Models`

### Example Generated TypeScript Interface

```typescript
export interface OpenCLISpec {
  opencli: string;
  info: CliInfo;
  conventions?: Conventions;
  arguments?: Argument[];
  options?: Option[];
  commands?: Command[];
  exitCodes?: ExitCode[];
  examples?: Example[];
  metadata?: Record<string, any>;
}

export interface Command {
  name: string;
  description?: string;
  aliases?: string[];
  arguments?: Argument[];
  options?: Option[];
  commands?: Command[];
  exitCodes?: ExitCode[];
  examples?: Example[];
  metadata?: Record<string, any>;
}
```

### Validation Usage

```javascript
// Using ajv for JSON Schema validation
import Ajv from 'ajv';
import openCLISchema from './opencli-schema.json';

const ajv = new Ajv();
const validate = ajv.compile(openCLISchema);

function validateCLISpec(spec) {
  const valid = validate(spec);
  if (!valid) {
    console.error(validate.errors);
    return false;
  }
  return true;
}
```

## Technical Notes

### Schema Validation
- Uses JSON Schema Draft 7 for validation
- Supports custom metadata through `unevaluatedProperties`
- Enforces type safety for all fields
- Allows flexible extension points

### Compatibility
- Designed to work with any CLI tool
- Language and platform agnostic
- Supports modern CLI patterns
- Backward compatible design philosophy

### Schema Design Decisions
- Uses `$ref` for reusable definitions
- Allows metadata at multiple levels for extensibility
- Required fields kept minimal for flexibility
- Supports recursive command structures

## Conclusion

OpenCLI represents a significant step toward standardizing CLI tool documentation and enabling better tooling around command-line interfaces. As the specification matures, it has the potential to transform how we interact with and document CLI applications, similar to how OpenAPI transformed REST API documentation.