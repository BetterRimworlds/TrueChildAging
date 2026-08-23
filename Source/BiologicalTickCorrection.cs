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

namespace BetterRimworlds.TrueChildAging;

/// <summary>
/// Signed biological-tick math used by the Harmony patches. Isolated so it can
/// be unit-tested without RimWorld pawn objects.
/// </summary>
internal static class BiologicalTickCorrection
{
    public const int MinFactor = -5;
    public const int MaxFactor = 5;
    public const int DefaultFactor = 4;
    public const float ChildAgingRangeEndYears = 20f;
    public const long TicksPerYear = 3600000L;

    public readonly struct TickResult
    {
        public TickResult(long biologicalTicks, float progress, bool reachedFloor)
        {
            BiologicalTicks = biologicalTicks;
            Progress = progress;
            ReachedFloor = reachedFloor;
        }

        public long BiologicalTicks { get; }
        public float Progress { get; }

        /// <summary>
        /// True when correction stopped at the effective floor: the greater of
        /// chronological age and the current life-stage minimum age.
        /// </summary>
        public bool ReachedFloor { get; }

        public bool ReachedChronologicalFloor => ReachedFloor;
    }

    public static int ClampFactor(int factor)
    {
        if (factor < MinFactor)
        {
            return MinFactor;
        }

        if (factor > MaxFactor)
        {
            return MaxFactor;
        }

        return factor;
    }

    /// <summary>
    /// Value exposed through <c>ChildAgingMultiplier</c>. Zero and negative
    /// modes report 1x so that, once biological age is no longer ahead of
    /// chronological age, aging and growth-point learning tick 1:1. Freeze and
    /// reverse are applied only in the biological-tick path while bio is ahead.
    /// </summary>
    public static float ChildAgingMultiplierForConsumers(int factor)
    {
        factor = ClampFactor(factor);
        return factor <= 0 ? 1f : factor;
    }

    public static bool IsCorrectionMode(int factor)
    {
        return ClampFactor(factor) < 0;
    }

    public static bool IsFreezeMode(int factor)
    {
        return ClampFactor(factor) == 0;
    }

    public static bool IsInChildAgingRange(float biologicalYears)
    {
        return biologicalYears < ChildAgingRangeEndYears;
    }

    public static bool BiologicalAgeIsAhead(long biologicalTicks, long chronologicalTicks)
    {
        return biologicalTicks > chronologicalTicks;
    }

    /// <summary>
    /// 0x holds biological age still only while it is ahead of chronological
    /// age. Once they meet, the child factor is 1x (true 1:1, plus vanilla
    /// gene / adult-interpolation modifiers).
    /// </summary>
    public static bool CanFreeze(
        int factor,
        float biologicalYears,
        long biologicalTicks,
        long chronologicalTicks)
    {
        return IsFreezeMode(factor)
            && IsInChildAgingRange(biologicalYears)
            && BiologicalAgeIsAhead(biologicalTicks, chronologicalTicks);
    }

    public static bool CanCorrect(
        int factor,
        float biologicalYears,
        long biologicalTicks,
        long chronologicalTicks,
        long lifeStageMinTicks = 0)
    {
        if (!IsCorrectionMode(factor)
            || !IsInChildAgingRange(biologicalYears)
            || !BiologicalAgeIsAhead(biologicalTicks, chronologicalTicks))
        {
            return false;
        }

        return biologicalTicks > EffectiveFloorTicks(chronologicalTicks, lifeStageMinTicks);
    }

    /// <summary>
    /// Negative mode has already reached the current life-stage minimum while
    /// biological age is still ahead of chronological age. Hold still rather
    /// than reversing into the previous stage (child → toddler, toddler → baby)
    /// or aging forward again at 1x.
    /// </summary>
    public static bool CanHoldAtLifeStageFloor(
        int factor,
        float biologicalYears,
        long biologicalTicks,
        long chronologicalTicks,
        long lifeStageMinTicks)
    {
        if (!IsCorrectionMode(factor)
            || !IsInChildAgingRange(biologicalYears)
            || !BiologicalAgeIsAhead(biologicalTicks, chronologicalTicks))
        {
            return false;
        }

        return biologicalTicks <= EffectiveFloorTicks(chronologicalTicks, lifeStageMinTicks);
    }

    /// <summary>
    /// Ticks at the start of a life stage. Reverse aging must not cross below
    /// this value, or <c>RecalculateLifeStageIndex</c> would drop the pawn into
    /// the previous stage.
    /// </summary>
    public static long LifeStageFloorTicks(float minAgeYears)
    {
        if (minAgeYears <= 0f)
        {
            return 0L;
        }

        double ticks = (double)minAgeYears * TicksPerYear;
        if (ticks >= long.MaxValue)
        {
            return long.MaxValue;
        }

        return (long)Math.Ceiling(ticks);
    }

    public static long EffectiveFloorTicks(long chronologicalTicks, long lifeStageMinTicks)
    {
        return lifeStageMinTicks > chronologicalTicks ? lifeStageMinTicks : chronologicalTicks;
    }

    public static float SignedBiologicalRate(int factor, float geneMultiplier)
    {
        return ClampFactor(factor) * geneMultiplier;
    }

    /// <summary>
    /// True while freeze or reverse is holding/correcting biological age.
    /// Vanilla <c>CalculateGrowth</c> still adds wall-clock progress in those
    /// modes, which is what the age tooltip's <c>growth</c> value shows.
    /// </summary>
    public static bool ShouldSyncGrowthToBiologicalAge(
        int factor,
        float biologicalYears,
        long biologicalTicks,
        long chronologicalTicks,
        long lifeStageMinTicks = 0)
    {
        return CanFreeze(factor, biologicalYears, biologicalTicks, chronologicalTicks)
            || CanCorrect(factor, biologicalYears, biologicalTicks, chronologicalTicks, lifeStageMinTicks)
            || CanHoldAtLifeStageFloor(
                factor,
                biologicalYears,
                biologicalTicks,
                chronologicalTicks,
                lifeStageMinTicks);
    }

    /// <summary>
    /// Matches vanilla <c>CalculateInitialGrowth</c>: fraction of adult min age,
    /// clamped to [0, 1].
    /// </summary>
    public static float GrowthFromBiologicalTicks(long biologicalTicks, float adultMinAge)
    {
        if (adultMinAge <= 0f)
        {
            return 1f;
        }

        if (biologicalTicks <= 0)
        {
            return 0f;
        }

        float years = biologicalTicks / (float)TicksPerYear;
        if (years >= adultMinAge)
        {
            return 1f;
        }

        return years / adultMinAge;
    }

    public static bool ShouldSuppressBirthday(long watermarkTicks, int birthdayAge)
    {
        return watermarkTicks >= 0 && (long)birthdayAge * TicksPerYear <= watermarkTicks;
    }

    public static bool ShouldClearWatermark(long biologicalTicks, long watermarkTicks)
    {
        return watermarkTicks >= 0 && biologicalTicks > watermarkTicks;
    }

    public static long CombineWatermark(long existingWatermark, long candidateTicks)
    {
        if (existingWatermark < 0)
        {
            return candidateTicks;
        }

        return candidateTicks > existingWatermark ? candidateTicks : existingWatermark;
    }

    /// <summary>
    /// Truncates toward zero. Vanilla <c>FloorToInt</c> is unsafe for negative
    /// accumulation (it rounds toward -∞).
    /// </summary>
    public static int TruncateTowardZero(float value)
    {
        return (int)value;
    }

    public static TickResult Apply(
        long biologicalTicks,
        long chronologicalTicks,
        float progress,
        int interval,
        float rate,
        long lifeStageMinTicks = 0)
    {
        if (interval < 0)
        {
            interval = 0;
        }

        long floorTicks = EffectiveFloorTicks(chronologicalTicks, lifeStageMinTicks);
        if (biologicalTicks <= floorTicks)
        {
            return new TickResult(biologicalTicks, progress, true);
        }

        progress += rate * interval;
        int wholeTicks = TruncateTowardZero(progress);
        if (wholeTicks != 0)
        {
            progress -= wholeTicks;
        }

        long newBiologicalTicks = biologicalTicks + wholeTicks;
        bool reachedFloor = false;
        if (newBiologicalTicks <= floorTicks)
        {
            newBiologicalTicks = floorTicks;
            progress = 0f;
            reachedFloor = true;
        }

        return new TickResult(newBiologicalTicks, progress, reachedFloor);
    }
}
