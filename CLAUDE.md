# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Simple Trans is a RimWorld mod that adds a system for differentiating how pawns interact with pregnancy and AGAR (Assigned Gender at Rimworld). The mod was originally created for RimWorld 1.5 and has been decompiled and updated for 1.6 compatibility.

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
├── 1.5/                       # Original 1.5 version (reference)
├── 1.6/                       # Updated 1.6 version
│   └── Assemblies/            # Compiled DLLs (auto-generated)
├── About/                     # Mod metadata
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