using System;
using HarmonyLib;
using Il2Cpp;

namespace FishingFinds
{
    /// <summary>
    /// Adds "Chance to catch valuables: X%" to the ice fishing skill benefits, one value per skill level.
    /// </summary>
    [HarmonyPatch(typeof(Skill_IceFishing), nameof(Skill_IceFishing.GetTierBenefits))]
    internal static class Skill_IceFishing_GetTierBenefits
    {
        private static bool loggedOnce;

        // index = skill tier, 0-based (tier 0 = level 1).
        private static void Postfix(int index, ref string __result)
        {
            try
            {
                if (!Config.Current.Enabled) return;
                int level = Math.Clamp(index + 1, 1, 5);
                // Only the chance without bait: it's the one that grows with the skill.
                var line = Loc.T("SKILL_CHANCE", Config.Current.ChanceAt(level));
                __result = string.IsNullOrEmpty(__result) ? line : __result + "\n" + line;

                if (!loggedOnce)
                {
                    loggedOnce = true;
                    Main.Log.Msg($"Skill benefits text extended (first call: tier index {index})");
                }
            }
            catch (Exception e)
            {
                Main.Log.Error($"GetTierBenefits: {e}");
            }
        }
    }
}
