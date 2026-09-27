using System;
using System.Collections.Generic;
using System.Linq;

namespace FishingFinds
{
    /// <summary>
    /// Text lookup: Russian if the game language is Russian, otherwise English (the texts are in Texts.cs).
    /// Keys "FISHFINDS_X" or "FISHFINDS_X|argument" are served through the Localization.Get patch, so ModSettings
    /// labels follow the game language. Some texts are built from the loot table (item and category names).
    /// </summary>
    internal static class Loc
    {
        public const string Prefix = "FISHFINDS_";

        /// <summary>Keys whose text is built from the loot table; the argument is an item or category id.</summary>
        private static readonly Dictionary<string, Func<string, string>> Dynamic = new()
        {
            ["ITEM_LABEL"] = id => "    " + ItemName(id),
            ["CHANCE_DESC"] = _ => T("CHANCE_DESC", ChanceNow()),
            ["BONUS_DESC"] = _ => T("BONUS_DESC", ChanceNow()),
            ["SHARE_LABEL"] = id => "    " + CategoryName(id),
            ["SHOW_CATEGORY"] = id => T("SHOW_CATEGORY", CategoryName(id), LootData.Current?.ItemsIn(id).Count() ?? 0),
            ["SHARE_DESC"] = id => T("SHARE_DESC", CategoryName(id), LootData.Current?.Category(id)?.Share ?? 0),
            ["ADDED_DESC"] = id => ItemDescription("ADDED_DESC", id),
            ["REMOVED_DESC"] = id => ItemDescription("REMOVED_DESC", id),
            ["TIER_DESC"] = id => Item(id) is { } i ? T("TIER_DESC", CategoryName(i.Category), TierName(Config.DefaultExtremeTier(i))) : id,
        };

        public static bool IsRussian =>
            CurrentGameLanguage() is { } lang && lang.IndexOf("russian", StringComparison.OrdinalIgnoreCase) >= 0;

        private static string CurrentGameLanguage()
        {
            try
            {
                var lang = Il2Cpp.Localization.s_Language;
                if (!string.IsNullOrEmpty(lang)) return lang;
            }
            catch
            {
                // ignored, try the property below
            }

            try
            {
                return Il2Cpp.Localization.Language;
            }
            catch
            {
                return null;
            }
        }

        public static string T(string key, params object[] args)
        {
            if (!Texts.All.TryGetValue(key, out var t)) return key;
            var text = IsRussian ? t.ru : t.en;
            return args.Length > 0 ? string.Format(text, args) : text;
        }

        public static string Key(string key, object arg = null) => Prefix + key + (arg != null ? "|" + arg : "");

        public static string TierName(Tier tier) => T("TIER_" + tier);

        /// <summary>"Now: level N → X%" with the values currently shown in the menu, or a note when no game is loaded.</summary>
        private static string ChanceNow()
        {
            try
            {
                if (Il2Cpp.GameManager.IsMainMenuActive()) return T("CHANCE_NOW_UNKNOWN");
                int level = LootRoller.FishingLevel();
                var (chance, bonus) = SettingsMenu.ShownChance();
                return T("CHANCE_NOW", level, Math.Clamp(chance + bonus * (level - 1), 0, 100));
            }
            catch
            {
                return T("CHANCE_NOW_UNKNOWN");
            }
        }

        private static ItemData Item(string id) => LootData.Current?.Items.FirstOrDefault(i => i.Id == id);

        private static string ItemName(string id) => Item(id) is { } i ? (IsRussian ? i.NameRu : i.NameEn) : id;

        private static string CategoryName(string id) =>
            LootData.Current?.Category(id) is { } c ? (IsRussian ? c.NameRu : c.NameEn) : id;

        private static string ItemDescription(string key, string id) =>
            Item(id) is { } i ? T(key, CategoryName(i.Category), TierName(i.Tier)) : id;

        // Every text handed out, with the key it came from: lets a cached text be rebuilt in another language.
        private static readonly Dictionary<string, string> KeyByText = new();

        /// <summary>Text for a "FISHFINDS_…" key; null if the key is not ours.</summary>
        public static string Resolve(string fullKey)
        {
            if (fullKey == null || !fullKey.StartsWith(Prefix, StringComparison.Ordinal)) return null;

            var body = fullKey.Substring(Prefix.Length);
            int bar = body.IndexOf('|');
            var key = bar < 0 ? body : body.Substring(0, bar);
            var arg = bar < 0 ? null : body.Substring(bar + 1);

            string text = Dynamic.TryGetValue(key, out var build) && arg != null ? build(arg)
                : arg != null ? T(key, arg)
                : T(key);

            if (KeyByText.Count < 5000) KeyByText[text] = fullKey;
            return text;
        }

        /// <summary>
        /// Brings a text of this mod to the current game language: a key, or a text handed out earlier in any language.
        /// Null if the text is not ours.
        /// </summary>
        public static string Relocalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            if (text.StartsWith(Prefix, StringComparison.Ordinal)) return Resolve(text);
            return KeyByText.TryGetValue(text, out var key) ? Resolve(key) : null;
        }
    }
}
