using System;
using System.Collections.Generic;
using System.Linq;
using Il2Cpp;

namespace FishingFinds
{
    /// <summary>
    /// Decides whether a catch becomes an item and which one: chance -> category by share -> item by tier weight.
    ///
    /// Chance: targeted catch (rod, no bait/lure) = base + bonus per ice fishing level;
    /// casual catch (rod with bait/lure, tip-ups) = a share of the base chance, no level bonus.
    /// Rarity limits: exceptional items need the set ice fishing level; tip-ups have their own limit.
    /// </summary>
    internal static class LootRoller
    {
        private const int MaxAttempts = 5;

        private static readonly Random Rng = new();

        public static float Weight(Tier tier) => tier switch
        {
            Tier.Frequent => 10f,
            Tier.Common => 5f,
            Tier.Rare => 2f,
            Tier.VeryRare => 0.5f,
            Tier.Exceptional => 0.1f,
            _ => 0f,
        };

        /// <summary>Ice fishing skill level 1..5 (the game counts tiers from 0).</summary>
        public static int FishingLevel()
        {
            try
            {
                var skill = GameManager.GetSkillsManager()?.GetSkill(SkillType.IceFishing);
                return skill != null ? Math.Clamp(skill.GetCurrentTierNumber() + 1, 1, 5) : 1;
            }
            catch (Exception e)
            {
                Main.Log.Warning($"Could not read the ice fishing skill, using level 1: {e.Message}");
                return 1;
            }
        }

        /// <summary>
        /// Rolls the chance. True = replace the fish with an item.
        /// Casual catch (tip-ups, rod with bait/lure): reduced chance, no per-level bonus.
        /// </summary>
        public static bool RollChance(int level, bool casual, out float chance, out float roll)
        {
            chance = casual ? Config.Current.CasualChance : Config.Current.ChanceAt(level);
            roll = (float)(Rng.NextDouble() * 100.0);
            return roll < chance;
        }

        /// <summary>Rarest tier allowed for this catch: exceptional needs the skill level, tip-ups have their own limit.</summary>
        public static Tier TierLimit(int level, bool tipUp)
        {
            var config = Config.Current;
            var limit = level >= config.ExceptionalLevel ? Tier.Exceptional : Tier.VeryRare;
            if (tipUp && config.TipUpTierLimit < limit) limit = config.TipUpTierLimit;
            return limit;
        }

        /// <summary>
        /// Picks and creates an item. Null = the player gets the fish: nothing could be created,
        /// or (blocked = true) the drawn item is one the game disables on Interloper/Misery and that setting is on.
        /// </summary>
        public static GearItem CreateItem(int level, bool tipUp, out string log, out bool blocked)
        {
            var config = Config.Current;
            var data = LootData.Current;
            var maxTier = TierLimit(level, tipUp);
            var failed = new HashSet<string>();
            var notes = new List<string>();

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var item = Pick(config, data, maxTier, failed, out var category, out blocked);
                if (blocked)
                {
                    log = $"{item.NameEn} is not available on Interloper/Misery";
                    return null;
                }
                if (item == null)
                {
                    notes.Add("nothing to draw");
                    break;
                }

                var tier = config.EffectiveTier(item);
                var prefab = item.Gear[Rng.Next(item.Gear.Length)];
                var gear = GearItem.InstantiateGearItem(prefab);
                if (gear == null)
                {
                    notes.Add($"{prefab} not available");
                    failed.Add(item.Id);
                    continue;
                }

                if (prefab == "GEAR_JerrycanRusty") FillJerryCan(gear);

                log = $"{item.NameEn} [{prefab}] from {category.Id}, tier {tier}" + (notes.Count > 0 ? $" ({string.Join("; ", notes)})" : "");
                return gear;
            }

            log = string.Join("; ", notes);
            blocked = false;
            return null;
        }

        /// <summary>
        /// Draws an item; with "Like Interloper/Misery" on, an item the game disables there is not replaced
        /// by another one — the catch stays a fish, so all other rates stay as they are and there is simply less loot.
        /// </summary>
        private static ItemData Pick(Config config, LootData data, Tier maxTier, HashSet<string> failed, out CategoryData category, out bool blocked)
        {
            var item = Draw(config, data, maxTier, failed, out category);
            blocked = item != null && config.HardModeLoot && item.HighDifficulty;
            return item;
        }

        // maxTier: items rarer than this are skipped (the enum goes from common to rare).
        private static ItemData Draw(Config config, LootData data, Tier maxTier, HashSet<string> failed, out CategoryData category)
        {
            float ItemWeight(ItemData i)
            {
                if (failed.Contains(i.Id)) return 0f;
                var tier = config.EffectiveTier(i);
                if (tier > maxTier) return 0f;
                return Weight(tier);
            }

            // Only categories that still have something to give.
            var categories = data.Categories
                .Select(c => (c, share: (float)config.Share(c), items: data.ItemsIn(c.Id).Select(i => (i, w: ItemWeight(i))).Where(x => x.w > 0f).ToList()))
                .Where(x => x.share > 0f && x.items.Count > 0)
                .ToList();

            category = null;
            if (categories.Count == 0) return null;

            var picked = WeightedPick(categories, x => x.share);
            category = picked.c;
            return WeightedPick(picked.items, x => x.w).i;
        }

        private static T WeightedPick<T>(List<T> list, Func<T, float> weight)
        {
            double roll = Rng.NextDouble() * list.Sum(x => (double)weight(x));
            foreach (var x in list)
            {
                roll -= weight(x);
                if (roll < 0) return x;
            }
            return list[list.Count - 1];
        }

        /// <summary>A jerry can comes out 10–50% full of kerosene.</summary>
        private static void FillJerryCan(GearItem gear)
        {
            try
            {
                var liquid = gear.m_LiquidItem;
                if (liquid == null)
                {
                    Main.Log.Warning("Jerry can has no LiquidItem, left as is");
                    return;
                }

                float fill = 0.1f + (float)Rng.NextDouble() * 0.4f;
                liquid.m_Liquid = liquid.m_LiquidCapacity * fill;
                Main.Log.Msg($"Jerry can filled to {fill:P0}: {liquid.m_Liquid.ToStringMetric()}");
            }
            catch (Exception e)
            {
                Main.Log.Error($"Could not fill the jerry can: {e}");
            }
        }
    }
}
