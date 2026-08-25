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

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BetterRimworlds.TrueChildAging;

internal static class PregnancyDurationStore
{
    private sealed class Duration { public int Days; }
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Hediff_Pregnant, Duration> durations =
        new System.Runtime.CompilerServices.ConditionalWeakTable<Hediff_Pregnant, Duration>();
    private static readonly FieldInfo gestationField = AccessTools.Field(typeof(RaceProperties), "gestationPeriodDays");

    /// Samples once while the option is on. Turning the option off ignores the stored roll.
    public static void InitializeAtConception(Hediff_Pregnant hediff)
    {
        if (hediff == null || durations.TryGetValue(hediff, out _) || !ShouldExtend(hediff))
            return;

        durations.Add(hediff, new Duration { Days = NineMonthPregnancy.RollDurationDays() });
    }

    public static bool TryGetOverrideDays(Hediff_Pregnant hediff, out float days)
    {
        days = 0f;
        if (!ShouldExtend(hediff))
            return false;

        if (!durations.TryGetValue(hediff, out Duration box) || box.Days <= 0)
        {
            int rolled = NineMonthPregnancy.RollDurationDays();
            if (box == null)
                durations.Add(hediff, new Duration { Days = rolled });
            else
                box.Days = rolled;
            days = rolled;
            return true;
        }

        days = box.Days;
        return true;
    }

    public static float Resolve(RaceProperties raceProps, Hediff_Pregnant hediff)
    {
        return ResolveFromStored(raceProps, hediff);
    }

    public static float ResolveForPawn(RaceProperties raceProps, Pawn pawn)
    {
        return ResolveFromStored(raceProps, pawn?.health?.hediffSet?.GetFirstHediff<Hediff_Pregnant>());
    }

    private static float ResolveFromStored(RaceProperties raceProps, Hediff_Pregnant hediff)
    {
        Settings settings = TrueChildAging.settings;
        bool enabled = settings != null && settings.NineMonthPregnancies;
        bool isHuman = hediff?.def == HediffDefOf.PregnantHuman;
        int stored = TryGetOverrideDays(hediff, out float days) ? (int)days : 0;
        return NineMonthPregnancy.ResolveDays(enabled, isHuman, stored, raceProps.gestationPeriodDays);
    }

    public static void Expose(Hediff_Pregnant hediff)
    {
        int days = durations.TryGetValue(hediff, out Duration box) ? box.Days : 0;
        Scribe_Values.Look(ref days, "tcaNineMonthGestationDays", 0);
        if (days <= 0) return;
        if (box == null) durations.Add(hediff, new Duration { Days = days });
        else box.Days = days;
    }

    private static bool ShouldExtend(Hediff_Pregnant hediff)
    {
        Settings settings = TrueChildAging.settings;
        if (settings == null || hediff?.pawn == null || hediff.def == null)
            return false;

        return NineMonthPregnancy.ShouldExtend(
            settings.NineMonthPregnancies,
            hediff.def == HediffDefOf.PregnantHuman);
    }

    private static bool IsGestationField(CodeInstruction instruction) =>
        instruction.opcode == OpCodes.Ldfld && instruction.operand is FieldInfo field && field == gestationField;

    public static IEnumerable<CodeInstruction> ReplaceGestationReads(IEnumerable<CodeInstruction> instructions, MethodInfo resolver, int pawnArg, string context)
    {
        int replacements = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (!IsGestationField(instruction))
            {
                yield return instruction;
                continue;
            }

            // The RaceProperties value is already on the stack from the vanilla field read.
            replacements++;
            CodeInstruction first = new CodeInstruction(OpCodes.Ldarg, pawnArg)
            {
                labels = instruction.labels,
                blocks = instruction.blocks
            };
            yield return first;
            yield return new CodeInstruction(OpCodes.Call, resolver);
        }

        if (replacements != 1)
            throw new InvalidOperationException($"Expected exactly one gestationPeriodDays read in {context}, found {replacements}.");
    }
}

[HarmonyPatch(typeof(Hediff), nameof(Hediff.PostMake))]
internal static class Patch_Hediff_PostMake
{
    static void Postfix(Hediff __instance)
    {
        if (__instance is Hediff_Pregnant pregnant) PregnancyDurationStore.InitializeAtConception(pregnant);
    }
}

#if RIMWORLD16
[HarmonyPatch(typeof(Hediff_Pregnant), "TickInterval")]
#else
[HarmonyPatch(typeof(Hediff_Pregnant), "Tick")]
#endif
internal static class Patch_Hediff_Pregnant_Tick
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
        PregnancyDurationStore.ReplaceGestationReads(instructions,
            AccessTools.Method(typeof(PregnancyDurationStore), nameof(PregnancyDurationStore.Resolve)), 0,
            "Hediff_Pregnant.Tick/TickInterval");
}

[HarmonyPatch(typeof(Hediff_Pregnant), nameof(Hediff_Pregnant.ExposeData))]
internal static class Patch_Hediff_Pregnant_ExposeData
{
    static void Postfix(Hediff_Pregnant __instance) => PregnancyDurationStore.Expose(__instance);
}

[HarmonyPatch(typeof(Hediff_Pregnant), nameof(Hediff_Pregnant.DebugString))]
internal static class Patch_Hediff_Pregnant_DebugString
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
        PregnancyDurationStore.ReplaceGestationReads(instructions,
            AccessTools.Method(typeof(PregnancyDurationStore), nameof(PregnancyDurationStore.Resolve)), 0,
            "Hediff_Pregnant.DebugString");
}

[HarmonyPatch(typeof(PawnColumnWorker_Pregnant), "GetTooltipText")]
internal static class Patch_PawnColumnWorker_Pregnant_GetTooltipText
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
        PregnancyDurationStore.ReplaceGestationReads(instructions,
            AccessTools.Method(typeof(PregnancyDurationStore), nameof(PregnancyDurationStore.ResolveForPawn)), 0,
            "PawnColumnWorker_Pregnant.GetTooltipText");
}
