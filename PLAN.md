 # PLAN.md — True Child Aging

  ## Summary

  Create True Child Aging as an independent RimWorld mod and repository. It will provide an integer Child Aging Factor slider from -5x to +5x, defaulting to RimWorld’s
  +4x.

  Positive values replace the normal child-aging factor. Negative values actively correct biological age toward chronological age, but never below it. Vanilla cannot
  process negative biological-tick accumulation, so this requires a dedicated Harmony extension to Pawn_AgeTracker. Aging implementation
  (https://raw.githubusercontent.com/Chillu1/RimWorldDecompiled/master/Verse/Pawn_AgeTracker.cs), vanilla 4× default
  (https://raw.githubusercontent.com/Chillu1/RimWorldDecompiled/master/RimWorld/Difficulty.cs).

  ## Standalone Mod Structure

  - Create sibling repository /code/Rimworld/True-Child-Aging.
  - Use:
      - Mod name: True Child Aging
      - Assembly: TrueChildAging
      - Package ID: HopeSeekr.BetterRimworlds.TrueChildAging
      - Harmony ID: HopeSeekr.BetterRimworlds.TrueChildAging

  - Provide independent metadata, README, source solution, settings class, Harmony bootstrap, patches, and test project.
  - Support RimWorld 1.4, 1.5, and 1.6 with version-specific assemblies.
  - Declare Harmony and Biotech as required dependencies and load after Harmony.
  - Do not modify or depend on ED-EnhancedOptions.

  ## Settings and Behavior

  - Persist int ChildAgingFactor under the key ChildAgingFactor, default 4, clamped to [-5, 5].
  - Render an integer-snapping slider with a signed label such as Child Aging Factor: +4x.
  - Apply changes immediately; no restart is required because Harmony patches remain installed and read the current setting.
  - Explain the modes in the tooltip and README:
      - +1x..+5x: replace RimWorld’s child-aging factor.
      - 0x: freeze biological aging while the pawn remains in RimWorld’s child-aging range.
      - -1x..-5x: reverse biological aging at that rate only while biological age exceeds chronological age.

  - Positive values retain RimWorld’s normal transition from child aging toward adult aging.
  - Negative correction is limited to humanlike pawns whose biological age remains within RimWorld’s child-aging range, including its transition period up to age 20.
  - Do not correct biologically adult pawns aged 20 or above, even when their chronological age is lower.
  - Growth vats retain vanilla behavior and are excluded from correction.
  - During correction, growth-point learning continues at a positive 1x rate.

  ## Harmony and Save-State Implementation

  - Patch Pawn_AgeTracker.ChildAgingMultiplier:
      - Return the configured value for positive and zero modes.
      - Return 1x in negative mode for growth-point and other non-aging consumers.

  - Patch Pawn_AgeTracker.BiologicalTicksPerTick:
      - For an eligible pawn in negative mode with biological age ahead of chronological age, return the configured signed correction rate, retaining applicable gene
        multipliers.

      - At or below the boundary, leave the effective child factor at vanilla 1x; other vanilla modifiers remain active.
      - If modifiers subsequently place biological age ahead again, correction may resume.

  - Prefix the private biological-tick routine while correction is active:
      - Support signed fractional accumulation using truncation toward zero for negative whole ticks.
      - Clamp the resulting biological age to the pawn’s current chronological age.
      - Clear the fractional correction remainder when the boundary is reached.
      - Never alter chronological age or allow biological age to cross below it.

  - Isolate the signed-tick and boundary calculation in an internal pure helper so it can be unit tested without RimWorld pawn objects.
  - Add a save-persistent GameComponent recording each correcting pawn and their highest previously reached biological-age tick:
      - Record the watermark before the pawn first moves backward.
      - Keep the greatest watermark across multiple correction sessions.
      - Prefix BirthdayBiological(int) and suppress birthdays at or below the watermark, preventing duplicate growth moments, traits, passions, work unlocks, letters, and
        birthday effects.

      - Retain benefits already earned; reversal does not remove traits, passions, or prior growth rewards.
      - Remove the watermark after biological age advances beyond the previous maximum.
      - Serialize pawn references and tick values and discard invalid references after loading.

  ## Test Plan

  - Add NUnit tests for the pure correction helper:
      - All integer settings from -5 through +5.
      - Signed fractional accumulation across repeated and large intervals.
      - Exact-boundary arrival and attempted undershoot.
      - Biological age already equal to or below chronological age.
      - Gene-adjusted negative rates.
      - Age-20 eligibility cutoff.

  - Build and test the 1.4, 1.5, and 1.6 configurations independently.
  - In-game verification:
      - Default +4x matches vanilla.
      - Positive settings preserve vanilla child-to-adult interpolation.
      - 0x freezes eligible biological aging.
      - Negative settings correct at the selected rate and stop exactly at chronological age.
      - Biologically adult pawns are not corrected.
      - Growth vats remain unchanged.
      - Learning remains positive at 1x during correction.
      - Correction watermarks survive save/load and mod-setting changes.
      - Re-crossed birthdays and growth moments are suppressed, while the first milestone beyond the previous maximum occurs normally.
      - Harmony coexistence is checked with another mod postfixing the same age getters.

  ## Assumptions

  - The standalone mod intentionally overrides the storyteller’s child-aging setting.
  - “Biological children only” means RimWorld’s complete child-aging influence range, ending at biological age 20.
  - Negative mode corrects biological age rather than waiting for chronological age to catch up through cryptosleep.
  - Vanilla gene and aging modifiers remain meaningful, except that the chronological-age floor is absolute.


