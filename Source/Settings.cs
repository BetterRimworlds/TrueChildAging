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

    public override void ExposeData()
    {
        Scribe_Values.Look(ref ChildAgingFactor, "ChildAgingFactor", DefaultChildAgingFactor);
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

        listing.End();
    }

    private const string Tooltip =
        "Child Aging Factor\n" +
        "Controls the rate at which biological children age.\n\n" +
        "1x to 5x: Replaces RimWorld's child-aging factor (default 4x).\n" +
        "0x: Freezes biological aging while it is ahead of chronological age; when they meet, both tick 1:1.\n" +
        "-1x to -5x: Reverses biological aging toward chronological age; when they meet, both tick 1:1. Never below chronological age or the current life stage (a child cannot regress to toddler or baby).\n\n" +
        "Changes apply immediately; no restart required.";
}
