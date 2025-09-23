# Version Management Guide

This document explains how version management works for the Unity EventBus package using **Conventional Commits** and **Semantic Versioning**.

## Overview

We use **semantic versioning** (SemVer) with the format `MAJOR.MINOR.PATCH`:
- **MAJOR**: Breaking changes (e.g., 1.0.0 → 2.0.0)
- **MINOR**: New features (backward compatible) (e.g., 1.0.0 → 1.1.0)
- **PATCH**: Bug fixes (backward compatible) (e.g., 1.0.0 → 1.0.1)

## How It Works

Version bumps are **automatically determined** by your commit messages using **Conventional Commits**:

```bash
feat: add new dispatcher type     # → MINOR bump (1.0.0 → 1.1.0)
fix: resolve memory leak          # → PATCH bump (1.0.0 → 1.0.1)
feat!: breaking change            # → MAJOR bump (1.0.0 → 2.0.0)
```

## Conventional Commits Format

Use this format for all commit messages:

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Commit Types

| Type | Description | Version Bump |
|------|-------------|--------------|
| `feat` | New feature | **MINOR** |
| `fix` | Bug fix | **PATCH** |
| `perf` | Performance improvement | **PATCH** |
| `refactor` | Code refactoring | **PATCH** |
| `docs` | Documentation changes | **PATCH** |
| `style` | Code style changes | **PATCH** |
| `test` | Test changes | **PATCH** |
| `build` | Build system changes | **PATCH** |
| `ci` | CI/CD changes | **PATCH** |
| `chore` | Maintenance tasks | **PATCH** |

### Breaking Changes

Add `!` after the type for breaking changes:

```bash
feat!: remove deprecated API     # → MAJOR bump (1.0.0 → 2.0.0)
```

Or use the footer:

```bash
feat: add new API

BREAKING CHANGE: The old API is no longer supported
```

## Release Workflow

### Automatic Version Bumping

1. **Make changes** on feature branch
2. **Use conventional commits** in your commit messages
3. **Merge to main** branch
4. **Semantic Release** automatically:
   - Analyzes commits since last release
   - Determines version bump type
   - Updates `package.json` version
   - Generates `CHANGELOG.md`
   - **No release created** (version only)

### Manual Release Creation

When you're ready to release:

1. **Go to GitHub Actions** → "Manual Release" workflow
2. **Click "Run workflow"**
3. **Enter version number** (e.g., `1.1.0`)
4. **Add release notes** (optional)
5. **Click "Run workflow"**

The workflow will:
- Validate version format
- Check if version already exists
- Update `package.json` to exact version
- Create Git tag
- Create GitHub release

### Example Workflow

```bash
# Work on feature
git checkout -b feature/new-dispatcher
git add .
git commit -m "feat: add thread pool dispatcher"
git push origin feature/new-dispatcher

# Merge to main
git checkout main
git merge feature/new-dispatcher
git push origin main

# Semantic Release automatically:
# - Detects "feat:" commit
# - Bumps minor version (1.0.0 → 1.1.0)
# - Updates package.json and CHANGELOG.md
# - NO release created yet

# When ready to release:
# 1. Go to GitHub Actions → "Manual Release"
# 2. Enter version: 1.1.0
# 3. Add release notes
# 4. Run workflow → Creates official release
```


## Examples

### Good Commit Messages

```bash
# New features (MINOR bump)
feat: add thread pool dispatcher
feat(dispatcher): add async event processing
feat!: remove deprecated EventBus constructor

# Bug fixes (PATCH bump)
fix: resolve memory leak in event queue
fix(dispatcher): handle null event handlers
fix: prevent infinite loop in subscription cleanup

# Documentation (PATCH bump)
docs: add usage examples to README
docs(api): document EventBus methods

# Maintenance (PATCH bump)
chore: update Unity dependencies
chore: add GitHub Actions workflow
refactor: simplify event queue implementation
```

### Bad Commit Messages

```bash
# ❌ Too vague
"update stuff"
"fix bug"
"changes"

# ❌ No conventional format
"Added new dispatcher"
"Fixed memory leak"
"Updated documentation"
```

## Best Practices

1. **Always use conventional commits** - This enables automatic versioning
2. **Be descriptive** - Explain what and why, not just what
3. **Use present tense** - "add feature" not "added feature"
4. **Keep first line under 50 characters** - Use body for details
5. **Use scope when helpful** - `feat(dispatcher): add async support`

## Unity Package Manager Integration

Users can install the package using:

```
https://github.com/dvos-tools/eventbus.git?path=/&version=1.0.0
```

Or add to `manifest.json`:
```json
{
  "dependencies": {
    "com.dvos-tools.bus": "https://github.com/dvos-tools/eventbus.git?path=/&version=1.0.0"
  }
}
```

## Troubleshooting

### No Release Created
If semantic-release doesn't create a release:
1. Check if commits follow conventional format
2. Verify commits are on `main` branch
3. Check GitHub Actions logs for errors

### Wrong Version Bump
If the wrong version bump occurs:
1. Check commit message format
2. Use `feat!:` for breaking changes
3. Use `BREAKING CHANGE:` footer for complex breaking changes

### Manual Override
If you need to manually set a version:
1. Create a commit with `chore(release): 1.2.0`
2. This will trigger semantic-release with the specified version