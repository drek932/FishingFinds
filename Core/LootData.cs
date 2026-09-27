using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using MelonLoader.Utils;

namespace FishingFinds
{
    /// <summary>Rarity of an item inside its category. Off = never drops.</summary>
    public enum Tier
    {
        Off,
        Frequent,
        Common,
        Rare,
        VeryRare,
        Exceptional,
    }

    /// <summary>Where an item comes from; decides which settings switch it on or off.</summary>
    public enum Origin
    {
        /// <summary>Part of the Beachcombing loot, always on (unless changed in the extreme settings).</summary>
        Beachcombing,

        /// <summary>Not from Beachcombing, added by this mod. On by default, can be switched off.</summary>
        Added,

        /// <summary>Beachcombing loot removed by this mod. Off by default, can be switched on.</summary>
        Removed,
    }

    public sealed class CategoryData
    {
        public string Id { get; set; }
        public string NameEn { get; set; }
        public string NameRu { get; set; }

        /// <summary>Relative chance to pick this category (the defaults add up to 100).</summary>
        public int Share { get; set; }
    }

    public sealed class ItemData
    {
        /// <summary>Stable key for settings: the first prefab name.</summary>
        public string Id { get; set; }

        public string NameEn { get; set; }
        public string NameRu { get; set; }
        public string Category { get; set; }
        public Tier Tier { get; set; }
        public Origin Origin { get; set; }
        public bool Dlc { get; set; }

        /// <summary>
        /// The game disables this item on Interloper/Misery (its prefab has DisableObjectForXPMode with those modes).
        /// Used by the "Like Interloper/Misery" setting. Source: docs/data/xp-report.txt.
        /// </summary>
        public bool HighDifficulty { get; set; }

        /// <summary>Prefab names; with several, one is picked at random (e.g. books).</summary>
        public string[] Gear { get; set; }
    }

    /// <summary>
    /// The loot table: Mods/FishingFinds_Loot.json. Holds defaults only; player choices live in Config.
    /// Written from DefaultTable on first start, and again (old file kept as .bak) when the format version changes.
    /// </summary>
    internal sealed class LootData
    {
        public const int CurrentVersion = 5;

        public int Version { get; set; }
        public List<CategoryData> Categories { get; set; } = new();
        public List<ItemData> Items { get; set; } = new();

        public static LootData Current { get; private set; }

        /// <summary>The table was replaced by a newer format this run (saved shares/tiers may refer to old defaults).</summary>
        public static bool Upgraded { get; private set; }

        public static string FilePath => Path.Combine(MelonEnvironment.ModsDirectory, "FishingFinds_Loot.json");

        internal static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new JsonStringEnumConverter() },
        };

        public static void Load()
        {
            LootData data = null;
            if (File.Exists(FilePath))
            {
                try
                {
                    data = JsonSerializer.Deserialize<LootData>(File.ReadAllText(FilePath), JsonOptions);
                }
                catch (Exception e)
                {
                    Main.Log.Error($"Could not read {Path.GetFileName(FilePath)}: {e.Message}");
                }

                if (data == null || data.Version != CurrentVersion)
                {
                    Backup();
                    data = null;
                    Upgraded = true;
                }
            }

            if (data == null)
            {
                data = Defaults();
                Save(data);
                Main.Log.Msg($"Wrote default loot table to {Path.GetFileName(FilePath)}");
            }

            data.Validate();
            Current = data;
        }

        private static LootData Defaults() => new()
        {
            Version = CurrentVersion,
            Categories = DefaultTable.Categories(),
            Items = DefaultTable.Items(),
        };

        private static void Save(LootData data)
        {
            try
            {
                File.WriteAllText(FilePath, JsonSerializer.Serialize(data, JsonOptions));
            }
            catch (Exception e)
            {
                Main.Log.Error($"Could not write {Path.GetFileName(FilePath)}: {e.Message}");
            }
        }

        private static void Backup()
        {
            var bak = FilePath + ".bak";
            if (File.Exists(bak)) bak = FilePath + "." + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".bak";
            try
            {
                File.Move(FilePath, bak);
                Main.Log.Msg($"Old loot table format, kept as {Path.GetFileName(bak)}");
            }
            catch (Exception e)
            {
                Main.Log.Error($"Could not back up the old loot table: {e.Message}");
            }
        }

        /// <summary>Drops broken entries so one typo in the json doesn't break the whole table.</summary>
        private void Validate()
        {
            Categories = Categories?.Where(c => c != null && !string.IsNullOrWhiteSpace(c.Id)).GroupBy(c => c.Id).Select(g => g.First()).ToList() ?? new();
            var categoryIds = new HashSet<string>(Categories.Select(c => c.Id));

            int before = Items?.Count ?? 0;
            Items = Items?
                .Where(i => i != null && !string.IsNullOrWhiteSpace(i.Id) && i.Gear != null && i.Gear.Any(g => !string.IsNullOrWhiteSpace(g)) && categoryIds.Contains(i.Category))
                .GroupBy(i => i.Id).Select(g => g.First())
                .ToList() ?? new();
            foreach (var item in Items)
                item.Gear = item.Gear.Where(g => !string.IsNullOrWhiteSpace(g)).ToArray();

            if (Items.Count != before)
                Main.Log.Warning($"Loot table: skipped {before - Items.Count} invalid or duplicate items");

            Main.Log.Msg($"Loot table: {Categories.Count} categories, {Items.Count} items " +
                         $"({Items.Count(i => i.Origin == Origin.Beachcombing)} Beachcombing, {Items.Count(i => i.Origin == Origin.Added)} added, {Items.Count(i => i.Origin == Origin.Removed)} removed)");
        }

        public CategoryData Category(string id) => Categories.FirstOrDefault(c => c.Id == id);

        public IEnumerable<ItemData> ItemsIn(string categoryId) => Items.Where(i => i.Category == categoryId);
    }
}
