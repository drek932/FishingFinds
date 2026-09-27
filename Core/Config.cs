using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MelonLoader.Utils;

namespace FishingFinds
{
    /// <summary>
    /// Player settings: Mods/FishingFinds.json. Also answers "what is in effect right now" (the effective values below).
    ///
    /// Three levels: general settings are always used; deep settings replace some of them while Deep is on;
    /// extreme settings (only with Deep) replace the added/removed switches with a tier for every item.
    /// Values of a switched-off level are kept, just not used.
    /// Per-item and per-category values are stored only when they differ from the defaults.
    /// </summary>
    internal sealed class Config
    {
        public const int DefaultChance = 25;
        public const int DefaultBonusPerLevel = 5;
        public const int BasicExceptionalLevel = 3;

        // ---------- basic ----------

        public bool Enabled { get; set; } = true;

        /// <summary>Basic choice: exceptional items only from ice fishing level 3 (false = always).</summary>
        public bool ExceptionalFromLevel3 { get; set; } = true;

        /// <summary>Tip-ups can also bring up items (casual catch, with a rarity limit).</summary>
        public bool AllowTipUps { get; set; } = true;

        /// <summary>Rod fishing with bait or a lure can also bring up items (casual catch).</summary>
        public bool AllowBaitCatches { get; set; } = true;

        /// <summary>Items the game disables on Interloper/Misery never drop: such a catch stays a fish.</summary>
        public bool HardModeLoot { get; set; } = false;

        public bool DisableAllAdded { get; set; } = false;

        /// <summary>Switches for added and removed items, by item id. Missing = default (added on, removed off).</summary>
        public Dictionary<string, bool> ItemOn { get; set; } = new();

        // ---------- deep ----------

        public bool Deep { get; set; } = false;
        public int ChancePercent { get; set; } = DefaultChance;
        public int BonusPerLevel { get; set; } = DefaultBonusPerLevel;
        public int ExceptionalMinLevel { get; set; } = BasicExceptionalLevel;

        /// <summary>Casual catch (tip-ups, rod with bait/lure): percentage of the base chance, no per-level bonus.</summary>
        public int CasualChancePercent { get; set; } = DefaultCasualChancePercent;

        /// <summary>Rarest tier a tip-up can bring up.</summary>
        public Tier TipUpMaxTier { get; set; } = DefaultTipUpMaxTier;

        /// <summary>Category shares by category id. Missing = default from the loot table.</summary>
        public Dictionary<string, int> CategoryShares { get; set; } = new();

        // ---------- extreme ----------

        public bool Extreme { get; set; } = false;

        /// <summary>Tier of every item by item id. Missing = default.</summary>
        public Dictionary<string, Tier> ItemTiers { get; set; } = new();

        // ---------- effective values ----------

        public bool ExtremeActive => Deep && Extreme;

        public int BaseChance => Deep ? ChancePercent : DefaultChance;
        public int Bonus => Deep ? BonusPerLevel : DefaultBonusPerLevel;

        public int ChanceAt(int fishingLevel) => Math.Clamp(BaseChance + Bonus * (fishingLevel - 1), 0, 100);

        public const int DefaultCasualChancePercent = 50;
        public const Tier DefaultTipUpMaxTier = Tier.Rare;

        /// <summary>Casual catch (tip-ups, rod with bait/lure): a share of the base chance, the same at every skill level.</summary>
        public float CasualChance => Math.Clamp(BaseChance * (Deep ? CasualChancePercent : DefaultCasualChancePercent) / 100f, 0f, 100f);

        public Tier TipUpTierLimit => Deep ? TipUpMaxTier : DefaultTipUpMaxTier;

        public int ExceptionalLevel => Deep ? Math.Clamp(ExceptionalMinLevel, 1, 5) : (ExceptionalFromLevel3 ? BasicExceptionalLevel : 1);

        public int Share(CategoryData c) => Deep && CategoryShares.TryGetValue(c.Id, out var s) ? Math.Max(0, s) : c.Share;

        public bool IsOn(ItemData item) => ItemOn.TryGetValue(item.Id, out var on) ? on : item.Origin != Origin.Removed;

        /// <summary>Tier from the basic switches only.</summary>
        public Tier BasicTier(ItemData item) => item.Origin switch
        {
            Origin.Added => !DisableAllAdded && IsOn(item) ? item.Tier : Tier.Off,
            Origin.Removed => IsOn(item) ? item.Tier : Tier.Off,
            _ => item.Tier,
        };

        /// <summary>Default of the extreme list: removed items off, everything else at its table tier.</summary>
        public static Tier DefaultExtremeTier(ItemData item) => item.Origin == Origin.Removed ? Tier.Off : item.Tier;

        public Tier ExtremeTier(ItemData item) => ItemTiers.TryGetValue(item.Id, out var t) ? t : DefaultExtremeTier(item);

        public Tier EffectiveTier(ItemData item) => ExtremeActive ? ExtremeTier(item) : BasicTier(item);

        // ---------- load / save ----------

        public static Config Current { get; private set; } = new();

        public static string FilePath => Path.Combine(MelonEnvironment.ModsDirectory, "FishingFinds.json");

        public static void Load()
        {
            if (!File.Exists(FilePath)) return;
            try
            {
                Current = JsonSerializer.Deserialize<Config>(File.ReadAllText(FilePath), LootData.JsonOptions) ?? new Config();
                Current.ItemOn ??= new();
                Current.CategoryShares ??= new();
                Current.ItemTiers ??= new();

                // New table format: categories and default tiers may have changed, so values saved for the old ones are dropped.
                if (LootData.Upgraded && (Current.CategoryShares.Count > 0 || Current.ItemTiers.Count > 0))
                {
                    Main.Log.Msg($"Loot table updated: reset {Current.CategoryShares.Count} saved category share(s) and {Current.ItemTiers.Count} saved tier(s) to the new defaults");
                    Current.CategoryShares.Clear();
                    Current.ItemTiers.Clear();
                    Save();
                }
            }
            catch (Exception e)
            {
                Main.Log.Error($"Could not read {Path.GetFileName(FilePath)}, using defaults: {e.Message}");
                Current = new Config();
            }
        }

        public static void Save()
        {
            try
            {
                File.WriteAllText(FilePath, JsonSerializer.Serialize(Current, LootData.JsonOptions));
            }
            catch (Exception e)
            {
                Main.Log.Error($"Could not write {Path.GetFileName(FilePath)}: {e.Message}");
            }
        }

        public string DescribeShares(LootData data)
        {
            int total = data.Categories.Sum(Share);
            return string.Join(", ", data.Categories.Select(c => $"{c.Id} {Share(c)}" + (Share(c) != c.Share ? $" (default {c.Share})" : ""))) +
                   $" | total {total}" + (Deep ? "" : " (defaults, deep off)");
        }

        public string Describe(LootData data) =>
            $"enabled={Enabled}, hardMode={HardModeLoot}, casual chance {CasualChance:0.#}% (baitCatches={AllowBaitCatches}, tipUps={AllowTipUps} up to {TipUpTierLimit}), chance={BaseChance}%+{Bonus}%/level, exceptional from level {ExceptionalLevel}, " +
            $"deep={Deep}, extreme={ExtremeActive}, items on={data.Items.Count(i => EffectiveTier(i) != Tier.Off)}/{data.Items.Count}";
    }
}
