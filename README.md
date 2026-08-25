# RimWorld True Child Aging

True Child Aging replaces RimWorld's storyteller child-aging factor with an integer **Child Aging Factor** slider from **-5x to 5x**. The default is **4x**, matching vanilla Biotech.

Vanilla cannot process negative biological-tick accumulation, so this mod Harmony-patches `Pawn_AgeTracker` and uses signed fractional tick math that truncates toward zero.

## Thorough Automated Tests Suite

**NOTE:** This mod has an entire suite of automated tests in order to ensure that your youngest colonists will age appropriately according to this mod's settings. The utmost care is taken to ensure they will reach their true Biological Age

## Modes

| Setting        | Effect                                                                                                             |
|----------------|--------------------------------------------------------------------------------------------------------------------|
| **1x to 5x**   | Children age at this speed (vanilla is 4x). From 11 to 20 they gradually slow to the adult rate. |
| **0x**         | Freeze biological aging only while it is ahead of chronological age. When they meet, both tick **1:1**. |
| **-1x to -5x** | Reverse biological aging while bio is ahead of chrono. When they meet, both tick **1:1**. Never below chronological age, and never below the pawn's current life stage (a child cannot become a toddler; a toddler cannot become a baby). |

Freeze and reverse only apply to humanlike pawns younger than 20. Adults (biological age 20+) are never slowed, frozen, or reversed, even if their chronological age is lower. Reverse aging also stops at the current life-stage minimum: biological age will not drop a child to toddler or a toddler to baby. If chronological age is still below that floor, biological age holds until chronological age catches up.

Growth vats keep vanilla behavior and are excluded from correction. During negative correction, growth-point learning continues at a positive **1x** rate.

Changes apply immediately; Harmony patches stay installed and read the current setting. No restart is required.

## 9 Month Pregnancies

Optional 9 Month Pregnancies (On/Off): 

* When On, human pregnancies last 45 days (9 RimWorld months) plus or minus 6 days. 
* When Off, human pregnancies use vanilla 18 days. The current setting applies immediately to any future pregnancy.

## Birthdays and growth moments

When a child is walked backward, the mod records a save-persistent watermark of the highest biological age they had already reached. Birthdays, growth moments, traits, passions, work unlocks, letters, and other birthday effects at or below that watermark are suppressed so they are not awarded twice. Benefits already earned are kept. The watermark is removed after biological age advances past the previous maximum.

## Requirements

- RimWorld **1.4**, **1.5**, or **1.6**
- [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)
- **Biotech**

Package ID: `HopeSeekr.BetterRimworlds.TrueChildAging`

## Building

Requires the [.NET SDK](https://dotnet.microsoft.com/) and RimWorld game assemblies under `/rimworld/<version>/`. Assemblies are compiled per version, then the mod folder is synced into each RimWorld Mods directory.

```bash
./build.sh
```

Unit tests for the signed-tick helper:

```bash
dotnet test Tests/TrueChildAging.Tests.csproj
```

## Latest Changes

#### Version 1.0.0

* **[2026-08-25 04:56:05 EEST]** Added the Steam Workshop mod ID.
* **[2026-08-25 03:51:15 EEST]** Added the Preview Image and Mod Icon.
* **[2026-08-24 10:43:21 EEST]** Added an option for 9 month pregancies.
* **[2026-08-23 23:31:08 EEST]** Applied life-stage floor when reverse-correcting pawn age.
* **[2026-08-23 23:07:14 EEST]** Added life-stage floor to reverse biological-tick correction.
* **[2026-08-23 22:27:33 EEST]** Added Harmony patches for child aging freeze and reverse.
* **[2026-08-23 22:19:07 EEST]** Added save-persistent child-aging watermark GameComponent.
* **[2026-08-23 22:05:18 EEST]** Added signed biological-tick correction helper.
* **[2026-08-23 22:02:46 EEST]** Added the Settings page.
* **[2026-08-23 19:41:54 EEST]** Initial.

You can also read the full [CHANGELOG.md](CHANGELOG.md).

## License

**CC-BY-ND-4.0**
Creative Commons NoDerivations v4.0: Please see the [license file](LICENSE.md) for more information.

**YOU MAY FORK THIS PROJECT.**

**YOU MAY NOT PUBLISH ANY DERIVATION of this project to either your own website or a third-party host.**

**YOU MAY NOT PUBLISH ANY DERIVATION ON STEAM WORKSHOP WITHOUT EXPLICIT APPROVAL.**

## Contributors

[Theodore R. Smith](https://github.com/hopeseekr/) <hopeseekr@gmail.com>  
GPG Fingerprint: D8EA 6E4D 5952 159D 7759  2BB4 EEB6 CE72 F441 EC41  
WhatsApp / Signal: +1 832-303-9477
