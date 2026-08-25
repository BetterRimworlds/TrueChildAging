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
    public const int MinChildAgingFactor = -5;
    public const int MaxChildAgingFactor = 5;
    public const int DefaultChildAgingFactor = 4;

    public int ChildAgingFactor = DefaultChildAgingFactor;
    public bool NineMonthPregnancies = false;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref ChildAgingFactor, "ChildAgingFactor", DefaultChildAgingFactor);
        Scribe_Values.Look(ref NineMonthPregnancies, "NineMonthPregnancies", false);
        ChildAgingFactor = Mathf.Clamp(ChildAgingFactor, MinChildAgingFactor, MaxChildAgingFactor);
    }

    public void DoSettingsWindowContents(Rect inRect)
    {
        Listing_Standard listing = new Listing_Standard();
        listing.Begin(inRect);

        Text.Font = GameFont.Medium;
        Text.Font = GameFont.Small;
        listing.Gap(10f);

        // Value row, e.g. "Child Aging Factor: 4x".
        Rect valueRow = listing.GetRect(26f);
        Widgets.DrawHighlightIfMouseover(valueRow);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(valueRow, "Child Aging Factor: " + ChildAgingFactor.ToString("0x;-0x;0x"));
        Text.Anchor = TextAnchor.UpperLeft;
        TooltipHandler.TipRegion(valueRow, Tooltip);

        // Integer-snapping slider from -5x to 5x.
        ChildAgingFactor = Mathf.RoundToInt(listing.Slider(ChildAgingFactor, MinChildAgingFactor, MaxChildAgingFactor));

        listing.Gap(4f);
        listing.Label("Positive values (1x to 5x): children age at this speed. RimWorld's default is 4x. From 11 to 20 they gradually slow to the adult rate.");
        listing.Gap(4f);
        listing.Label("0x freezes biological aging only while it is ahead of chronological age. When they meet, both tick 1:1.");
        listing.Gap(4f);
        listing.Label("Negative values (-5x to -1x) reverse biological age toward chronological age at that rate. When they meet, both tick 1:1. Reverse aging never drops a pawn below their current life stage: a child cannot become a toddler, and a toddler cannot become a baby.");
        listing.Gap(4f);
        listing.Label("Correction never applies to biologically adult pawns (aged 20 or above), and growth vats always retain vanilla behavior.");
        listing.Label("During correction, growth-point learning continues at a positive 1x rate.");
        listing.Label("Changes apply immediately; no restart required.");

        listing.GapLine(12f);
        if (listing.ButtonTextLabeled(
                "9 Month Pregnancies",
                NineMonthPregnancies ? "On" : "Off",
                TextAnchor.MiddleLeft,
                null,
                NineMonthTooltip))
        {
            NineMonthPregnancies = !NineMonthPregnancies;
        }

        listing.Gap(4f);
        listing.Label("On: human pregnancies last 45 days (9 RimWorld months) plus or minus 6 days. Off: vanilla 18-day human pregnancies. Each pregnancy rolls its own duration. The current setting is applied immediately, to all future pregnancies.");

        listing.End();
    }

    private const string Tooltip =
        "Child Aging Factor\n" +
        "Controls the rate at which biological children age.\n\n" +
        "1x to 5x: Replaces RimWorld's child-aging factor (default 4x).\n" +
        "0x: Freezes biological aging while it is ahead of chronological age; when they meet, both tick 1:1.\n" +
        "-1x to -5x: Reverses biological aging toward chronological age; when they meet, both tick 1:1. Never below chronological age or the current life stage (a child cannot regress to toddler or baby).\n\n" +
        "Changes apply immediately; no restart required.";

    private const string NineMonthTooltip =
        "9 Month Pregnancies\n" +
        "On: extend human pregnancies to 45 days (9 RimWorld months) plus or minus 6 days.\n" +
        "Off: vanilla 18-day human pregnancies.\n\n" +
        "Each pregnancy rolls its own duration. The current setting is applied immediately; no restart required.";
}
