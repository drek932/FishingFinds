using System;
using HarmonyLib;
using Il2Cpp;

namespace FishingFinds
{
    /// <summary>
    /// The game creates the caught fish in IceFishingHole.InstantiateFish; returning another GearItem
    /// makes the player pull that item out of the hole instead.
    ///
    /// Two kinds of catch:
    ///  - targeted: rod without bait or lure — full chance with the skill bonus, every rarity;
    ///  - casual: rod with bait/lure (if enabled) or any tip-up (if enabled) — reduced chance, no skill bonus;
    ///    tip-ups also have a rarity limit. Tip-ups don't check bait (the game uses it up when the tip-up is set).
    /// </summary>
    [HarmonyPatch(typeof(IceFishingHole), nameof(IceFishingHole.InstantiateFish))]
    internal static class IceFishingHole_InstantiateFish
    {
        // false = the catch was replaced, the original (fish creation) is skipped.
        private static bool Prefix(IceFishingHole __instance, ref GearItem __result)
        {
            try
            {
                var config = Config.Current;
                if (!config.Enabled || __instance == null || LootData.Current == null)
                    return true;

                bool tipUp = __instance.m_State == FishingState.PassiveFishing;
                bool hasTackle = !tipUp && (__instance.m_SelectedBait != null || __instance.m_SelectedLure != null);

                if ((tipUp && !config.AllowTipUps) || (hasTackle && !config.AllowBaitCatches))
                {
                    Main.Log.Msg($"Catch: fish ({(tipUp ? "tip-up" : "rod with bait/lure")}, turned off in settings)");
                    return true;
                }

                bool casual = tipUp || hasTackle;
                int level = LootRoller.FishingLevel();
                string mode = tipUp ? $"tip-up, casual, max tier {LootRoller.TierLimit(level, true)}"
                    : hasTackle ? $"rod with bait/lure, casual, level {level}"
                    : $"rod, level {level}";

                if (!LootRoller.RollChance(level, casual, out float chance, out float roll))
                {
                    Main.Log.Msg($"Catch: fish ({mode}, roll {roll:0.0} >= {chance:0.#}%)");
                    return true;
                }

                var item = LootRoller.CreateItem(level, tipUp, out var details, out bool blocked);
                if (blocked)
                {
                    Main.Log.Msg($"Catch: fish (hard mode: {details}; {mode})");
                    return true;
                }
                if (item == null)
                {
                    Main.Log.Warning($"Catch: fish, no item could be created ({details})");
                    return true;
                }

                Main.Log.Msg($"Catch: {details} ({mode}, roll {roll:0.0} < {chance:0.#}%)");
                __result = item;
                return false;
            }
            catch (Exception e)
            {
                Main.Log.Error($"InstantiateFish: {e}");
                return true;
            }
        }
    }
}
