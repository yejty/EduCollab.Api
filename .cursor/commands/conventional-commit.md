---
name: Generate Conventional Commit Message
description: Analyzes staged git changes and generates a conventional commit message
---

Analyze the currently staged git changes (or all changes if nothing is staged) and generate a conventional commit message following the Conventional Commits specification.

## Instructions:

1. First, check the git status and diff of the changes
2. Analyze the changes to understand what was modified
3. Generate a commit message in this format:

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

## Commit Types:
- **feat**: A new feature
- **fix**: A bug fix
- **docs**: Documentation only changes
- **style**: Changes that don't affect code meaning (formatting, missing semi-colons, etc)
- **refactor**: Code change that neither fixes a bug nor adds a feature
- **perf**: Performance improvements
- **test**: Adding or correcting tests
- **chore**: Changes to build process, dependencies, or auxiliary tools
- **ci**: Changes to CI configuration files and scripts
- **build**: Changes affecting the build system or external dependencies
- **revert**: Reverts a previous commit

## Guidelines:
- **Write all descriptions, body, and footers in Slovak language**
- **Use past tense in Slovak** ("pridaná funkcia" not "pridaj funkciu")
- Keep the description concise (max 50-72 characters)
- Don't capitalize the first letter of description
- Don't end description with a period
- If changes span multiple types, choose the most significant or suggest splitting the commit
- Include scope when relevant (e.g., api, core, worker, tests)
- Add a body if the changes need more explanation (in Slovak)
- Add breaking change footer if applicable: `BREAKING CHANGE: popis v slovenčine`

## Output:
Provide the generated commit message in a code block, and explain briefly what changes were detected.

