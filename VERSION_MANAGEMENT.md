# Version Management Guide

This document explains how version management works for the Unity EventBus package.

## Overview

We use **semantic versioning** (SemVer) with the format `MAJOR.MINOR.PATCH`:
- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

## Version Sources

The package version is managed in these places:
1. **`package.json`** - Primary source of truth for Unity Package Manager
2. **Git tags** - Release markers (e.g., `v1.0.0`)
3. **`CHANGELOG.md`** - Release notes and history
4. **GitHub Releases** - Official release documentation

## Release Workflows

### Option 1: Automated (Recommended)
Use GitHub Actions for automated releases:

```bash
# Create a release tag
git tag v1.1.0
git push origin v1.1.0
```

The GitHub Action will:
- Update `package.json` version
- Generate changelog from conventional commits
- Create GitHub release
- Push changes

### Option 2: Manual with Scripts
Use npm scripts for manual version management:

```bash
# Patch version (1.0.0 → 1.0.1)
npm run version:patch

# Minor version (1.0.0 → 1.1.0)  
npm run version:minor

# Major version (1.0.0 → 2.0.0)
npm run version:major
```

### Option 3: Manual with Custom Script
Use the custom version script:

```bash
# Update to specific version
./update-version.sh 1.1.0

# Create release
./update-version.sh 1.1.0 --release
```

## Conventional Commits

Use conventional commit messages for automatic changelog generation:

```
feat: add new dispatcher type
fix: resolve memory leak in event queue
docs: update README with examples
chore: update dependencies
```

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

## Best Practices

1. **Always use semantic versioning**
2. **Update CHANGELOG.md for each release**
3. **Create GitHub releases for major/minor versions**
4. **Use conventional commits for automatic changelog**
5. **Test the package import after each release**

## Troubleshooting

### Version Mismatch
If versions get out of sync:
1. Check `package.json` version
2. Verify git tags: `git tag -l`
3. Update manually: `./update-version.sh <version>`

### Missing Release
If a release wasn't created:
1. Check GitHub Actions logs
2. Manually create release: `./update-version.sh <version> --release`
3. Push tags: `git push --tags`