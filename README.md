# Simple Trans (Continued)

A RimWorld mod that adds a comprehensive system for differentiating how pawns interact with pregnancy and AGAR (Assigned Gender at Rimworld), featuring transgender and cisgender pawns with realistic reproductive capabilities.

## About This Version

This is a **continued version** of the original Simple Trans mod by **Runaway**. The original mod was created for RimWorld 1.5 and this version has been **completely decompiled, modernized, and updated for RimWorld 1.6** compatibility.

**Original Author:** Runaway  
**Current Maintainer:** azrazalea  
**RimWorld Version:** 1.6+  

## What This Mod Does

Simple Trans introduces a nuanced gender identity system that separates biological reproductive capabilities from gender presentation:

- **Gender Identity:** Pawns can be cisgender (gender matches biological sex) or transgender (gender differs from biological sex)
- **Reproductive Capabilities:** Independent of gender, pawns can have the ability to carry pregnancies, sire offspring, both, or neither
- **Realistic Combinations:** Supports all combinations like trans men who can carry pregnancies, trans women who can sire, etc.
- **Configurable Rates:** Fully customizable percentage chances for transgender populations and reproductive capabilities

## Key Features

### Core Mechanics
- Pawns assigned cis/trans status on generation based on configurable percentages
- Reproductive abilities (carry/sire) assigned independently of gender presentation
- Pregnancy and lovin' systems check reproductive capabilities instead of gender
- Full integration with RimWorld's existing systems

### Compatibility & Integration
- **Vanilla Expanded Framework:** Full support for gendered genes with cisgender restrictions
- **Non-Binary Gender:** Complete compatibility with NBG mod
- **Biotech DLC:** Full integration with genes and pregnancy systems
- **XML Extensions:** Comprehensive settings menu

### Debug Tools
Extensive dev mode options for testing and troubleshooting:
- Validate all humanlike genders
- Set transgender/cisgender status
- Manage reproductive capabilities
- Clear and regenerate Simple Trans hediffs

## Installation Requirements

### Required Mods
- **Harmony** (brrainz.harmony)
- **XML Extensions** (imranfish.xmlextensions) 
- **Vanilla Expanded Framework** (OskarPotocki.VanillaFactionsExpanded.Core)

### Compatible Mods
- **Way Better Romance** - Enhanced romance mechanics (compatible)
- **Non-Binary Gender** - Adds non-binary gender options
- **Samesex IVF** - Compatible, but Simple Trans handles IVF procedures independently

### Load Order
Must be loaded **after** Vanilla Social Interactions Expanded to ensure proper pregnancy functionality.

## Incompatibilities

- **RJW** - Overlapping functionality
- **Universal Pregnancy** - Potential conflicts (untested)
- **Dysphoria** - Different approaches to transgender mechanics
- **Hermaphrodite Gene Continued** - Patches same code paths
- **Way Better Romance + Non-Binary Gender together** - Known incompatibility

## Technical Information

### Development
This version was created by:
1. **Decompiling** the original 1.5 mod using ILSpy
2. **Modernizing** the codebase with C# best practices
3. **Updating** for RimWorld 1.6 compatibility
4. **Adding** comprehensive error handling and documentation
5. **Enhancing** with improved debugging tools

### Architecture
- **Harmony Patches:** Integrates with RimWorld's core systems
- **Hediff System:** Uses health conditions to track gender identity and reproductive capabilities
- **Gene Integration:** Works with Biotech genes and VEF extensions
- **Settings System:** XML Extensions-powered configuration

## Contributing

This mod's source code is fully documented and modernized. Contributions are welcome for:
- Bug fixes and compatibility improvements
- Additional mod integrations
- Feature enhancements
- Translation support

## License & Attribution

This mod is a continuation of **Simple Trans** by **Runaway**. The original concept, design, and implementation belong to the original author. This version represents a technical modernization and compatibility update.

**Original Mod Philosophy (by Runaway):**
> "This mod was inspired by frustration with other options for having explicitly trans pawns. No shade on RJW, Universal Pregnancy, or Dysphoria, but none of those were what I was looking for here."

**Trans rights are human rights.**

## Support

For issues, suggestions, or questions about this continued version, please use the appropriate mod channels. This is a community-maintained continuation focused on preserving and modernizing the original mod's functionality for current RimWorld versions.

---

*This README reflects the continued version for RimWorld 1.6. The original Simple Trans mod and its design philosophy remain credited to Runaway.*