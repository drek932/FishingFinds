using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ModSettings;

namespace FishingFinds
{
    /// <summary>Base class for the generated settings: forwards ModSettings events to SettingsMenu.</summary>
    public abstract class DynamicSettingsBase : ModSettingsBase
    {
        protected override void OnConfirm() => SettingsMenu.Apply(this);

        protected override void OnChange(FieldInfo field, object oldValue, object newValue) =>
            SettingsMenu.OnFieldChanged(this, field, oldValue, newValue);
    }

    /// <summary>
    /// The "Fishing Finds" menu. The item lists come from the loot table, so the settings class is generated
    /// at startup (Reflection.Emit), one field per item. Three levels:
    ///  general (always) -> deep (while "Enable deep settings" is on) -> extreme (while deep and extreme are on).
    /// Turning a level on copies the values it replaces (the exceptional choice, the item switches) into it.
    ///
    /// This file builds the menu; SettingsMenu.Values.cs reads and saves the values;
    /// SettingsMenu.Behavior.cs handles changes before "Apply" (copying between levels, reset, visibility).
    /// </summary>
    // public: the class generated in the dynamic assembly calls FillValues.
    public static partial class SettingsMenu
    {
        private const string MenuName = "Fishing Finds";

        private enum Kind
        {
            Enabled,
            ExceptionalChoice,
            TipUps,
            BaitCatches,
            HardMode,
            DisableAllAdded,
            ShowAdded,
            AddedItem,
            ShowRemoved,
            RemovedItem,
            Deep,
            Chance,
            Bonus,
            ExceptionalMin,
            CasualChance,
            TipUpMaxTier,
            ShowShares,
            Share,
            Extreme,
            Reset,
            ShowCategoryTiers,
            ItemTier,
        }

        private sealed class FieldMeta
        {
            public Kind Kind;
            public ItemData Item;
            public CategoryData Category;
        }

        // Options of "Rarest item on a tip-up" (choice index -> tier).
        private static readonly Tier[] TipUpTierChoices = { Tier.Rare, Tier.VeryRare, Tier.Exceptional };

        private static int TipUpChoiceIndex(Tier tier) => Math.Max(0, Array.IndexOf(TipUpTierChoices, tier));

        private static readonly Dictionary<string, FieldMeta> Meta = new();
        private static DynamicSettingsBase settings;
        private static List<(FieldInfo field, FieldMeta meta)> fieldList;
        private static List<FieldInfo> visibilityDrivers;
        private static string lastVisibilityState;

        // ---------- building the menu ----------

        public static void Register()
        {
            var type = GenerateSettingsType(LootData.Current);
            settings = (DynamicSettingsBase)Activator.CreateInstance(type);
            fieldList = Fields(settings).ToList();
            visibilityDrivers = fieldList
                .Where(x => x.meta.Kind is Kind.Deep or Kind.Extreme or Kind.ShowAdded or Kind.ShowRemoved or Kind.ShowShares or Kind.ShowCategoryTiers)
                .Select(x => x.field).ToList();
            settings.AddToModSettings(MenuName);
            UpdateVisibility();
            Main.Log.Msg($"Settings menu: {Meta.Count} fields");
        }

        private static Type GenerateSettingsType(LootData data)
        {
            var tb = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("FishingFinds.GeneratedSettings"), AssemblyBuilderAccess.Run)
                .DefineDynamicModule("Main")
                .DefineType("FishingFindsSettings", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, typeof(DynamicSettingsBase));

            // General
            var f = Define(tb, "g_enabled", typeof(bool), Kind.Enabled, "ENABLED", "ENABLED_DESC");
            f.SetCustomAttribute(Text<SectionAttribute>(Loc.Key("SECTION_GENERAL"), true));
            f = Define(tb, "g_exceptional", typeof(int), Kind.ExceptionalChoice, "EXCEPTIONAL", "EXCEPTIONAL_DESC");
            f.SetCustomAttribute(Choice("EXCEPTIONAL_ALWAYS", "EXCEPTIONAL_LEVEL3"));
            Define(tb, "g_baitCatches", typeof(bool), Kind.BaitCatches, "BAIT_CATCHES", "BAIT_CATCHES_DESC");
            Define(tb, "g_tipups", typeof(bool), Kind.TipUps, "TIPUPS", "TIPUPS_DESC");
            Define(tb, "g_hardMode", typeof(bool), Kind.HardMode, "HARDMODE", "HARDMODE_DESC");

            // Added items
            var added = data.Items.Where(i => i.Origin == Origin.Added).ToList();
            f = Define(tb, "a_disableAll", typeof(bool), Kind.DisableAllAdded, "DISABLE_ADDED", "DISABLE_ADDED_DESC");
            f.SetCustomAttribute(Text<SectionAttribute>(Loc.Key("SECTION_ADDED"), true));
            Define(tb, "a_show", typeof(bool), Kind.ShowAdded, Loc.Key("SHOW_LIST_COUNT", added.Count), "SHOW_LIST_DESC");
            for (int i = 0; i < added.Count; i++)
                Define(tb, $"a_{i}", typeof(bool), Kind.AddedItem, Loc.Key("ITEM_LABEL", added[i].Id), Loc.Key("ADDED_DESC", added[i].Id), item: added[i]);

            // Removed items
            var removed = data.Items.Where(i => i.Origin == Origin.Removed).ToList();
            f = Define(tb, "r_show", typeof(bool), Kind.ShowRemoved, Loc.Key("SHOW_LIST_COUNT", removed.Count), "SHOW_LIST_DESC");
            f.SetCustomAttribute(Text<SectionAttribute>(Loc.Key("SECTION_REMOVED"), true));
            for (int i = 0; i < removed.Count; i++)
                Define(tb, $"r_{i}", typeof(bool), Kind.RemovedItem, Loc.Key("ITEM_LABEL", removed[i].Id), Loc.Key("REMOVED_DESC", removed[i].Id), item: removed[i]);

            // Deep
            f = Define(tb, "d_deep", typeof(bool), Kind.Deep, "DEEP", "DEEP_DESC");
            f.SetCustomAttribute(Text<SectionAttribute>(Loc.Key("SECTION_DEEP"), true));
            // Descriptions are built when shown: they include the chance at the player's current level.
            Define(tb, "d_chance", typeof(int), Kind.Chance, "CHANCE", Loc.Key("CHANCE_DESC", "now")).SetCustomAttribute(Slider(0, 100));
            Define(tb, "d_bonus", typeof(int), Kind.Bonus, "BONUS", Loc.Key("BONUS_DESC", "now")).SetCustomAttribute(Slider(0, 20));
            Define(tb, "d_exceptionalMin", typeof(int), Kind.ExceptionalMin, "EXCEPTIONAL_MIN", "EXCEPTIONAL_MIN_DESC").SetCustomAttribute(Slider(1, 5));
            Define(tb, "d_casualChance", typeof(int), Kind.CasualChance, "CASUAL_CHANCE", "CASUAL_CHANCE_DESC").SetCustomAttribute(Slider(0, 100));
            Define(tb, "d_tipUpMaxTier", typeof(int), Kind.TipUpMaxTier, "TIPUP_MAXTIER", "TIPUP_MAXTIER_DESC")
                .SetCustomAttribute(Choice(TipUpTierChoices.Select(t => "TIER_" + t).ToArray()));
            Define(tb, "d_showShares", typeof(bool), Kind.ShowShares, Loc.Key("SHOW_SHARES", data.Categories.Count), "SHOW_LIST_DESC");
            for (int i = 0; i < data.Categories.Count; i++)
            {
                var c = data.Categories[i];
                Define(tb, $"d_share_{i}", typeof(int), Kind.Share, Loc.Key("SHARE_LABEL", c.Id), Loc.Key("SHARE_DESC", c.Id), category: c)
                    .SetCustomAttribute(Slider(0, 100));
            }

            // Extreme
            f = Define(tb, "x_extreme", typeof(bool), Kind.Extreme, "EXTREME", "EXTREME_DESC");
            f.SetCustomAttribute(Text<SectionAttribute>(Loc.Key("SECTION_EXTREME"), true));
            Define(tb, "x_reset", typeof(bool), Kind.Reset, "RESET", "RESET_DESC");
            var tierChoice = Choice(Enum.GetNames(typeof(Tier)).Select(n => "TIER_" + n).ToArray());
            int n = 0;
            for (int i = 0; i < data.Categories.Count; i++)
            {
                var c = data.Categories[i];
                Define(tb, $"x_show_{i}", typeof(bool), Kind.ShowCategoryTiers, Loc.Key("SHOW_CATEGORY", c.Id), "SHOW_LIST_DESC", category: c);
                foreach (var item in data.ItemsIn(c.Id))
                    Define(tb, $"x_tier_{n++}", typeof(int), Kind.ItemTier, Loc.Key("ITEM_LABEL", item.Id), Loc.Key("TIER_DESC", item.Id), item: item, category: c)
                        .SetCustomAttribute(tierChoice);
            }

            DefineConstructor(tb);
            return tb.CreateType();
        }

        /// <summary>A field with a localized name (a Loc key or a full "FISHFINDS_…" key) and a description key.</summary>
        private static FieldBuilder Define(TypeBuilder tb, string name, Type type, Kind kind, string title, string description,
            ItemData item = null, CategoryData category = null)
        {
            var f = tb.DefineField(name, type, FieldAttributes.Public);
            f.SetCustomAttribute(Text<NameAttribute>(FullKey(title), true));
            // The description is an untranslated key: it is translated when shown (see ModSettingsPatches).
            f.SetCustomAttribute(Text<DescriptionAttribute>(FullKey(description), false));
            Meta[name] = new FieldMeta { Kind = kind, Item = item, Category = category };
            return f;
        }

        private static string FullKey(string key) => key.StartsWith(Loc.Prefix, StringComparison.Ordinal) ? key : Loc.Key(key);

        private static CustomAttributeBuilder Text<T>(string text, bool localize) =>
            new(typeof(T).GetConstructor(new[] { typeof(string) }), new object[] { text },
                new[] { typeof(T).GetProperty("Localize") }, new object[] { localize });

        private static CustomAttributeBuilder Choice(params string[] keys) =>
            new(typeof(ChoiceAttribute).GetConstructor(new[] { typeof(string[]) }),
                new object[] { keys.Select(k => Loc.Key(k)).ToArray() },
                new[] { typeof(ChoiceAttribute).GetProperty("Localize") }, new object[] { true });

        private static CustomAttributeBuilder Slider(float from, float to) =>
            new(typeof(SliderAttribute).GetConstructor(new[] { typeof(float), typeof(float) }), new object[] { from, to });

        /// <summary>
        /// Constructor: fill the fields from Config first, then call the base constructor,
        /// so ModSettings remembers exactly these as the confirmed values.
        /// </summary>
        private static void DefineConstructor(TypeBuilder tb)
        {
            var il = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes).GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(SettingsMenu).GetMethod(nameof(FillValues), BindingFlags.Public | BindingFlags.Static));
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(DynamicSettingsBase).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null));
            il.Emit(OpCodes.Ret);
        }
    }
}
