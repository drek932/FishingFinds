using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FishingFinds
{
    /// <summary>Menu behaviour before Apply: copying values between levels, reset, protecting values written by code, visibility.</summary>
    public static partial class SettingsMenu
    {
        // ---------- reacting to menu changes (before "Apply") ----------

        public static void OnFieldChanged(DynamicSettingsBase target, FieldInfo field, object oldValue, object newValue)
        {
            if (!Meta.TryGetValue(field.Name, out var m)) return;

            // A control that was just shown (or whose value was just set by code) may push its stale value back.
            if (protectFrames > 0 && Protected.TryGetValue(field, out var kept) && !Equals(kept, newValue))
            {
                field.SetValue(settings, kept);
                restoredCount++;
                settings.RefreshGUI();
                return;
            }

            // Show the affected controls first, then write values: a control overwrites its field when it appears.
            UpdateVisibility();

            if (m.Kind == Kind.Deep && newValue is true) { CopyExceptionalChoiceToDeep(); ResetChanceToDefaults(); }
            else if (m.Kind == Kind.HardMode && newValue is true) TurnOffAddedForHardMode();
            else if (m.Kind == Kind.Extreme && newValue is true) CopySwitchesToTiers();
            else if (m.Kind == Kind.Reset && newValue is true) ResetTiers(field);

            settings.RefreshGUI();
        }

        // ---------- values written by code ----------

        // Values the menu must not change for a short while (see OnFieldChanged).
        private static readonly Dictionary<FieldInfo, object> Protected = new();
        private const int ProtectForFrames = 60;
        private static int protectFrames;
        private static int restoredCount;

        private static void SetByCode(FieldInfo field, object value)
        {
            field.SetValue(settings, value);
            Protect(field);
        }

        private static void Protect(FieldInfo field)
        {
            Protected[field] = field.GetValue(settings);
            protectFrames = ProtectForFrames;
        }

        /// <summary>Re-checks protected values every frame and puts them back if a control changed them without OnChange.</summary>
        private static void KeepProtectedValues()
        {
            if (protectFrames <= 0) return;

            bool changed = false;
            foreach (var (field, value) in Protected)
            {
                if (Equals(field.GetValue(settings), value)) continue;
                field.SetValue(settings, value);
                restoredCount++;
                changed = true;
            }
            if (changed) settings.RefreshGUI();

            if (--protectFrames == 0)
            {
                if (restoredCount > 0) Main.Log.Msg($"Menu: restored {restoredCount} value(s) overwritten by controls that just appeared");
                Protected.Clear();
                restoredCount = 0;
            }
        }

        /// <summary>"Like Interloper/Misery" on: added items are not balanced for hard modes, so they are switched off too (can be undone).</summary>
        private static void TurnOffAddedForHardMode()
        {
            var (field, _) = fieldList.First(x => x.meta.Kind == Kind.DisableAllAdded);
            if ((bool)field.GetValue(settings)) return;
            SetByCode(field, true);
            Main.Log.Msg("Menu: hard mode on, added items turned off");
        }

        /// <summary>Deep on: chance and bonus start from the mod's defaults (25% + 5% per level).</summary>
        private static void ResetChanceToDefaults()
        {
            SetByCode(fieldList.First(x => x.meta.Kind == Kind.Chance).field, Config.DefaultChance);
            SetByCode(fieldList.First(x => x.meta.Kind == Kind.Bonus).field, Config.DefaultBonusPerLevel);
            SetByCode(fieldList.First(x => x.meta.Kind == Kind.CasualChance).field, Config.DefaultCasualChancePercent);
            SetByCode(fieldList.First(x => x.meta.Kind == Kind.TipUpMaxTier).field, TipUpChoiceIndex(Config.DefaultTipUpMaxTier));
            Main.Log.Msg($"Menu: deep on, chance reset to {Config.DefaultChance}% + {Config.DefaultBonusPerLevel}% per level, " +
                         $"casual catch {Config.DefaultCasualChancePercent}% of base, tip-ups up to {Config.DefaultTipUpMaxTier}");
        }
        /// <summary>Deep on: the level slider takes over the general choice ("always" = 1, "from level 3" = 3).</summary>
        private static void CopyExceptionalChoiceToDeep()
        {
            bool always = Get<int>(Kind.ExceptionalChoice) == 0;
            var (field, _) = fieldList.First(x => x.meta.Kind == Kind.ExceptionalMin);
            int level = (int)field.GetValue(settings);
            if (always && level != 1) SetByCode(field, 1);
            else if (!always && level == 1) SetByCode(field, Config.BasicExceptionalLevel);
            Main.Log.Msg($"Menu: deep on, exceptional from level {field.GetValue(settings)} (general choice: {(always ? "always" : "from level 3")})");
        }

        /// <summary>
        /// Extreme on: items switched off by the added/removed lists become Off, items switched on get their tier back.
        /// Tiers set earlier are kept otherwise.
        /// </summary>
        private static void CopySwitchesToTiers()
        {
            bool allAddedOff = Get<bool>(Kind.DisableAllAdded);
            int changed = 0;
            var switches = new Dictionary<string, bool>();
            foreach (var (f, meta) in fieldList)
                if (meta.Kind is Kind.AddedItem or Kind.RemovedItem) switches[meta.Item.Id] = (bool)f.GetValue(settings);

            foreach (var (field, m) in fieldList.Where(x => x.meta.Kind == Kind.ItemTier))
            {
                var item = m.Item;
                bool on = item.Origin switch
                {
                    Origin.Added => !allAddedOff && switches.GetValueOrDefault(item.Id, true),
                    Origin.Removed => switches.GetValueOrDefault(item.Id, false),
                    _ => true,
                };
                var tier = (Tier)(int)field.GetValue(settings);
                if (on && tier == Tier.Off) { SetByCode(field, (int)item.Tier); changed++; }
                else if (!on && tier != Tier.Off) { SetByCode(field, (int)Tier.Off); changed++; }
            }
            Main.Log.Msg($"Menu: extreme on, {changed} tier(s) synced with the added/removed switches");
        }

        private static void ResetTiers(FieldInfo resetField)
        {
            int changed = 0;
            foreach (var (field, m) in fieldList.Where(x => x.meta.Kind == Kind.ItemTier))
            {
                int value = (int)Config.DefaultExtremeTier(m.Item);
                if ((int)field.GetValue(settings) != value) changed++;
                SetByCode(field, value);
            }
            // The tab already needs confirmation (the toggle was changed in the menu), so the switch can go back off.
            SetByCode(resetField, false);
            Main.Log.Msg($"Menu: extreme tiers reset to defaults, {changed} changed (press Apply to keep)");
        }

        // ---------- visibility ----------

        /// <summary>
        /// Called every frame: closing the menu without "Apply" puts the old values back without OnChange,
        /// so the visible sections are re-checked whenever the switches that drive them change.
        /// </summary>
        public static void LateUpdate()
        {
            if (settings == null) return;
            if (!settings.IsVisible())
            {
                // Menu closed: ModSettings restored or confirmed the values itself.
                Protected.Clear();
                protectFrames = 0;
                return;
            }
            if (VisibilityState() != lastVisibilityState) UpdateVisibility();
            KeepProtectedValues();
        }

        private static string VisibilityState()
        {
            var sb = new System.Text.StringBuilder(visibilityDrivers.Count);
            foreach (var f in visibilityDrivers) sb.Append((bool)f.GetValue(settings) ? '1' : '0');
            return sb.ToString();
        }

        private static void UpdateVisibility()
        {
            bool deep = Get<bool>(Kind.Deep);
            bool extreme = deep && Get<bool>(Kind.Extreme);
            bool showAdded = Get<bool>(Kind.ShowAdded);
            bool showRemoved = Get<bool>(Kind.ShowRemoved);
            bool showShares = Get<bool>(Kind.ShowShares);
            var showCategory = new Dictionary<string, bool>();
            foreach (var (f, meta) in fieldList)
                if (meta.Kind == Kind.ShowCategoryTiers) showCategory[meta.Category.Id] = (bool)f.GetValue(settings);

            foreach (var (field, m) in fieldList)
            {
                bool visible = m.Kind switch
                {
                    Kind.ExceptionalChoice => !deep,
                    Kind.DisableAllAdded or Kind.ShowAdded or Kind.ShowRemoved => !extreme,
                    Kind.AddedItem => !extreme && showAdded,
                    Kind.RemovedItem => !extreme && showRemoved,
                    Kind.Chance or Kind.Bonus or Kind.ExceptionalMin or Kind.CasualChance or Kind.TipUpMaxTier or Kind.ShowShares or Kind.Extreme => deep,
                    Kind.Share => deep && showShares,
                    Kind.Reset or Kind.ShowCategoryTiers => extreme,
                    Kind.ItemTier => extreme && showCategory[m.Category.Id],
                    _ => true,
                };
                // A control that is about to appear keeps the field's current value, not its own stale one.
                if (visible && !settings.IsFieldVisible(field)) Protect(field);
                settings.SetFieldVisible(field, visible);
            }
            lastVisibilityState = VisibilityState();
        }
    }
}
