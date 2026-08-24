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
using Verse;

namespace BetterRimworlds.TrueChildAging;

/// Save-persistent record of each correcting pawn's highest previously reached
/// biological-age tick. Used to suppress duplicate birthdays and growth moments
/// while biological age is walked back toward chronological age.
public class GameComponent_ChildAgingWatermarks : GameComponent
{
    private Dictionary<Pawn, long> watermarks = new Dictionary<Pawn, long>();
    private List<Pawn> watermarkPawnWorkingList;
    private List<long> watermarkTickWorkingList;

    public GameComponent_ChildAgingWatermarks(Game game)
    {
    }

    public static GameComponent_ChildAgingWatermarks Get()
    {
        return Current.Game?.GetComponent<GameComponent_ChildAgingWatermarks>();
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(
            ref watermarks,
            "childAgingWatermarks",
            LookMode.Reference,
            LookMode.Value,
            ref watermarkPawnWorkingList,
            ref watermarkTickWorkingList);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            DiscardInvalidReferences();
        }
    }

    public void RecordWatermark(Pawn pawn, long biologicalTicks)
    {
        if (pawn == null)
        {
            return;
        }

        if (watermarks == null)
        {
            watermarks = new Dictionary<Pawn, long>();
        }

        long existing = watermarks.TryGetValue(pawn, out long stored) ? stored : -1L;
        watermarks[pawn] = BiologicalTickCorrection.CombineWatermark(existing, biologicalTicks);
    }

    public bool ShouldSuppressBirthday(Pawn pawn, int birthdayAge)
    {
        if (pawn == null || watermarks == null || !watermarks.TryGetValue(pawn, out long watermarkTicks))
        {
            return false;
        }

        return BiologicalTickCorrection.ShouldSuppressBirthday(watermarkTicks, birthdayAge);
    }

    public void NotifyBiologicalTicks(Pawn pawn, long biologicalTicks)
    {
        if (pawn == null || watermarks == null)
        {
            return;
        }

        if (!watermarks.TryGetValue(pawn, out long watermarkTicks))
        {
            return;
        }

        if (BiologicalTickCorrection.ShouldClearWatermark(biologicalTicks, watermarkTicks))
        {
            watermarks.Remove(pawn);
        }
    }

    private void DiscardInvalidReferences()
    {
        if (watermarks == null)
        {
            watermarks = new Dictionary<Pawn, long>();
            return;
        }

        List<Pawn> invalid = null;
        foreach (KeyValuePair<Pawn, long> entry in watermarks)
        {
            if (entry.Key == null || entry.Key.Destroyed)
            {
                invalid ??= new List<Pawn>();
                invalid.Add(entry.Key);
            }
        }

        if (invalid == null)
        {
            return;
        }

        Dictionary<Pawn, long> validWatermarks = new Dictionary<Pawn, long>();
        foreach (KeyValuePair<Pawn, long> entry in watermarks)
        {
            if (entry.Key != null && !entry.Key.Destroyed)
            {
                validWatermarks[entry.Key] = entry.Value;
            }
        }

        watermarks = validWatermarks;
    }
}
