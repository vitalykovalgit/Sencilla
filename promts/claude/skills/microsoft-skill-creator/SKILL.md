# Microsoft Skill Creator

Create hybrid skills for Microsoft technologies that store essential knowledge locally while enabling dynamic Learn MCP lookups for deeper details.

## About Skills

Skills are modular packages that extend agent capabilities with specialized knowledge and workflows. A skill transforms a general-purpose agent into a specialized one for a specific domain.

### Skill Structure

```
skill-name/
├── SKILL.md (required)     # Frontmatter (name, description) + instructions
├── references/             # Documentation loaded into context as needed
├── sample_codes/           # Working code examples
└── assets/                 # Files used in output (templates, etc.)
```

### Key Principles

- **Frontmatter is critical**: `name` and `description` determine when the skill triggers—be clear and comprehensive
- **Concise is key**: Only include what agents don't already know; context window is shared
- **No duplication**: Information lives in SKILL.md OR reference files, not both

## Learn MCP Tools

| Tool | Purpose | When to Use |
|------|---------|-------------|
| `microsoft_docs_search` | Search official docs | First pass discovery, finding topics |
| `microsoft_docs_fetch` | Get full page content | Deep dive into important pages |
| `microsoft_code_sample_search` | Find code examples | Get implementation patterns |

### CLI Alternative

If the Learn MCP server is not available, use the `mslearn` CLI from a terminal or shell (for example, Bash, PowerShell, or cmd) instead:

```bash
# Run directly (no install needed)
npx @microsoft/learn-cli search "semantic kernel overview"

# Or install globally, then run
npm install -g @microsoft/learn-cli
mslearn search "semantic kernel overview"
```

| MCP Tool | CLI Command |
|----------|-------------|
| `microsoft_docs_search(query: "...")` | `mslearn search "..."` |
| `microsoft_code_sample_search(query: "...", language: "...")` | `mslearn code-search "..." --language ...` |
| `microsoft_docs_fetch(url: "...")` | `mslearn fetch "..."` |

Generated skills should include this same CLI fallback table so agents can use either path.

## Creation Process

### Step 1: Investigate the Topic

Build deep understanding using Learn MCP tools in three phases:

**Phase 1 - Scope Discovery:**
```
microsoft_docs_search(query="{technology} overview what is")
microsoft_docs_search(query="{technology} concepts architecture")
microsoft_docs_search(query="{technology} getting started tutorial")
```

**Phase 2 - Core Content:**
```
microsoft_docs_fetch(url="...")  # Fetch pages from Phase 1
microsoft_code_sample_search(query="{technology}", language="{lang}")
```

**Phase 3 - Depth:**
```
microsoft_docs_search(query="{technology} best practices")
microsoft_docs_search(query="{technology} troubleshooting errors")
```

#### Investigation Checklist

After investigating, verify:
- [ ] Can explain what the technology does in one paragraph
- [ ] Identified 3-5 key concepts
- [ ] Have working code for basic usage
- [ ] Know the most common API patterns
- [ ] Have search queries for deeper topics

### Step 2: Clarify with User

Present findings and ask:
1. "I found these key areas: [list]. Which are most important?"
2. "What tasks will agents primarily perform with this skill?"
3. "Which programming language should code samples prioritize?"

### Step 3: Generate the Skill

Use the appropriate template from [skill-templates.md](references/skill-templates.md):

| Technology Type | Template |
|-----------------|----------|
| Client library, NuGet/npm package | SDK/Library |
| Azure resource | Azure Service |
| App development framework | Framework/Platform |
| REST API, protocol | API/Protocol |

#### Generated Skill Structure

```
{skill-name}/
├── SKILL.md                    # Core knowledge + Learn MCP guidance
├── references/                 # Detailed local documentation (if needed)
└── sample_codes/               # Working code examples
    ├── getting-started/
    └── common-patterns/
```

### Step 4: Balance Local vs Dynamic Content

**Store locally when:**
- Foundational (needed for any task)
- Frequently accessed
- Stable (won't change)
- Hard to find via search

**Keep dynamic when:**
- Exhaustive reference (too large)
- Version-specific
- Situational (specific tasks only)
- Well-indexed (easy to search)

#### Content Guidelines

| Content Type | Local | Dynamic |
|--------------|-------|---------|
| Core concepts (3-5) | ✅ Full | |
| Hello world code | ✅ Full | |
| Common patterns (3-5) | ✅ Full | |
| Top API methods | Signature + example | Full docs via fetch |
| Best practices | Top 5 bullets | Search for more |
| Troubleshooting | | Search queries |
| Full API reference | | Doc links |

### Step 5: Validate

1. Review: Is local content sufficient for common tasks?
2. Test: Do suggested search queries return useful results?
3. Verify: Do code samples run without errors?

## Common Investigation Patterns

### For SDKs/Libraries
```
"{name} overview" → purpose, architecture
"{name} getting started quickstart" → setup steps
"{name} API reference" → core classes/methods
"{name} samples examples" → code patterns
"{name} best practices performance" → optimization
```

### For Azure Services
```
"{service} overview features" → capabilities
"{service} quickstart {language}" → setup code
"{service} REST API reference" → endpoints
"{service} SDK {language}" → client library
"{service} pricing limits quotas" → constraints
```

### For Frameworks/Platforms
```
"{framework} architecture concepts" → mental model
"{framework} project structure" → conventions
"{framework} tutorial walkthrough" → end-to-end flow
"{framework} configuration options" → customization
```

## Example: Creating a "Semantic Kernel" Skill

### Investigation

```
microsoft_docs_search(query="semantic kernel overview")
microsoft_docs_search(query="semantic kernel plugins functions")
microsoft_code_sample_search(query="semantic kernel", language="csharp")
microsoft_docs_fetch(url="https://learn.microsoft.com/semantic-kernel/overview/")
```

### Generated Skill

```
semantic-kernel/
├── SKILL.md
└── sample_codes/
    ├── getting-started/
    │   └── hello-kernel.cs
    └── common-patterns/
        ├── chat-completion.cs
        └── function-calling.cs
```

### Generated SKILL.md

```markdown
---
name: semantic-kernel
description: Build AI agents with Microsoft Semantic Kernel. Use for LLM-powered apps with plugins, planners, and memory in .NET or Python.
---

# Semantic Kernel

Orchestration SDK for integrating LLMs into applications with plugins, planners, and memory.

## Key Concepts

- **Kernel**: Central orchestrator managing AI services and plugins
- **Plugins**: Collections of functions the AI can call
- **Planner**: Sequences plugin functions to achieve goals
- **Memory**: Vector store integration for RAG patterns

## Quick Start

See [getting-started/hello-kernel.cs](sample_codes/getting-started/hello-kernel.cs)

## Learn More

| Topic | How to Find |
|-------|-------------|
| Plugin development | `microsoft_docs_search(query="semantic kernel plugins custom functions")` |
| Planners | `microsoft_docs_search(query="semantic kernel planner")` |
| Memory | `microsoft_docs_fetch(url="https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/agent-memory")` |

## CLI Alternative

If the Learn MCP server is not available, use the `mslearn` CLI instead:

| MCP Tool | CLI Command |
|----------|-------------|
| `microsoft_docs_search(query: "...")` | `mslearn search "..."` |
| `microsoft_code_sample_search(query: "...", language: "...")` | `mslearn code-search "..." --language ...` |
| `microsoft_docs_fetch(url: "...")` | `mslearn fetch "..."` |

Run directly with `npx @microsoft/learn-cli <command>` or install globally with `npm install -g @microsoft/learn-cli`.
```

---

## Reference: skill-templates

# Skill Templates

Ready-to-use templates for different types of Microsoft technologies.

## CLI Alternative for MCP Tools

All templates below use MCP tool calls (e.g., `microsoft_docs_search`, `microsoft_docs_fetch`, `microsoft_code_sample_search`). If the Learn MCP server is not available, replace them with CLI equivalents:

| MCP Tool | CLI Command |
|----------|-------------|
| `microsoft_docs_search(query: "...")` | `mslearn search "..."` |
| `microsoft_code_sample_search(query: "...", language: "...")` | `mslearn code-search "..." --language ...` |
| `microsoft_docs_fetch(url: "...")` | `mslearn fetch "..."` |

Run directly with `npx @microsoft/learn-cli <command>` or install globally with `npm install -g @microsoft/learn-cli`.

## Template 1: SDK/Library Skill

For client libraries, SDKs, and programming frameworks.

```markdown
---
name: {sdk-name}
description: {What it does}. Use when agents need to {primary task} with {technology context}. Supports {languages/platforms}.
---

# {SDK Name}

{One paragraph: what it is, why it exists, when to use it}

## Installation

{Package manager commands for supported languages}

## Key Concepts

{3-5 essential concepts, one paragraph each max}

### {Concept 1}
{Brief explanation}

### {Concept 2}
{Brief explanation}

## Quick Start

{Minimal working example - inline if <30 lines, otherwise reference sample_codes/}

## Common Patterns

### {Pattern 1: e.g., "Basic CRUD"}
```{language}
{code}
```

### {Pattern 2: e.g., "Error Handling"}
```{language}
{code}
```

## API Quick Reference

| Class/Method | Purpose | Example |
|--------------|---------|---------|
| {name} | {what it does} | `{usage}` |

For full API documentation:
- `microsoft_docs_search(query="{sdk} {class} API reference")`
- `microsoft_docs_fetch(url="{url}")`

## Best Practices

- **Do**: {recommendation}
- **Do**: {recommendation}
- **Avoid**: {anti-pattern}

See [best-practices.md](references/best-practices.md) for detailed guidance.

## Learn More

| Topic | How to Find |
|-------|-------------|
| {Advanced topic 1} | `microsoft_docs_search(query="{sdk} {topic}")` |
| {Advanced topic 2} | `microsoft_docs_fetch(url="{url}")` |
| {Code examples} | `microsoft_code_sample_search(query="{sdk} {scenario}", language="{lang}")` |
```

---

## Template 2: Azure Service Skill

For Azure services and cloud resources.

```markdown
---
name: {service-name}
description: Work with {Azure Service}. Use when agents need to {primary capabilities}. Covers provisioning, configuration, and SDK usage.
---

# {Azure Service Name}

{One paragraph: what the service does, primary use cases}

## Overview

- **Category**: {Compute/Storage/AI/Networking/etc.}
- **Key capability**: {main value proposition}
- **When to use**: {scenarios}

## Getting Started

### Prerequisites
- Azure subscription
- {Other requirements}

### Provisioning
{CLI/Portal/Bicep snippet for creating the resource}

## SDK Usage ({Language})

### Installation
```
{package install command}
```

### Authentication
```{language}
{auth code pattern}
```

### Basic Operations
```{language}
{CRUD or primary operations}
```

## Key Configurations

| Setting | Purpose | Default |
|---------|---------|---------|
| {setting} | {what it controls} | {value} |

## Pricing & Limits

- **Pricing model**: {consumption/tier-based/etc.}
- **Key limits**: {important quotas}

For current pricing: `microsoft_docs_search(query="{service} pricing")`

## Common Patterns

### {Pattern 1}
{Code or configuration}

### {Pattern 2}
{Code or configuration}

## Troubleshooting

| Issue | Solution |
|-------|----------|
| {Common error} | {Fix} |

For more issues: `microsoft_docs_search(query="{service} troubleshoot {symptom}")`

## Learn More

| Topic | How to Find |
|-------|-------------|
| REST API | `microsoft_docs_fetch(url="{url}")` |
| ARM/Bicep | `microsoft_docs_search(query="{service} bicep template")` |
| Security | `microsoft_docs_search(query="{service} security best practices")` |
```

---

## Template 3: Framework/Platform Skill

For development frameworks and platforms (e.g., ASP.NET, MAUI, Blazor).

```markdown
---
name: {framework-name}
description: Build {type of apps} with {Framework}. Use when agents need to create, modify, or debug {framework} applications.
---

# {Framework Name}

{One paragraph: what it is, what you build with it, why choose it}

## Project Structure

```
{typical-project}/
├── {folder}/     # {purpose}
├── {file}        # {purpose}
└── {file}        # {purpose}
```

## Getting Started

### Create New Project
```bash
{CLI command to scaffold}
```

### Project Configuration
{Key files to configure and what they control}

## Core Concepts

### {Concept 1: e.g., "Components"}
{Explanation with minimal code example}

### {Concept 2: e.g., "Routing"}
{Explanation with minimal code example}

### {Concept 3: e.g., "State Management"}
{Explanation with minimal code example}

## Common Patterns

### {Pattern 1}
```{language}
{code}
```

### {Pattern 2}
```{language}
{code}
```

## Configuration Options

| Setting | File | Purpose |
|---------|------|---------|
| {setting} | {file} | {what it does} |

## Deployment

{Brief deployment guidance or reference}

For detailed deployment: `microsoft_docs_search(query="{framework} deploy {target}")`

## Learn More

| Topic | How to Find |
|-------|-------------|
| {Advanced feature} | `microsoft_docs_search(query="{framework} {feature}")` |
| {Integration} | `microsoft_docs_fetch(url="{url}")` |
| {Samples} | `microsoft_code_sample_search(query="{framework} {scenario}")` |
```

---

## Template 4: API/Protocol Skill

For APIs, protocols, and specifications (e.g., Microsoft Graph, OOXML).

```markdown
---
name: {api-name}
description: Interact with {API/Protocol}. Use when agents need to {primary operations}. Covers authentication, endpoints, and common operations.
---

# {API/Protocol Name}

{One paragraph: what it provides access to, primary use cases}

## Authentication

{Auth method and code pattern}

## Base Configuration

- **Base URL**: `{url}`
- **Version**: `{version}`
- **Format**: {JSON/XML/etc.}

## Common Endpoints/Operations

### {Operation 1: e.g., "List Items"}
```
{HTTP method} {endpoint}
```
```{language}
{SDK code}
```

### {Operation 2: e.g., "Create Item"}
```
{HTTP method} {endpoint}
```
```{language}
{SDK code}
```

## Request/Response Patterns

### Pagination
{How to handle pagination}

### Error Handling
{Error format and common codes}

## Quick Reference

| Operation | Endpoint/Method | Notes |
|-----------|-----------------|-------|
| {op} | `{endpoint}` | {note} |

## Permissions/Scopes

| Operation | Required Permission |
|-----------|---------------------|
| {op} | `{permission}` |

## Learn More

| Topic | How to Find |
|-------|-------------|
| Full endpoint reference | `microsoft_docs_fetch(url="{url}")` |
| Permissions | `microsoft_docs_search(query="{api} permissions {resource}")` |
| SDKs | `microsoft_docs_search(query="{api} SDK {language}")` |
```

---

## Choosing a Template

| Technology Type | Template | Examples |
|-----------------|----------|----------|
| Client library, NuGet/npm package | SDK/Library | Semantic Kernel, Azure SDK, MSAL |
| Azure resource | Azure Service | Cosmos DB, Azure Functions, App Service |
| App development framework | Framework/Platform | ASP.NET Core, Blazor, MAUI |
| REST API, protocol, specification | API/Protocol | Microsoft Graph, OOXML, FHIR |

## Customization Guidelines

Templates are starting points. Customize by:

1. **Adding sections** for unique aspects of the technology
2. **Removing sections** that don't apply
3. **Adjusting depth** based on complexity (more concepts for complex tech)
4. **Adding reference files** for detailed content that doesn't fit in SKILL.md
5. **Adding sample_codes/** for working examples beyond inline snippets
