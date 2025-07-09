# CLAUDE.md

This file provides guidance to AI coding assistants (Claude Code, GitHub Copilot, etc.) when working with code in this repository. It contains project-specific context to help AI tools understand the codebase structure and development workflow.

## Project Overview

Simple Trans is a RimWorld mod that adds a system for differentiating how pawns interact with pregnancy and AGAR (Assigned Gender at Rimworld). The mod was originally created for RimWorld 1.5 and has been decompiled and updated for 1.6 ONLY.

## Development Commands

### Building and Deployment
```bash
# Build and automatically deploy to RimWorld mods folder
dotnet build Source/Simple-Trans.csproj

# Build in Release mode
dotnet build Source/Simple-Trans.csproj --configuration Release
```

**Note**: The project automatically deploys to your RimWorld installation after building!

### Development Workflow
1. Make code changes in `Source/`
2. Run `dotnet build` - this will:
   - Compile the mod
   - Copy the DLL to `1.6/Assemblies/`
   - Deploy the entire mod to `RimWorld/Mods/Simple Trans/`
3. Launch RimWorld to test

## Project Structure

```
Simple Trans/
├── Source/                     # C# source code
│   ├── Simple_Trans/          # Main mod logic
│   │   ├── SimpleTrans.cs     # Entry point and initialization
│   │   ├── SimpleTransPregnancyUtility.cs  # Core pregnancy/gender logic
│   │   └── SimpleTransDebug.cs # Debug tools and logging
│   ├── Simple_Trans.Patches/  # Harmony patches
│   │   ├── NBGPatches.cs      # NonBinary Gender compatibility
│   │   ├── VECore_*.cs        # Vanilla Expanded Framework patches
│   │   └── Pregnancy*.cs      # RimWorld pregnancy system patches
│   └── Properties/            # Assembly metadata
├── 1.6/                       # Updated 1.6 version
│   └── Assemblies/            # Compiled DLLs (auto-generated)
|   ModSupport/Ideology/      # Ideology specific defs and patches 
├── About/                    # Mod metadata
│   ├── About.xml             # Mod info, dependencies, versions
│   ├── Preview.png           # Steam Workshop preview
│   └── PublishedFileId.txt   # Steam Workshop ID
├── Defs/                      # XML definitions
│   ├── Hediffs.xml           # Health conditions (gender identity, pregnancy abilities)
│   └── Settings.xml          # Mod settings using XML Extensions
└── Languages/                 # Localization
    └── English/Keyed/
        └── SimpleTrans.xml
```

## Key Dependencies

1. **XML Extensions** (imranfish.xmlextensions) - Required for settings menu
2. **Harmony** (brrainz.harmony) - Required for patching game code  
3. **Vanilla Expanded Framework** (OskarPotocki.VanillaFactionsExpanded.Core) - Required dependency
4. **NonBinary Gender** (optional) - Compatibility with nonbinary gender mod

## Core Architecture

### Mod Mechanics
The mod adds several hediffs (health conditions) to pawns:
- **Cisgender/Transgender** - Gender identity markers
- **PregnancyCarry** - Ability to carry pregnancy  
- **PregnancySire** - Ability to sire children

### Key Components

1. **SimpleTrans.cs** - Main entry point
   - Initializes Harmony patches
   - Detects other mod compatibility (HAR, NonBinary Gender)
   - Loads settings from XML Extensions

2. **SimpleTransPregnancyUtility.cs** - Core logic
   - `ValidateOrSetGender()` - Main gender assignment logic
   - `CanCarry()` / `CanSire()` - Check pawn abilities
   - `SetTrans()` / `SetCis()` - Apply gender identity hediffs
   - `ClearGender()` - Remove all gender-related hediffs

3. **Harmony Patches** - Game integration
   - Patches pregnancy utility methods to use mod's gender system
   - Integrates with Vanilla Expanded Framework genes
   - Provides NonBinary Gender compatibility

### Development Notes

- **Decompiled codebase**: This mod was reverse-engineered from compiled DLL using ILSpy
- **Namespace updates**: Changed `VanillaGenesExpanded` → `VEF.Genes` for 1.6 compatibility
- **Fixed decompiler artifacts**: Removed invalid `ref` casts and operator calls
- **Auto-deployment**: Project automatically copies mod to RimWorld on build

### Important Compatibility Notes

- Must load after XML Extensions, Biotech DLC, Harmony, and VSIE
- Incompatible with "rim.job.world"
- The mod uses XML Extensions for its settings system
- Works with gender-forcing genes from various mods (configurable via settings)

### Settings Configuration

Mod settings are configured via XML Extensions and include:
- Population percentage for cisgender pawns
- Reproduction ability percentages for trans pawns
- Gene interaction behavior (whether genes represent AGAB or gender)
- Debug mode toggle

### Building for Multiple Versions

Currently set up for RimWorld 1.6, but the structure supports multi-version builds:
- Update `About.xml` supported versions
- Create version-specific folders (1.5/, 1.6/, etc.)
- Modify project file OutputPath for different versions

## Important Guidelines for AI Assistants

### Common Pitfalls to Avoid

1. **Never modify pawn generation randomness** - The maintainer handles all random pawn generation logic personally
2. **Avoid breaking save compatibility** - Always preserve existing hediff names and structures
3. **Don't change mod load order** - The current load order is carefully tested
4. **Respect the original philosophy** - This mod focuses on respectful transgender representation

### Key Files for Specific Tasks

- **Gender assignment logic**: `SimpleTransPregnancyUtility.cs` - `ValidateOrSetGender()`
- **Pregnancy patches**: Files in `Simple_Trans.Patches/` starting with `Pregnancy`
- **Settings**: `1.6/Defs/Settings.xml` (XML Extensions format)
- **Surgery definitions**: `1.6/Defs/Surgery_SimpleTrans.xml`
- **Ritual definitions**: `ModSupport/Ideology/` folder

### Code Style Preferences

1. **Use comprehensive logging** in debug mode - see `SimpleTransDebug.cs` for examples
2. **Null-check everything** - RimWorld mods must be defensive
3. **Preserve original comments** when refactoring
4. **Use explicit type declarations** over `var` for clarity
5. **Add XML documentation** to all public methods

### Testing Guidelines

1. **Always test with debug mode enabled** in mod settings
2. **Check compatibility with**: Vanilla Expanded Framework, Non-Binary Gender, Intimacy
3. **Test save/load compatibility** after any hediff changes
4. **Verify pregnancy system** works for all gender/capability combinations

### Common Development Tasks

#### Adding a new surgery:
1. Define in `Surgery_SimpleTrans.xml`
2. Create Recipe class in `Surgery/` folder
3. Add dialog if user choice is needed
4. Update localization in `SimpleTrans.xml`

#### Adding mod compatibility:
1. Check mod detection in `SimpleTrans.cs` constructor
2. Add patches in `Simple_Trans.Patches/`
3. Use `[HarmonyPatch]` attributes appropriately
4. Test load order requirements

#### Debugging pregnancy issues:
1. Enable debug mode in settings
2. Check `SimpleTransDebug.DebugPregnancyChance()` output
3. Verify hediffs with dev mode "Simple Trans: Show Gender" action

### Important Context

- This mod was decompiled from a compiled DLL, so some code patterns may seem unusual
- The maintainer is actively developing this mod with AI assistance
- User feedback is important - check GitHub issues before making major changes
- The mod philosophy is inclusive and respectful - maintain this tone in all additions
