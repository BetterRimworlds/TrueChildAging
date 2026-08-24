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

using NUnit.Framework;

namespace BetterRimworlds.TrueChildAging.Tests;

[TestFixture]
public class NineMonthPregnancyTests
{
    [Test]
    public void DurationRange_IsNineRimworldMonthsPlusOrMinusSixDays()
    {
        Assert.AreEqual(45, NineMonthPregnancy.BaseDays);
        Assert.AreEqual(6, NineMonthPregnancy.VarianceDays);
        Assert.AreEqual(39, NineMonthPregnancy.MinDays);
        Assert.AreEqual(51, NineMonthPregnancy.MaxDays);
    }

    [Test]
    public void RollDurationDays_StaysWithinInclusiveVariance()
    {
        for (int i = 0; i < 200; i++)
        {
            int days = NineMonthPregnancy.RollDurationDays();
            Assert.GreaterOrEqual(days, NineMonthPregnancy.MinDays);
            Assert.LessOrEqual(days, NineMonthPregnancy.MaxDays);
        }
    }

    [Test]
    public void ShouldExtend_OnlyWhenEnabledAndHumanPregnancy()
    {
        Assert.IsFalse(NineMonthPregnancy.ShouldExtend(false, true));
        Assert.IsFalse(NineMonthPregnancy.ShouldExtend(true, false));
        Assert.IsFalse(NineMonthPregnancy.ShouldExtend(false, false));
        Assert.IsTrue(NineMonthPregnancy.ShouldExtend(true, true));
    }

    [Test]
    public void ResolveDays_WhenDisabled_UsesVanillaEvenIfNineMonthRollIsStored()
    {
        Assert.AreEqual(18f, NineMonthPregnancy.ResolveDays(false, true, 45, 18f));
        Assert.AreEqual(18f, NineMonthPregnancy.ResolveDays(false, true, 39, 18f));
        Assert.AreEqual(18f, NineMonthPregnancy.ResolveDays(false, true, 51, 18f));
        Assert.AreEqual(18f, NineMonthPregnancy.ResolveDays(false, true, 0, 18f));
    }

    [Test]
    public void ResolveDays_WhenEnabled_UsesStoredNineMonthRollForHumanPregnancy()
    {
        Assert.AreEqual(45f, NineMonthPregnancy.ResolveDays(true, true, 45, 18f));
        Assert.AreEqual(39f, NineMonthPregnancy.ResolveDays(true, true, 39, 18f));
        Assert.AreEqual(18f, NineMonthPregnancy.ResolveDays(true, true, 0, 18f));
        Assert.AreEqual(18f, NineMonthPregnancy.ResolveDays(true, false, 45, 18f));
    }

    [Test]
    public void BetterRandomPick_IncludesBothBounds()
    {
        Assert.AreEqual(45, BetterRandom.pick(45, 45));

        bool sawMin = false;
        bool sawMax = false;
        for (int i = 0; i < 500; i++)
        {
            int value = BetterRandom.pick(39, 51);
            Assert.GreaterOrEqual(value, 39);
            Assert.LessOrEqual(value, 51);
            if (value == 39)
            {
                sawMin = true;
            }

            if (value == 51)
            {
                sawMax = true;
            }
        }

        Assert.IsTrue(sawMin, "Inclusive min (39) should appear in 500 rolls.");
        Assert.IsTrue(sawMax, "Inclusive max (51) should appear in 500 rolls.");
    }
}
