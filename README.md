# Simple Trans (Expanded)

🏳️‍⚧️ **Gender Identity & Reproductive Biology System for RimWorld**

A comprehensive continuation of Runaway's original Simple Trans mod, completely rebuilt for RimWorld 1.6+ with extensive medical systems, mod compatibility, and safety features.

## About This Version

This is a **major expansion** of the original Simple Trans mod by **Runaway**. Originally for RimWorld 1.5, this version has been **completely decompiled, modernized, and updated for RimWorld 1.6+** with significant new features and improvements.

**Original Author:** Runaway  
**Current Maintainer:** azrazalea  
**RimWorld Version:** 1.6+  
**Original Mod:** [Simple Trans](https://steamcommunity.com/sharedfiles/filedetails/?id=3345402828) (RimWorld 1.5)

⚠️ **If you're on RimWorld 1.5, please use the original mod!**

## Core Concept

Separates gender identity from reproductive biology using RimWorld's hediff system. Instead of gender determining fertility, pawns get individual reproductive capabilities and gender identities that can be medically modified.

**Trans rights are human rights.**

## Key Features

### 🧬 Gender Identity & Reproduction System
- **Gender Identity Hediffs:** Pawns spawn with "Cisgender" or "Transgender" hediffs (configurable spawn rates)
- **Reproductive Capabilities:** Separate "Carrying Capability" and "Siring Capability" hediffs control reproduction
- **All Combinations Supported:** Trans men who can carry, trans women who can sire, etc.
- **Fully Configurable:** Control spawn percentages and reproductive ability rates
- **Updated IVF & Fertility:** All surgeries work based on reproductive capabilities, not gender

### 🔬 Medical Systems

#### Organ Transplantation
- Extract reproductive organs from one pawn, transplant to another
- Extracted organs maintain their type (natural carrying/siring anatomy)
- Surgery can fail, potentially causing capability-specific sterilization
- Pregnancy safely terminated if carry organs removed (with warning)
- Children from transplanted organs are genetic children of current organ owner

#### Prosthetic Systems
- **Basic Prosthetics:** 70% fertility rate compared to natural anatomy
- **Bionic Prosthetics:** 120% fertility rate with perfect contraception control
  - Both partners with matching bionics wanting children: 100% pregnancy chance
  - Either partner with bionics wanting to avoid pregnancy: 0% chance
- Prosthetics work alongside base capabilities

#### Sterilization System
- Capability-specific sterilization: "SterilizedCarry" vs "SterilizedSire" hediffs
- Failed surgeries apply appropriate sterilization type
- Backwards compatibility converts vanilla "Sterilized" hediffs

### 🎭 Ideology DLC Features
*(Optional - mod doesn't require Ideology)*

#### Gender Affirmation Party Ritual
- Ritual for changing name and gender identity
- Community celebration of gender transition
- Provides mood bonuses and social recognition

#### Biosculpter Gender Affirming Cycle
- **Body type transformation:** Changes physical appearance (vanilla body types)
- **Reproductive organ modification:** Alters reproductive capabilities
- Removes prosthetics to replace with natural organs
- Cures all sterilizations

### 🛡️ Safety & Consent System
- Comprehensive informed consent dialogs for irreversible procedures
- Pregnancy preservation warnings with detailed change summaries
- Color-coded UI showing exactly what will happen
- Changes can be undone through additional procedures

### 🔧 Gene Integration
- **Vanilla Expanded Framework:** Complete integration with gene systems
- **Configurable Gene Behavior:** Genes can represent assigned gender at birth OR current identity
- **Body Type Respect:** Changes respect cisgender status (won't alter cis pawns inappropriately)
- **Gene-Gender Conflicts:** Handled intelligently

## Installation Requirements

### Required
- **Biotech DLC**
- **Harmony** (brrainz.harmony)
- **XML Extensions** (imranfish.xmlextensions) 
- **Vanilla Expanded Framework** (OskarPotocki.VanillaFactionsExpanded.Core)

### Load Order
Must load **AFTER** Vanilla Social Interactions Expanded

### Compatibility
✅ **Intimacy** - Full compatibility  
✅ **Non-Binary Gender** - Full integration (everyone attracted to non-binary pawns currently)  
✅ **Intimacy + Non-Binary Gender** - Full compatibility (special patching)  
✅ **Vanilla Framework Expanded** - Complete gene system integration  
❌ **Same Sex IVF** - Not recommended (Simple Trans handles IVF independently)

### Incompatibilities
- **RJW** - Overlapping functionality
- **Universal Pregnancy** - Potential conflicts
- **Dysphoria** - Different approaches to transgender mechanics

## Mid-Save Installation

⚠️ **If adding mid-save:** Go to Development Mode → Debug Tools → Click "Validate All Genders" to set up existing pawns. Use debug tools to customize individual pawns as needed.

## Debug Tools

### Debug Mode Features
- Extensive logging for pregnancy/romance mechanics and mod compatibility testing
- Enable in mod settings for detailed reports
- Essential for testing compatibility with other mods

### Dev Actions
- Validate all genders
- Set individual gender identities
- Modify reproductive capabilities
- Clear and regenerate Simple Trans hediffs

## Technical Information

### Development
This version was created by:
1. **Decompiling** the original mod using ILSpy
2. **Modernizing** the codebase with C# best practices
3. **Updating** for RimWorld 1.6 compatibility
4. **Adding** comprehensive error handling and documentation
5. **Enhancing** with improved debugging tools

**AI Assistance Disclosure:** This continuation makes extensive use of AI coding assistants (Claude Opus/Sonnet 4) for code modernization, documentation, and implementation of new features. The maintainer reviews all AI-generated code, handles complex logic (particularly random pawn generation), and ensures code quality and correctness.

### Architecture
- **Harmony Patches:** Integrates with RimWorld's core systems
- **Hediff System:** Uses health conditions to track gender identity and reproductive capabilities
- **Gene Integration:** Works with Biotech genes and VEF extensions
- **Settings System:** XML Extensions-powered configuration

### Building from Source

To compile this mod from source, you'll need:

1. **.NET Framework 4.8** or higher
2. **RimWorld installation** (the project references game assemblies)
3. **Required mod dependencies** in adjacent directories:
   - `../VanillaExpandedFramework/1.6/Assemblies/VEF.dll`
   - `../XML Extensions/1.6/Assemblies/XmlExtensions.dll`
   - `../Non-Binary Gender/1.6/Assemblies/NonBinaryGender.dll` (optional)

The project expects these mods to be installed in directories parallel to this mod. If your mod directory structure differs, update the paths in `Source/Simple-Trans.csproj`.

To build:
```bash
dotnet build Source/Simple-Trans.csproj
```

The build process will automatically:
- Compile the mod to `1.6/Assemblies/`
- Deploy the entire mod to your RimWorld Mods folder

## Contributing

This mod's source code is fully documented and modernized. Contributions are welcome for:
- Bug fixes and compatibility improvements
- Additional mod integrations
- Feature enhancements
- Translation support

## Links & Resources

📁 **[GitHub Repository](https://github.com/azrazalea/Simple-Trans)** - Source code, issues, and contributions  
🔗 **[Original Simple Trans Mod](https://steamcommunity.com/sharedfiles/filedetails/?id=3345402828)** - By Runaway (for RimWorld 1.5)

## License & Attribution

This mod is a continuation of **Simple Trans** by **Runaway**. The original concept, design, and implementation belong to the original author. This version represents a technical modernization and major feature expansion.

**Original Mod Philosophy (by Runaway):**
> "This mod was inspired by frustration with other options for having explicitly trans pawns. No shade on RJW, Universal Pregnancy, or Dysphoria, but none of those were what I was looking for here."

**Trans rights are human rights.**

## Support

For issues, suggestions, or questions about this continued version, please use the GitHub repository or appropriate mod channels. This is a community-maintained continuation focused on preserving and expanding the original mod's functionality for current RimWorld versions.

---

*Mid-save compatible • Full source code modernization • Extensive medical systems*