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

/// Human pregnancy duration used by the 9 Month Pregnancies setting.
/// Isolated so duration math can be unit-tested without RimWorld types.
internal static class NineMonthPregnancy
{
    public const int BaseDays = 45;
    public const int VarianceDays = 6;
    public const int MinDays = BaseDays - VarianceDays;
    public const int MaxDays = BaseDays + VarianceDays;

    public static int RollDurationDays()
    {
        return BetterRandom.pick(MinDays, MaxDays);
    }

    public static bool ShouldExtend(bool enabled, bool isHumanPregnancy)
    {
        return enabled && isHumanPregnancy;
    }

    /// Live setting is the authority. A stored 9-month roll is ignored while the option is off.
    public static float ResolveDays(bool enabled, bool isHumanPregnancy, int overrideDays, float vanillaDays)
    {
        if (!ShouldExtend(enabled, isHumanPregnancy) || overrideDays <= 0)
        {
            return vanillaDays;
        }

        return overrideDays;
    }
}
