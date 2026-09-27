using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FishingFinds
{
    /// <summary>Menu values: Config -> menu fields when the menu is created, menu fields -> Config on Apply.</summary>
    public static partial class SettingsMenu
    {
        // ---------- values ----------

        /// <summary>Config -> menu fields. Called from the generated constructor.</summary>
        public static void FillValues(object target)
        {
            var c = Config.Current;
            foreach (var (field, m) in Fields(target))
            {
                object value = m.Kind switch
                {
                    Kind.Enabled => c.Enabled,
                    Kind.ExceptionalChoice => c.ExceptionalFromLevel3 ? 1 : 0,
                    Kind.TipUps => c.AllowTipUps,
                    Kind.BaitCatches => c.AllowBaitCatches,
                    Kind.HardMode => c.HardModeLoot,
                    Kind.DisableAllAdded => c.DisableAllAdded,
                    Kind.AddedItem or Kind.RemovedItem => c.IsOn(m.Item),
                    Kind.Deep => c.Deep,
                    Kind.Chance => Math.Clamp(c.ChancePercent, 0, 100),
                    Kind.Bonus => Math.Clamp(c.BonusPerLevel, 0, 20),
                    Kind.ExceptionalMin => Math.Clamp(c.ExceptionalMinLevel, 1, 5),
                    Kind.CasualChance => Math.Clamp(c.CasualChancePercent, 0, 100),
                    Kind.TipUpMaxTier => TipUpChoiceIndex(c.TipUpMaxTier),
                    Kind.Share => Math.Clamp(c.CategoryShares.TryGetValue(m.Category.Id, out var s) ? s : m.Category.Share, 0, 100),
                    Kind.Extreme => c.Extreme,
                    Kind.ItemTier => (int)c.ExtremeTier(m.Item),
                    _ => false, // list toggles and reset start off
                };
                field.SetValue(target, value);
            }
        }

        /// <summary>Menu fields -> Config. Called when the player presses "Apply".</summary>
        public static void Apply(DynamicSettingsBase target)
        {
            var c = Config.Current;
            foreach (var (field, m) in Fields(target))
            {
                var v = field.GetValue(target);
                switch (m.Kind)
                {
                    case Kind.Enabled: c.Enabled = (bool)v; break;
                    case Kind.ExceptionalChoice: c.ExceptionalFromLevel3 = (int)v == 1; break;
                    case Kind.TipUps: c.AllowTipUps = (bool)v; break;
                    case Kind.BaitCatches: c.AllowBaitCatches = (bool)v; break;
                    case Kind.HardMode: c.HardModeLoot = (bool)v; break;
                    case Kind.DisableAllAdded: c.DisableAllAdded = (bool)v; break;
                    // Only values that differ from the defaults are stored, so later table updates reach the player.
                    case Kind.AddedItem:
                    case Kind.RemovedItem:
                        if ((bool)v == (m.Item.Origin != Origin.Removed)) c.ItemOn.Remove(m.Item.Id);
                        else c.ItemOn[m.Item.Id] = (bool)v;
                        break;
                    case Kind.Deep: c.Deep = (bool)v; break;
                    case Kind.Chance: c.ChancePercent = (int)v; break;
                    case Kind.Bonus: c.BonusPerLevel = (int)v; break;
                    case Kind.ExceptionalMin: c.ExceptionalMinLevel = (int)v; break;
                    case Kind.CasualChance: c.CasualChancePercent = (int)v; break;
                    case Kind.TipUpMaxTier: c.TipUpMaxTier = TipUpTierChoices[Math.Clamp((int)v, 0, TipUpTierChoices.Length - 1)]; break;
                    case Kind.Share:
                        if ((int)v == m.Category.Share) c.CategoryShares.Remove(m.Category.Id);
                        else c.CategoryShares[m.Category.Id] = (int)v;
                        break;
                    case Kind.Extreme: c.Extreme = (bool)v; break;
                    case Kind.ItemTier:
                        if ((Tier)(int)v == Config.DefaultExtremeTier(m.Item)) c.ItemTiers.Remove(m.Item.Id);
                        else c.ItemTiers[m.Item.Id] = (Tier)(int)v;
                        break;
                }
            }
            Config.Save();
            Main.Log.Msg("Settings applied: " + c.Describe(LootData.Current));
            Main.Log.Msg("Category shares: " + c.DescribeShares(LootData.Current));
            var changedTiers = LootData.Current.Items.Where(i => c.ExtremeActive && c.ExtremeTier(i) != Config.DefaultExtremeTier(i))
                .Select(i => $"{i.NameEn} {c.ExtremeTier(i)}").ToList();
            if (changedTiers.Count > 0) Main.Log.Msg($"Non-default tiers ({changedTiers.Count}): {string.Join(", ", changedTiers)}");
        }

        private static IEnumerable<(FieldInfo field, FieldMeta meta)> Fields(object target)
        {
            foreach (var f in target.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (Meta.TryGetValue(f.Name, out var m)) yield return (f, m);
        }

        private static T Get<T>(Kind kind) => (T)fieldList.First(x => x.meta.Kind == kind).field.GetValue(settings);


        /// <summary>
        /// Chance and bonus as currently shown in the menu (not yet applied), for the "now at level N" hint.
        /// Falls back to the applied settings when the menu doesn't exist.
        /// </summary>
        public static (int chance, int bonus) ShownChance()
        {
            if (settings == null) return (Config.Current.BaseChance, Config.Current.Bonus);
            if (!Get<bool>(Kind.Deep)) return (Config.DefaultChance, Config.DefaultBonusPerLevel);
            return (Get<int>(Kind.Chance), Get<int>(Kind.Bonus));
        }
    }
}
