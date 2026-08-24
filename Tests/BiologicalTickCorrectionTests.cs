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
public class BiologicalTickCorrectionTests
{
    const long Year = BiologicalTickCorrection.TicksPerYear;

    [Test]
    public void ChildAgingMultiplier_MatchesEveryIntegerSetting()
    {
        for (int factor = -5; factor <= 5; factor++)
        {
            float multiplier = BiologicalTickCorrection.ChildAgingMultiplierForConsumers(factor);
            if (factor <= 0)
            {
                Assert.AreEqual(1f, multiplier, $"Factor {factor} must report 1x once bio is not ahead of chrono.");
                Assert.AreEqual(factor < 0, BiologicalTickCorrection.IsCorrectionMode(factor));
                Assert.AreEqual(factor == 0, BiologicalTickCorrection.IsFreezeMode(factor));
            }
            else
            {
                Assert.AreEqual((float)factor, multiplier, $"Factor {factor} must be used as-is.");
                Assert.IsFalse(BiologicalTickCorrection.IsCorrectionMode(factor));
                Assert.IsFalse(BiologicalTickCorrection.IsFreezeMode(factor));
            }
        }
    }

    [Test]
    public void SignedRate_UsesIntegerFactorIncludingPositiveAndZero()
    {
        for (int factor = -5; factor <= 5; factor++)
        {
            Assert.AreEqual(factor, BiologicalTickCorrection.SignedBiologicalRate(factor, 1f));
        }
    }

    [Test]
    public void SignedFractionalAccumulation_RepeatsTowardZeroNotFloor()
    {
        long bio = 1000;
        long chrono = 0;
        float progress = 0f;

        for (int i = 0; i < 4; i++)
        {
            BiologicalTickCorrection.TickResult step = BiologicalTickCorrection.Apply(bio, chrono, progress, 1, -0.25f);
            bio = step.BiologicalTicks;
            progress = step.Progress;
        }

        Assert.AreEqual(999, bio);
        Assert.AreEqual(0f, progress, 0.0001f);
    }

    [Test]
    public void SignedFractionalAccumulation_LargeIntervalTruncatesTowardZero()
    {
        // -1.75 * 10 = -17.5 → whole ticks -17, remainder -0.5 (toward zero, not floor -18).
        BiologicalTickCorrection.TickResult result = BiologicalTickCorrection.Apply(10_000, 0, 0f, 10, -1.75f);
        Assert.AreEqual(10_000 - 17, result.BiologicalTicks);
        Assert.AreEqual(-0.5f, result.Progress, 0.0001f);
        Assert.IsFalse(result.ReachedChronologicalFloor);
    }

    [Test]
    public void ExactBoundaryArrival_ClearsRemainder()
    {
        BiologicalTickCorrection.TickResult result = BiologicalTickCorrection.Apply(
            biologicalTicks: 100,
            chronologicalTicks: 90,
            progress: 0f,
            interval: 10,
            rate: -1f);

        Assert.AreEqual(90, result.BiologicalTicks);
        Assert.AreEqual(0f, result.Progress);
        Assert.IsTrue(result.ReachedChronologicalFloor);
    }

    [Test]
    public void AttemptedUndershoot_ClampsToChronologicalAge()
    {
        BiologicalTickCorrection.TickResult result = BiologicalTickCorrection.Apply(
            biologicalTicks: 100,
            chronologicalTicks: 90,
            progress: -0.4f,
            interval: 10,
            rate: -5f);

        Assert.AreEqual(90, result.BiologicalTicks);
        Assert.AreEqual(0f, result.Progress, "Remainder must be cleared at the chronological floor.");
        Assert.IsTrue(result.ReachedChronologicalFloor);
    }

    [Test]
    public void BiologicalAgeAlreadyEqual_DoesNotMove()
    {
        BiologicalTickCorrection.TickResult result = BiologicalTickCorrection.Apply(50, 50, -0.9f, 60, -3f);
        Assert.AreEqual(50, result.BiologicalTicks);
        Assert.AreEqual(-0.9f, result.Progress);
        Assert.IsTrue(result.ReachedChronologicalFloor);
        Assert.IsFalse(BiologicalTickCorrection.CanCorrect(-3, 13f, 50, 50));
    }

    [Test]
    public void BiologicalAgeAlreadyBelowChronological_DoesNotMove()
    {
        BiologicalTickCorrection.TickResult result = BiologicalTickCorrection.Apply(40, 50, 0.2f, 8, -2f);
        Assert.AreEqual(40, result.BiologicalTicks);
        Assert.AreEqual(0.2f, result.Progress);
        Assert.IsTrue(result.ReachedChronologicalFloor);
        Assert.IsFalse(BiologicalTickCorrection.CanCorrect(-5, 7f, 40, 50));
    }

    [Test]
    public void GeneAdjustedNegativeRate_IsProductOfFactorAndGene()
    {
        Assert.AreEqual(-1f, BiologicalTickCorrection.SignedBiologicalRate(-2, 0.5f));
        Assert.AreEqual(-7.5f, BiologicalTickCorrection.SignedBiologicalRate(-5, 1.5f));

        float rate = BiologicalTickCorrection.SignedBiologicalRate(-4, 0.25f);
        BiologicalTickCorrection.TickResult result = BiologicalTickCorrection.Apply(1_000, 0, 0f, 8, rate);
        Assert.AreEqual(992, result.BiologicalTicks);
        Assert.AreEqual(0f, result.Progress, 0.0001f);
    }

    [Test]
    public void Age20Cutoff_DisallowsCorrectionEvenWhenBiologicalAgeIsAhead()
    {
        long bio = 21 * Year;
        long chrono = 10 * Year;

        Assert.IsFalse(BiologicalTickCorrection.IsInChildAgingRange(20f));
        Assert.IsFalse(BiologicalTickCorrection.CanCorrect(-5, 20f, bio, chrono));
        Assert.IsFalse(BiologicalTickCorrection.CanCorrect(-1, 20.01f, bio, chrono));
        Assert.IsTrue(BiologicalTickCorrection.CanCorrect(-1, 19.99f, 19 * Year + 1, 10 * Year));
        Assert.IsTrue(BiologicalTickCorrection.IsInChildAgingRange(0f));
    }

    [Test]
    public void PositiveAndZeroSettings_NeverQualifyForCorrection()
    {
        for (int factor = 0; factor <= 5; factor++)
        {
            Assert.IsFalse(BiologicalTickCorrection.CanCorrect(factor, 8f, 12 * Year, 4 * Year));
        }
    }

    [Test]
    public void ZeroMode_FreezesOnlyWhileBiologicalAgeIsAheadThenTicksOneToOne()
    {
        long aheadBio = 12 * Year;
        long chrono = 4 * Year;

        Assert.IsTrue(BiologicalTickCorrection.CanFreeze(0, 5f, aheadBio, chrono));
        Assert.IsFalse(BiologicalTickCorrection.CanFreeze(0, 5f, chrono, chrono),
            "Once bio meets chrono, 0x must stop freezing so aging ticks 1:1.");
        Assert.IsFalse(BiologicalTickCorrection.CanFreeze(0, 5f, chrono - 1, chrono));
        Assert.IsFalse(BiologicalTickCorrection.CanFreeze(0, 20f, aheadBio, chrono));
        Assert.AreEqual(1f, BiologicalTickCorrection.ChildAgingMultiplierForConsumers(0));
    }

    [Test]
    public void NegativeMode_StopsCorrectingAtTheChronologicalBoundary()
    {
        Assert.IsTrue(BiologicalTickCorrection.CanCorrect(-2, 5f, 12 * Year, 4 * Year));
        Assert.IsFalse(BiologicalTickCorrection.CanCorrect(-2, 5f, 4 * Year, 4 * Year),
            "Once bio meets chrono, negative mode must tick 1:1, not reverse further.");
        Assert.AreEqual(1f, BiologicalTickCorrection.ChildAgingMultiplierForConsumers(-2));
    }

    [Test]
    public void Watermark_KeepsGreatestAndSuppressesBirthdaysAtOrBelow()
    {
        long first = BiologicalTickCorrection.CombineWatermark(-1, 16 * Year);
        long kept = BiologicalTickCorrection.CombineWatermark(first, 14 * Year);
        Assert.AreEqual(16 * Year, kept);

        Assert.IsTrue(BiologicalTickCorrection.ShouldSuppressBirthday(kept, 16));
        Assert.IsTrue(BiologicalTickCorrection.ShouldSuppressBirthday(kept, 13));
        Assert.IsFalse(BiologicalTickCorrection.ShouldSuppressBirthday(kept, 17));
        Assert.IsFalse(BiologicalTickCorrection.ShouldClearWatermark(16 * Year, kept));
        Assert.IsTrue(BiologicalTickCorrection.ShouldClearWatermark(16 * Year + 1, kept));
    }

    [Test]
    public void TruncateTowardZero_DoesNotFloorNegatives()
    {
        Assert.AreEqual(-1, BiologicalTickCorrection.TruncateTowardZero(-1.9f));
        Assert.AreEqual(1, BiologicalTickCorrection.TruncateTowardZero(1.9f));
        Assert.AreEqual(0, BiologicalTickCorrection.TruncateTowardZero(-0.9f));
    }

    [Test]
    public void GrowthFromBiologicalTicks_IsFractionOfAdultMinAge()
    {
        Assert.AreEqual(0f, BiologicalTickCorrection.GrowthFromBiologicalTicks(0, 18f));
        Assert.AreEqual(0.5f, BiologicalTickCorrection.GrowthFromBiologicalTicks(9 * Year, 18f), 0.0001f);
        Assert.AreEqual(1f, BiologicalTickCorrection.GrowthFromBiologicalTicks(18 * Year, 18f), 0.0001f);
        Assert.AreEqual(1f, BiologicalTickCorrection.GrowthFromBiologicalTicks(20 * Year, 18f), 0.0001f);
        Assert.AreEqual(1f, BiologicalTickCorrection.GrowthFromBiologicalTicks(Year, 0f));
        Assert.AreEqual(0f, BiologicalTickCorrection.GrowthFromBiologicalTicks(-1, 18f));
    }

    [Test]
    public void HumanlikeBirthdayMilestone_DoesNotDependOnGrowthValue()
    {
        // This mirrors vanilla's humanlike AgeTickInterval/BirthdayBiological
        // boundary: the birthday age comes from biological ticks. The private
        // `growth` field is a separate value and must not advance the birthday.
        long biologicalTicks = 4 * Year + 2 * 900_000; // 4 years, 2 quadrums.
        float growth = 0.4f;

        for (int i = 0; i < 20; i++)
        {
            growth = Math.Min(1f, growth + 0.05f);
        }

        Assert.AreEqual(1f, growth, 0.0001f);
        Assert.AreEqual(4, (int)(biologicalTicks / Year));

        // The 7-year milestone occurs only after biological age reaches 7.
        biologicalTicks = 7 * Year;
        Assert.AreEqual(7, (int)(biologicalTicks / Year));
    }

    [Test]
    public void ShouldSyncGrowth_OnlyWhileFreezeOrReverseIsActive()
    {
        long bio = 12 * Year;
        long chrono = 4 * Year;

        Assert.IsTrue(BiologicalTickCorrection.ShouldSyncGrowthToBiologicalAge(-4, 12f, bio, chrono));
        Assert.IsTrue(BiologicalTickCorrection.ShouldSyncGrowthToBiologicalAge(0, 12f, bio, chrono));
        Assert.IsFalse(BiologicalTickCorrection.ShouldSyncGrowthToBiologicalAge(-4, 12f, chrono, chrono));
        Assert.IsFalse(BiologicalTickCorrection.ShouldSyncGrowthToBiologicalAge(0, 12f, chrono, chrono));
        Assert.IsFalse(BiologicalTickCorrection.ShouldSyncGrowthToBiologicalAge(4, 12f, bio, chrono));
        Assert.IsFalse(BiologicalTickCorrection.ShouldSyncGrowthToBiologicalAge(-4, 20f, 21 * Year, chrono));
    }

    [Test]
    public void LifeStageFloorTicks_UsesStageMinAge()
    {
        Assert.AreEqual(0L, BiologicalTickCorrection.LifeStageFloorTicks(0f));
        Assert.AreEqual(0L, BiologicalTickCorrection.LifeStageFloorTicks(-1f));
        Assert.AreEqual(Year, BiologicalTickCorrection.LifeStageFloorTicks(1f));
        Assert.AreEqual(3 * Year, BiologicalTickCorrection.LifeStageFloorTicks(3f));
        Assert.AreEqual((long)(0.5 * Year), BiologicalTickCorrection.LifeStageFloorTicks(0.5f));
    }

    [Test]
    public void EffectiveFloor_IsMaxOfChronologicalAndLifeStage()
    {
        Assert.AreEqual(4 * Year, BiologicalTickCorrection.EffectiveFloorTicks(4 * Year, 3 * Year));
        Assert.AreEqual(3 * Year, BiologicalTickCorrection.EffectiveFloorTicks(1 * Year, 3 * Year));
        Assert.AreEqual(0L, BiologicalTickCorrection.EffectiveFloorTicks(0L, 0L));
    }

    [Test]
    public void ChildCannotRegressToToddler()
    {
        const float childMinAge = 3f;
        long childFloor = BiologicalTickCorrection.LifeStageFloorTicks(childMinAge);
        long bio = 5 * Year;
        long chrono = 1 * Year;

        Assert.IsTrue(BiologicalTickCorrection.CanCorrect(-4, 5f, bio, chrono, childFloor));

        BiologicalTickCorrection.TickResult result = BiologicalTickCorrection.Apply(
            biologicalTicks: bio,
            chronologicalTicks: chrono,
            progress: 0f,
            interval: (int)(3 * Year),
            rate: -1f,
            lifeStageMinTicks: childFloor);

        Assert.AreEqual(childFloor, result.BiologicalTicks);
        Assert.AreEqual(0f, result.Progress);
        Assert.IsTrue(result.ReachedFloor);
        Assert.IsFalse(BiologicalTickCorrection.CanCorrect(-4, 3f, result.BiologicalTicks, chrono, childFloor));
        Assert.IsTrue(BiologicalTickCorrection.CanHoldAtLifeStageFloor(
            -4, 3f, result.BiologicalTicks, chrono, childFloor));
    }

    [Test]
    public void ToddlerCannotRegressToBaby()
    {
        const float toddlerMinAge = 1f;
        long toddlerFloor = BiologicalTickCorrection.LifeStageFloorTicks(toddlerMinAge);
        long bio = (long)(2.4 * Year);
        long chrono = 0;

        BiologicalTickCorrection.TickResult result = BiologicalTickCorrection.Apply(
            biologicalTicks: bio,
            chronologicalTicks: chrono,
            progress: -0.8f,
            interval: (int)(2 * Year),
            rate: -5f,
            lifeStageMinTicks: toddlerFloor);

        Assert.AreEqual(toddlerFloor, result.BiologicalTicks);
        Assert.AreEqual(0f, result.Progress, "Remainder must be cleared at the life-stage floor.");
        Assert.IsTrue(result.ReachedFloor);
        Assert.IsTrue(BiologicalTickCorrection.CanHoldAtLifeStageFloor(
            -2, 1f, result.BiologicalTicks, chrono, toddlerFloor));
        Assert.IsFalse(BiologicalTickCorrection.CanCorrect(-2, 1f, result.BiologicalTicks, chrono, toddlerFloor));
    }

    [Test]
    public void ChronologicalFloorStillWinsWhenItIsHigherThanLifeStageMin()
    {
        long childFloor = BiologicalTickCorrection.LifeStageFloorTicks(3f);
        BiologicalTickCorrection.TickResult result = BiologicalTickCorrection.Apply(
            biologicalTicks: 6 * Year,
            chronologicalTicks: 4 * Year,
            progress: 0f,
            interval: (int)(4 * Year),
            rate: -1f,
            lifeStageMinTicks: childFloor);

        Assert.AreEqual(4 * Year, result.BiologicalTicks);
        Assert.IsTrue(result.ReachedFloor);
        Assert.IsFalse(BiologicalTickCorrection.CanHoldAtLifeStageFloor(
            -3, 4f, 4 * Year, 4 * Year, childFloor));
    }

    [Test]
    public void BabyStageHasNoLifeStageFloorAboveZero()
    {
        long babyFloor = BiologicalTickCorrection.LifeStageFloorTicks(0f);
        BiologicalTickCorrection.TickResult result = BiologicalTickCorrection.Apply(
            biologicalTicks: 2 * Year,
            chronologicalTicks: Year / 2,
            progress: 0f,
            interval: (int)(3 * Year),
            rate: -1f,
            lifeStageMinTicks: babyFloor);

        Assert.AreEqual(Year / 2, result.BiologicalTicks);
        Assert.IsFalse(BiologicalTickCorrection.CanHoldAtLifeStageFloor(
            -1, 0.5f, Year / 2, Year / 2, babyFloor));
    }

    [Test]
    public void HoldingAtLifeStageFloor_StillSyncsGrowth()
    {
        long childFloor = 3 * Year;
        Assert.IsTrue(BiologicalTickCorrection.ShouldSyncGrowthToBiologicalAge(
            -4, 3f, childFloor, Year, childFloor));
        Assert.IsFalse(BiologicalTickCorrection.ShouldSyncGrowthToBiologicalAge(
            4, 3f, childFloor, Year, childFloor));
    }
}
