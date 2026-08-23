/*
 * This file is part of True Child Aging, a Better Rimworlds Project.
 *
 * Copyright © 2024-2025 Theodore R. Smith
 * Author: Theodore R. Smith <hopeseekr@gmail.com>
 *   GPG Fingerprint: D8EA 6E4D 5952 159D 7759  2BB4 EEB6 CE72 F441 EC41
 *   https://github.com/BetterRimworlds/TrueChildAging
 *
 * This file is licensed under the MIT License.
 */

using UnityEngine;
using Verse;

namespace BetterRimworlds.TrueChildAging;

public class TrueChildAging : Mod
{
    public static Settings settings;

    public TrueChildAging(ModContentPack content) : base(content)
    {
        settings = GetSettings<Settings>();

        // TODO: Implement per PLAN.md:
        //   - Harmony patches to Pawn_AgeTracker.ChildAgingMultiplier / BiologicalTicksPerTick
        //   - Save-persistent watermark GameComponent for negative (de-aging) mode
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        settings.DoSettingsWindowContents(inRect);
    }

    public override string SettingsCategory()
    {
        return "True Child Aging";
    }
}
