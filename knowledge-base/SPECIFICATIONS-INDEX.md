# Knowledge Base Index

## Overview

This repository contains comprehensive technical guidelines and specifications for .NET development, testing, code generation, and OpenCLI to MCP conversion. Each document provides in-depth guidance, best practices, and implementation examples.

## How to Use This Knowledge Base

### For Planning and Implementation
1. Reference specific documents by filename when asking for implementation help
2. Ask me to consult relevant guidelines before creating implementation plans
3. Use documents as authoritative guides for best practices and patterns

### Example Prompts
- "Using the `dotnet-testing-frameworks-guidelines.md`, help me set up a test project"
- "Following the patterns in `opencli-specification-documentation.md`, implement a CLI tool"
- "Based on `dotnet-source-generators-guidelines.md`, create a source generator for my code"

## Document Index

### Testing & Development Guidelines (my-research/)

1. **dotnet-testing-frameworks-guidelines.md**
   - Comprehensive guide to .NET testing frameworks
   - Microsoft.Testing.Platform, MSTest, NUnit, xUnit
   - TDD patterns, test organization, mocking strategies

2. **dotnet-testing-guidelines.md**
   - Behavior-driven testing principles
   - Schema usage in tests and type safety
   - Achieving 100% coverage through business behavior
   - Shouldly and NSubstitute usage

3. **dotnet-test-driven-development-guidelines.md**
   - TDD examples and anti-patterns
   - Red-Green-Refactor cycle implementation
   - Common TDD violations to avoid

### Code Quality & Standards (my-research/)

4. **dotnet-coding-style-guidelines.md**
   - Modern C# idioms and patterns
   - Code organization and naming conventions
   - Best practices for readable, maintainable code

5. **dotnet-coding-patterns-to-avoid-guidelines.md**
   - Anti-patterns in .NET development
   - Common mistakes and how to avoid them
   - Code review checklist based on best practices

6. **dotnet-collections-performance-guidelines.md**
   - Performance guide for immutable collections
   - FrozenSet/Dictionary vs ImmutableArray/List
   - Collection selection matrix and decision trees

### Code Generation (my-research/)

7. **dotnet-source-generators-guidelines.md**
   - .NET Source Generators comprehensive guide
   - IIncrementalGenerator patterns (ISourceGenerator is deprecated)
   - Compile-time code generation best practices
   - Roslyn analyzer integration

### Protocol & Integration (my-research/)

8. **modelcontextprotocol-aspnetcore-guidelines.md**
   - Model Context Protocol implementation for ASP.NET Core
   - MCP server architecture and API components
   - Transport mechanisms and connection lifecycle
   - Integration patterns and middleware design

### CLI Specifications (my-specifications/)

9. **opencli-specification-documentation.md**
   - OpenCLI framework documentation
   - Platform-agnostic CLI tool interface specification
   - JSON/YAML specification structure
   - Documentation and client interface generation

10. **opencli-to-mcp-conversion-guide.md**
    - Guide for converting OpenCLI to Model Context Protocol
    - Step-by-step migration process
    - Compatibility considerations
    - Best practices for conversion

11. **opencli-to-mcp-specification.md**
    - Technical specification for OpenCLI to MCP conversion
    - API mapping and transformation rules
    - Implementation requirements
    - Architecture patterns

## Quick Reference Guide

### By Use Case

**Setting up Testing:**
- Use `dotnet-testing-frameworks-guidelines.md` as primary guide
- Follow TDD principles in `dotnet-test-driven-development-guidelines.md`
- Apply behavior-driven testing from `dotnet-testing-guidelines.md`
- Follow TDD principles outlined in `CLAUDE.md`

**Code Quality Improvement:**
- Follow modern patterns in `dotnet-coding-style-guidelines.md`
- Avoid anti-patterns listed in `dotnet-coding-patterns-to-avoid-guidelines.md`
- Optimize collections using `dotnet-collections-performance-guidelines.md`

**Code Generation:**
- Leverage `dotnet-source-generators-guidelines.md` for compile-time generation
- Use IIncrementalGenerator (not deprecated ISourceGenerator)
- Follow best practices for IDE performance

**CLI Development:**
- Design using `opencli-specification-documentation.md`
- For MCP conversion, see `opencli-to-mcp-conversion-guide.md`
- Review technical details in `opencli-to-mcp-specification.md`

**MCP Integration:**
- Implement using `modelcontextprotocol-aspnetcore-guidelines.md`
- Understand protocol architecture and lifecycle
- Follow ASP.NET Core integration patterns

## Best Practices for Using This Knowledge Base

1. **Always specify which document to reference** when asking for implementation help
2. **Cross-reference related documents** for comprehensive solutions
3. **Follow the patterns and conventions** established in each document
4. **Use documents as living references** - they contain up-to-date best practices

## Integration with Development Workflow

These documents are designed to be used throughout the development lifecycle:

1. **Planning Phase**: Reference specifications for architectural decisions
2. **Implementation Phase**: Follow patterns and examples from guidelines
3. **Testing Phase**: Use testing guidelines for comprehensive coverage
4. **Code Review Phase**: Apply quality guidelines and anti-patterns checklist
5. **Documentation Phase**: Generate docs following specification patterns

## Note on CLAUDE.md

The `CLAUDE.md` file contains project-specific development guidelines that should be followed in conjunction with these documents. It emphasizes:
- Test-Driven Development (TDD) as a non-negotiable practice
- Functional programming patterns
- Immutable data structures
- Behavior-driven testing approaches

Always consult `CLAUDE.md` for project-specific requirements and combine with relevant guidelines for comprehensive guidance.