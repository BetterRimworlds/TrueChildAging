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

public class Settings : ModSettings
{
    public float powerEfficiency = 0.25f;
    public bool showDebugMessages = false;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref powerEfficiency,   "brw.thermovoltaic.powerEfficiency", 0.25f);
        Scribe_Values.Look(ref showDebugMessages, "brw.thermovoltaic.showDebugMessages", false);
    }

    public void DoSettingsWindowContents(Rect inRect)
    {
        Listing_Standard listing_Standard = new Listing_Standard();
        listing_Standard.Begin(inRect);

        string[] labels =
        {
            "Power generating efficiency:",
            "Show Debug Messages",
        };

        string buffer = null;
        listing_Standard.TextFieldNumericLabeled<float>(labels[0], ref powerEfficiency, ref buffer);
        listing_Standard.CheckboxLabeled(labels[0], ref showDebugMessages);

        listing_Standard.End();
    }
}
