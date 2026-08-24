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

using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace BetterRimworlds.TrueChildAging;

public class TrueChildAging : Mod
{
    public const string HarmonyId = "HopeSeekr.BetterRimworlds.TrueChildAging";

    public static Settings settings;

    public TrueChildAging(ModContentPack content) : base(content)
    {
        settings = GetSettings<Settings>();

        Harmony harmony = new Harmony(HarmonyId);
        foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
        {
            try
            {
                if (!type.IsDefined(typeof(HarmonyPatch), inherit: true))
                {
                    continue;
                }

                harmony.CreateClassProcessor(type).Patch();
            }
            catch (Exception ex)
            {
                Log.Error($"[True Child Aging] Harmony patch failed on {type.FullName}: {ex}");
            }
        }
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
