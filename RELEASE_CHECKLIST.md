# Kommand Release Checklist

This document outlines the step-by-step process for releasing a new version of Kommand to NuGet.

## Pre-Release Checklist

### 1. Verify Project State

- [ ] All planned features for this release are complete
- [ ] All open issues for this milestone are resolved
- [ ] All PRs for this milestone are merged
- [ ] No known critical bugs
- [ ] Branch is up to date with `main`

### 2. Run Quality Checks

#### Build Verification

```bash
# Clean build
dotnet clean
dotnet build -c Release
```

**Expected:** Build succeeds with no warnings or errors.

#### Test Suite

```bash
# Run all tests
dotnet test -c Release --verbosity normal
```

**Expected:** All tests pass (110/110 tests).

#### Code Coverage

```bash
# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportsFormat=opencover

# Check coverage report
# Look for overall line coverage percentage
```

**Expected:** Coverage >80% (current: 89.4%).

#### Benchmarks

```bash
# Run microbenchmarks
cd tests/Kommand.Benchmarks
dotnet run -c Release

# Run realistic workload benchmarks
dotnet run -c Release -- --realistic
```

**Expected:** Performance meets targets:
- Mediator dispatch: <2 μs (current: ~685 ns)
- Per-interceptor: <100 ns (current: ~74 ns)
- Total with 3 interceptors: <3 μs (current: ~915 ns)

#### Sample Project

```bash
cd samples/Kommand.Sample
dotnet run
```

**Expected:** Sample runs without errors, all features demonstrated.

### 3. Update Documentation

#### CHANGELOG.md

Update `CHANGELOG.md` with release information:

```markdown
## [1.0.0] - 2025-XX-XX

### Added
- Initial public release
- Core CQRS mediator with ICommand and IQuery
- Interceptor pipeline system
- Built-in OpenTelemetry support
- Custom validation system
- Notification (pub/sub) support
- Comprehensive documentation and samples

### Performance
- Sub-microsecond dispatch overhead (685ns)
- Minimal per-interceptor cost (74ns)
- <0.1% overhead for realistic workloads
```

Replace `XX-XX` with actual release date.

#### Version Number

Update version in `src/Kommand/Kommand.csproj`:

**For pre-release (alpha/beta):**
```xml
<Version>1.0.0-alpha.1</Version>
```

**For stable release:**
```xml
<Version>1.0.0</Version>
```

**Version Guidelines:**
- **Alpha** (`1.0.0-alpha.1`): Early testing, may have bugs
- **Beta** (`1.0.0-beta.1`): Feature complete, testing phase
- **RC** (`1.0.0-rc.1`): Release candidate, final testing
- **Stable** (`1.0.0`): Production-ready

#### README.md

- [ ] Verify all links work
- [ ] Verify code examples are accurate
- [ ] Verify badges will work (build, NuGet, coverage)
- [ ] Update version numbers in examples if needed

### 4. Create Package Locally

```bash
# Create package
cd src/Kommand
dotnet pack -c Release

# Verify package contents
cd bin/Release
unzip -l Kommand.*.nupkg
```

**Verify package includes:**
- [ ] `lib/net8.0/Kommand.dll`
- [ ] `lib/net8.0/Kommand.xml` (XML documentation)
- [ ] `README.md`
- [ ] `Kommand.nuspec` with correct metadata

**Verify .snupkg (symbols package) created:**
- [ ] `Kommand.*.snupkg` exists

## Release Process

### 5. Commit and Push Changes

```bash
# Stage all changes
git add .

# Commit with release message
git commit -m "chore: Prepare release v1.0.0

- Update CHANGELOG.md with release date
- Set version to 1.0.0
- Verify all documentation
"

# Push to main
git push origin main
```

**Wait for CI to pass** before proceeding!

Check: https://github.com/Atherio-Ltd/Kommand/actions

### 6. Create Git Tag

**For pre-release:**
```bash
git tag -a v1.0.0-alpha.1 -m "Release v1.0.0-alpha.1

First alpha release of Kommand.
"

git push origin v1.0.0-alpha.1
```

**For stable release:**
```bash
git tag -a v1.0.0 -m "Release v1.0.0

First stable release of Kommand - production-ready CQRS mediator for .NET 8+.
"

git push origin v1.0.0
```

### 7. Monitor Release Workflow

The tag push will trigger the GitHub Actions release workflow.

**Monitor at:** https://github.com/Atherio-Ltd/Kommand/actions

**The workflow will:**
1. ✅ Checkout code
2. ✅ Setup .NET 10
3. ✅ Restore dependencies
4. ✅ Build in Release configuration
5. ✅ Run all tests
6. ✅ Create NuGet package
7. ✅ Push to NuGet.org (requires `NUGET_API_KEY` secret)
8. ✅ Create GitHub Release

### 8. Verify NuGet.org Publication

**Wait 5-10 minutes** for package to appear on NuGet.org.

- [ ] Visit: https://www.nuget.org/packages/Kommand
- [ ] Verify version is correct
- [ ] Verify package metadata is correct
- [ ] Verify README displays correctly
- [ ] Test installation:
  ```bash
  dotnet new console -n TestKommand
  cd TestKommand
  dotnet add package Kommand
  # Should succeed
  ```

### 9. Verify GitHub Release

- [ ] Visit: https://github.com/Atherio-Ltd/Kommand/releases
- [ ] Verify release is created
- [ ] Verify release notes are generated
- [ ] Verify package files are attached (.nupkg and .snupkg)
- [ ] If pre-release, verify "Pre-release" badge is shown

### 10. Update GitHub Release Notes (Optional)

You can edit the auto-generated release notes to add:

- Summary of major changes
- Breaking changes (if any)
- Upgrade instructions
- Links to documentation
- Thank contributors

**Example:**
```markdown
## Kommand v1.0.0 - First Stable Release 🎉

Kommand is a lightweight, production-ready CQRS mediator for .NET 8+ with built-in OpenTelemetry support.

### Highlights
- ✅ Zero external dependencies (MIT license)
- ✅ Sub-microsecond overhead
- ✅ Built-in validation and OpenTelemetry
- ✅ Comprehensive documentation and samples

### Installation
\```bash
dotnet add package Kommand
\```

### Documentation
- [Getting Started Guide](https://github.com/Atherio-Ltd/Kommand/blob/main/docs/getting-started.md)
- [Sample Project](https://github.com/Atherio-Ltd/Kommand/tree/main/samples/Kommand.Sample)
- [Architecture Document](https://github.com/Atherio-Ltd/Kommand/blob/main/MEDIATOR_ARCHITECTURE_PLAN.md)

### What's Changed
[Auto-generated changelog here]
```

## Post-Release Checklist

### 11. Verify Package Discovery

- [ ] Search for "Kommand" on NuGet.org - package appears
- [ ] Search for "CQRS mediator" on NuGet.org - package appears in results
- [ ] Package shows correct tags (cqrs, mediator, dotnet, etc.)

### 12. Test Package Installation

Create a new test project and verify installation:

```bash
# Create test console app
dotnet new console -n KommandInstallTest
cd KommandInstallTest

# Install Kommand
dotnet add package Kommand

# Verify it works
# Add simple test code from README quick start
dotnet run
```

### 13. Update Project Status

- [ ] Close the milestone on GitHub (if using milestones)
- [ ] Close related issues with message: "Released in v1.0.0"
- [ ] Update project board (if using)
- [ ] Tweet/announce (see Task 6.8 for announcement templates)

### 14. Monitor Initial Feedback

For the first 24-48 hours after release:

- [ ] Monitor GitHub issues for bug reports
- [ ] Monitor GitHub discussions for questions
- [ ] Respond to Reddit/Twitter comments
- [ ] Watch NuGet download stats

## Troubleshooting

### Release Workflow Fails

**If the GitHub Actions release workflow fails:**

1. Check the workflow logs for errors
2. Common issues:
   - **Missing NUGET_API_KEY secret**: Add it in GitHub repo Settings → Secrets and variables → Actions
   - **NuGet push fails**: Check API key permissions and expiration
   - **Tests fail**: Fix tests and push to main before retrying
3. **To retry:** Delete the tag and recreate:
   ```bash
   # Delete local tag
   git tag -d v1.0.0

   # Delete remote tag
   git push origin :refs/tags/v1.0.0

   # Fix issues, commit, push

   # Recreate tag
   git tag -a v1.0.0 -m "Release v1.0.0"
   git push origin v1.0.0
   ```

### Package Not Appearing on NuGet

- **Wait 10-15 minutes** - indexing can be slow
- **Check package status** at https://www.nuget.org/packages/manage/upload
- **Verify API key** has push permissions
- **Check NuGet.org status** at https://status.nuget.org/

### Package Listed but Not Discoverable

- **Wait for indexing** - can take up to 30 minutes
- **Clear NuGet cache** locally:
  ```bash
  dotnet nuget locals all --clear
  ```
- **Search may use old cache** - try private browsing window

## GitHub Secrets Setup

### Adding NUGET_API_KEY Secret

This only needs to be done once:

1. **Create NuGet API Key:**
   - Go to https://www.nuget.org/account/apikeys
   - Click "Create"
   - Name: "Kommand GitHub Actions"
   - Scopes: "Push new packages and package versions"
   - Glob Pattern: "Kommand"
   - Expiration: 365 days (or longer)
   - Copy the API key (you won't see it again!)

2. **Add to GitHub:**
   - Go to https://github.com/Atherio-Ltd/Kommand/settings/secrets/actions
   - Click "New repository secret"
   - Name: `NUGET_API_KEY`
   - Value: [paste your NuGet API key]
   - Click "Add secret"

3. **Verify:**
   - Secret should appear in the list
   - Release workflow will now be able to push to NuGet

## Version Numbering Strategy

Kommand follows **Semantic Versioning 2.0.0** (https://semver.org/):

### Format: MAJOR.MINOR.PATCH[-PRERELEASE]

**MAJOR** (1.0.0 → 2.0.0):
- Breaking API changes
- Removal of deprecated features
- Major architectural changes

**MINOR** (1.0.0 → 1.1.0):
- New features (backwards compatible)
- New public APIs
- Deprecations (but not removals)

**PATCH** (1.0.0 → 1.0.1):
- Bug fixes
- Performance improvements
- Documentation updates
- Internal refactoring

**PRERELEASE**:
- `1.0.0-alpha.1`: Early development, may be unstable
- `1.0.0-beta.1`: Feature complete, testing
- `1.0.0-rc.1`: Release candidate, final testing

### Examples

- `1.0.0-alpha.1` → First alpha
- `1.0.0-alpha.2` → Second alpha (bug fixes)
- `1.0.0-beta.1` → First beta (feature complete)
- `1.0.0-rc.1` → Release candidate
- `1.0.0` → Stable release
- `1.0.1` → Patch (bug fix)
- `1.1.0` → Minor (new feature)
- `2.0.0` → Major (breaking change)

## Next Release

After a successful release, prepare for the next version:

1. **Update version in Kommand.csproj** to next development version:
   ```xml
   <Version>1.1.0-dev</Version>
   ```

2. **Add new section to CHANGELOG.md**:
   ```markdown
   ## [Unreleased]

   ### Added
   ### Changed
   ### Fixed
   ### Removed
   ```

3. **Create new milestone** on GitHub (if using milestones)

4. **Plan next release** - what features/fixes will be included?

---

## Quick Reference

**Pre-release (alpha) workflow:**
```bash
# 1. Update version to 1.0.0-alpha.1 in .csproj
# 2. Update CHANGELOG.md
# 3. Commit and push
git commit -m "chore: Prepare alpha release"
git push

# 4. Create and push tag
git tag -a v1.0.0-alpha.1 -m "Release v1.0.0-alpha.1"
git push origin v1.0.0-alpha.1

# 5. Monitor GitHub Actions
# 6. Verify on NuGet.org
```

**Stable release workflow:**
```bash
# 1. Update version to 1.0.0 in .csproj
# 2. Update CHANGELOG.md with release date
# 3. Commit and push
git commit -m "chore: Release v1.0.0"
git push

# 4. Create and push tag
git tag -a v1.0.0 -m "Release v1.0.0"
git push origin v1.0.0

# 5. Monitor GitHub Actions
# 6. Verify on NuGet.org
# 7. Announce! (see docs/announcement-template.md)
```
