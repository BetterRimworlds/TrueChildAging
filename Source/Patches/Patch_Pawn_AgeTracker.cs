/*
 * This file is part of True Child Aging, a Better Rimworlds Project.
 *
 * Copyright © 2024-2026 Theodore R. Smith
 * Author: Theodore R. Smith <hopeseekr@gmail.com>
 *   GPG Fingerprint: D8EA 6E4D 5952 159D 7759  2BB4 EEB6 CE72 F441 EC41
 *   https://github.com/BetterRimworlds/TrueChildAging
 *
 * This file is licensed under the MIT License.
 */

using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterRimworlds.TrueChildAging;

internal static class AgeTrackerAccess
{
    internal static readonly FieldInfo PawnField =
        AccessTools.Field(typeof(Pawn_AgeTracker), "pawn");

    internal static readonly FieldInfo ProgressField =
        AccessTools.Field(typeof(Pawn_AgeTracker), "progressToNextBiologicalTick");

    internal static readonly FieldInfo BiologicalTicksField =
        AccessTools.Field(typeof(Pawn_AgeTracker), "ageBiologicalTicksInt");

    internal static readonly FieldInfo GrowthField =
        AccessTools.Field(typeof(Pawn_AgeTracker), "growth");

    internal static readonly FieldInfo NextGrowthCheckTickField =
        AccessTools.Field(typeof(Pawn_AgeTracker), "nextGrowthCheckTick");

    internal static readonly MethodInfo RecalculateLifeStageIndexMethod =
        AccessTools.Method(typeof(Pawn_AgeTracker), "RecalculateLifeStageIndex");

    internal static Pawn PawnOf(Pawn_AgeTracker tracker)
    {
        return PawnField?.GetValue(tracker) as Pawn;
    }

    internal static bool ShouldOverrideForHumanlike(Pawn_AgeTracker tracker, out Pawn pawn)
    {
        pawn = PawnOf(tracker);
        if (pawn?.RaceProps == null || !pawn.RaceProps.Humanlike)
        {
            return false;
        }

        if (pawn.ParentHolder is Building_GrowthVat)
        {
            return false;
        }

        Settings settings = TrueChildAging.settings;
        if (settings == null)
        {
            return false;
        }

        return BiologicalTickCorrection.ShouldSyncGrowthToBiologicalAge(
            settings.ChildAgingFactor,
            tracker.AgeBiologicalYearsFloat,
            tracker.AgeBiologicalTicks,
            tracker.AgeChronologicalTicks);
    }

    internal static void SyncGrowthToBiologicalAge(Pawn_AgeTracker tracker)
    {
        if (GrowthField == null)
        {
            return;
        }

        float growth = BiologicalTickCorrection.GrowthFromBiologicalTicks(
            tracker.AgeBiologicalTicks,
            tracker.AdultMinAge);
        GrowthField.SetValue(tracker, growth);
    }

    internal static void RecalculateLifeStage(Pawn_AgeTracker tracker)
    {
        RecalculateLifeStageIndexMethod?.Invoke(tracker, null);
    }

    internal static void ScheduleNextGrowthCheck(Pawn_AgeTracker tracker)
    {
        if (NextGrowthCheckTickField == null || Find.TickManager == null)
        {
            return;
        }

        NextGrowthCheckTickField.SetValue(tracker, Find.TickManager.TicksGame + 240);
    }
}

[HarmonyPatch(typeof(Pawn_AgeTracker), "get_ChildAgingMultiplier")]
internal static class Patch_ChildAgingMultiplier
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    static void Postfix(ref float __result)
    {
        Settings settings = TrueChildAging.settings;
        if (settings == null)
        {
            return;
        }

        __result = BiologicalTickCorrection.ChildAgingMultiplierForConsumers(settings.ChildAgingFactor);
    }
}

[HarmonyPatch(typeof(Pawn_AgeTracker), "get_BiologicalTicksPerTick")]
internal static class Patch_BiologicalTicksPerTick
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    static void Postfix(Pawn_AgeTracker __instance, ref float __result)
    {
        Settings settings = TrueChildAging.settings;

        Pawn pawn = AgeTrackerAccess.PawnOf(__instance);
        if (pawn?.RaceProps == null || !pawn.RaceProps.Humanlike)
        {
            return;
        }

        if (pawn.ParentHolder is Building_GrowthVat)
        {
            return;
        }

        int factor = settings.ChildAgingFactor;
        float biologicalYears = __instance.AgeBiologicalYearsFloat;
        long biologicalTicks = __instance.AgeBiologicalTicks;
        long chronologicalTicks = __instance.AgeChronologicalTicks;

        if (BiologicalTickCorrection.CanFreeze(factor, biologicalYears, biologicalTicks, chronologicalTicks))
        {
            __result = 0f;
            return;
        }

        if (!BiologicalTickCorrection.CanCorrect(factor, biologicalYears, biologicalTicks, chronologicalTicks))
        {
            return;
        }

        float geneMultiplier = pawn.genes != null ? pawn.genes.BiologicalAgeTickFactor : 1f;
        __result = BiologicalTickCorrection.SignedBiologicalRate(factor, geneMultiplier);
    }
}

[HarmonyPatch(typeof(Pawn_AgeTracker), "TickBiologicalAge")]
internal static class Patch_TickBiologicalAge
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    static bool Prefix(Pawn_AgeTracker __instance, int interval)
    {
        Settings settings = TrueChildAging.settings;
        if (settings == null)
        {
            return true;
        }

        Pawn pawn = AgeTrackerAccess.PawnOf(__instance);
        if (pawn?.RaceProps == null || !pawn.RaceProps.Humanlike)
        {
            return true;
        }

        if (pawn.ParentHolder is Building_GrowthVat)
        {
            return true;
        }

        if (!BiologicalTickCorrection.CanCorrect(
                settings.ChildAgingFactor,
                __instance.AgeBiologicalYearsFloat,
                __instance.AgeBiologicalTicks,
                __instance.AgeChronologicalTicks))
        {
            return true;
        }

        if (AgeTrackerAccess.ProgressField == null || AgeTrackerAccess.BiologicalTicksField == null)
        {
            return true;
        }

        long biologicalTicks = (long)AgeTrackerAccess.BiologicalTicksField.GetValue(__instance);
        GameComponent_ChildAgingWatermarks.Get()?.RecordWatermark(pawn, biologicalTicks);

        float progress = (float)AgeTrackerAccess.ProgressField.GetValue(__instance);
        BiologicalTickCorrection.TickResult result = BiologicalTickCorrection.Apply(
            biologicalTicks,
            __instance.AgeChronologicalTicks,
            progress,
            interval,
            __instance.BiologicalTicksPerTick);

        AgeTrackerAccess.BiologicalTicksField.SetValue(__instance, result.BiologicalTicks);
        AgeTrackerAccess.ProgressField.SetValue(__instance, result.Progress);
        AgeTrackerAccess.SyncGrowthToBiologicalAge(__instance);

        int yearsBefore = (int)(biologicalTicks / BiologicalTickCorrection.TicksPerYear);
        int yearsAfter = (int)(result.BiologicalTicks / BiologicalTickCorrection.TicksPerYear);
        if (yearsAfter != yearsBefore)
        {
            AgeTrackerAccess.RecalculateLifeStage(__instance);
        }

        return false;
    }

    [HarmonyPostfix]
    static void Postfix(Pawn_AgeTracker __instance)
    {
        Pawn pawn = AgeTrackerAccess.PawnOf(__instance);
        if (pawn == null)
        {
            return;
        }

        GameComponent_ChildAgingWatermarks.Get()
            ?.NotifyBiologicalTicks(pawn, __instance.AgeBiologicalTicks);
    }
}

[HarmonyPatch(typeof(Pawn_AgeTracker), "CalculateGrowth")]
internal static class Patch_CalculateGrowth
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    static bool Prefix(Pawn_AgeTracker __instance)
    {
        if (!AgeTrackerAccess.ShouldOverrideForHumanlike(__instance, out _))
        {
            return true;
        }

        AgeTrackerAccess.SyncGrowthToBiologicalAge(__instance);
        AgeTrackerAccess.ScheduleNextGrowthCheck(__instance);
        AgeTrackerAccess.RecalculateLifeStage(__instance);
        return false;
    }
}

[HarmonyPatch(typeof(Pawn_AgeTracker), "BirthdayBiological")]
internal static class Patch_BirthdayBiological
{
    [HarmonyPrefix]
    static bool Prefix(Pawn_AgeTracker __instance, int birthdayAge)
    {
        Pawn pawn = AgeTrackerAccess.PawnOf(__instance);
        GameComponent_ChildAgingWatermarks watermarks = GameComponent_ChildAgingWatermarks.Get();
        if (watermarks == null || pawn == null)
        {
            return true;
        }

        return !watermarks.ShouldSuppressBirthday(pawn, birthdayAge);
    }
}
