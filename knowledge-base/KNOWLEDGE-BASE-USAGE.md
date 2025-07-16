# Knowledge Base Usage Instructions

## Quick Start

This folder contains a comprehensive collection of technical guidelines and specifications that serve as a knowledge base for .NET development, testing, code generation, and OpenCLI to MCP conversion. Here's how to effectively use these resources with Claude or any AI assistant.

## Effective Prompting Strategies

### 1. Reference Specific Documents

Instead of asking general questions, reference the specific documentation files:

❌ **Less Effective:**
```
"How do I write tests in .NET?"
```

✅ **More Effective:**
```
"Using the dotnet-testing-frameworks-guidelines.md as a guide, help me create tests for my data processing methods"
```

### 2. Combine Multiple Guidelines

Many tasks benefit from multiple documents:

✅ **Example:**
```
"Following the patterns in dotnet-testing-frameworks-guidelines.md and the TDD principles in dotnet-test-driven-development-guidelines.md, help me implement tests for a new payment processing feature"
```

### 3. Request Guideline-Based Plans

Before implementation, ask for plans based on guidelines:

✅ **Example:**
```
"Based on the opencli-to-mcp-conversion-guide.md, create a plan for converting my existing OpenCLI tool to use MCP"
```

### 4. Leverage Multiple Documents

Use multiple documents for comprehensive understanding:

✅ **Example:**
```
"Using dotnet-coding-style-guidelines.md and dotnet-collections-performance-guidelines.md, help me refactor this code to use modern C# patterns and optimal collection types"
```

## Common Use Cases and Document Mapping

### Setting Up a New Project

**Prompt Template:**
```
"Using the following guidelines:
- CLAUDE.md for development principles
- dotnet-testing-frameworks-guidelines.md for test setup
- dotnet-test-driven-development-guidelines.md for TDD practices
- dotnet-coding-style-guidelines.md for modern C# patterns

Help me set up a new .NET project with proper test structure and modern coding patterns"
```

### Building CLI Applications

**Prompt Template:**
```
"Using these specifications:
- opencli-specification-documentation.md for CLI architecture
- opencli-to-mcp-conversion-guide.md for MCP integration

Design and implement a CLI tool for [your use case]"
```

### Code Generation Tasks

**Prompt Template:**
```
"Based on dotnet-source-generators-guidelines.md, create a source generator that:
- [Describe what code should be generated]
- [Specify the trigger conditions]
- [Define the output format]"
```

### Code Quality and Refactoring

**Prompt Template:**
```
"Using:
- dotnet-coding-style-guidelines.md for modern C# patterns
- dotnet-coding-patterns-to-avoid-guidelines.md to identify anti-patterns
- dotnet-collections-performance-guidelines.md for collection optimization

Review and refactor my code to follow best practices and avoid common pitfalls"
```

## Advanced Usage Patterns

### 1. Cross-Document Integration

Many real-world scenarios require combining multiple guidelines:

```
"I need to build a microservice that:
- Has comprehensive tests (see dotnet-testing-frameworks-guidelines.md)
- Follows TDD practices (see dotnet-test-driven-development-guidelines.md)
- Uses modern C# patterns (see dotnet-coding-style-guidelines.md)

Please create an implementation plan that integrates all these requirements"
```

### 2. Guideline-Driven Code Reviews

```
"Review this code against:
- The testing patterns in dotnet-testing-frameworks-guidelines.md
- The TDD principles in dotnet-test-driven-development-guidelines.md
- The anti-patterns in dotnet-coding-patterns-to-avoid-guidelines.md
- The modern C# idioms in dotnet-coding-style-guidelines.md"
```

### 3. Migration and Conversion

```
"Using opencli-to-mcp-conversion-guide.md and opencli-to-mcp-specification.md, help me:
1. Analyze my current OpenCLI implementation
2. Create a migration plan to MCP
3. Implement the conversion with minimal disruption"
```

## Document Quick Reference

| Task | Primary Document | Supporting Documents |
|------|----------------------|--------------------------|
| Writing Tests | `dotnet-testing-frameworks-guidelines.md` | `dotnet-test-driven-development-guidelines.md`, `dotnet-testing-guidelines.md`, `CLAUDE.md` |
| Code Quality | `dotnet-coding-style-guidelines.md` | `dotnet-coding-patterns-to-avoid-guidelines.md`, `dotnet-collections-performance-guidelines.md` |
| CLI Development | `opencli-specification-documentation.md` | `opencli-to-mcp-conversion-guide.md`, `opencli-to-mcp-specification.md` |
| Code Generation | `dotnet-source-generators-guidelines.md` | - |
| MCP Integration | `modelcontextprotocol-aspnetcore-guidelines.md` | `opencli-to-mcp-conversion-guide.md` |

## Best Practices

### 1. Be Specific About Context
Always mention:
- Which document(s) to reference
- What specific section or pattern to follow
- Any constraints or requirements

### 2. Request Verification Against Guidelines
After implementation, ask:
```
"Verify this implementation against the patterns and best practices in [document.md]"
```

### 3. Use Documents for Learning
```
"Explain the key concepts from dotnet-source-generators-guidelines.md with simple examples"
```

### 4. Combine with Project Guidelines
Always consider `CLAUDE.md` alongside other documents:
```
"Implement this feature following:
- The TDD approach from CLAUDE.md
- The patterns from [relevant-document.md]"
```

## Example Workflow

### Step 1: Plan with Guidelines
```
"Using dotnet-testing-frameworks-guidelines.md and CLAUDE.md, create a TDD plan for implementing a shopping cart feature"
```

### Step 2: Implement with Guidance
```
"Following the plan and the patterns in the guidelines, implement the first failing test"
```

### Step 3: Verify Against Standards
```
"Check if this implementation follows the best practices outlined in the guidelines"
```

### Step 4: Refactor with Quality Guidelines
```
"Using dotnet-coding-style-guidelines.md and dotnet-coding-patterns-to-avoid-guidelines.md, refactor the implementation for better quality"
```

## Tips for Maximum Effectiveness

1. **Read the Index First**: Start with `SPECIFICATIONS-INDEX.md` to understand what's available
2. **Use Multiple Documents**: Most real-world tasks benefit from multiple guides
3. **Follow the Patterns**: The documents contain battle-tested patterns - use them
4. **Ask for Examples**: Request examples that follow guideline patterns
5. **Iterate with Feedback**: Use guidelines to validate and improve implementations

## Common Pitfalls to Avoid

1. **Don't ignore CLAUDE.md** - It contains project-specific requirements that override general practices
2. **Don't cherry-pick** - Guidelines are comprehensive; follow them fully
3. **Don't skip the planning phase** - Use guidelines to plan before implementing
4. **Don't forget cross-references** - Many guidelines reference each other

## Understanding Document Types

### Guidelines (my-research/*.md)
- Provide comprehensive guidance on specific topics
- Contain best practices and patterns to follow
- Include anti-patterns and things to avoid
- Offer performance optimization strategies

### Specifications (my-specifications/*.md)
- Define implementation standards and architectures
- Provide concrete patterns and examples
- Establish technical requirements
- Serve as reference documentation

### Usage Together
Guidelines inform best practices while specifications define implementation details. Use both for comprehensive understanding:

```
"Using both dotnet-testing-frameworks-guidelines.md (for best practices) and opencli-specification-documentation.md (for implementation), help me design a testable CLI tool"
```

## Getting Help

If you're unsure which document to use:
```
"Which guidelines should I reference for [describe your task]?"
```

For understanding document relationships:
```
"How do [document1.md] and [document2.md] work together?"
```

Remember: These documents represent accumulated best practices and proven patterns. Using them effectively will lead to higher quality, more maintainable code.