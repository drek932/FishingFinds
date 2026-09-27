using System;
using MelonLoader;

[assembly: MelonInfo(typeof(FishingFinds.Main), "FishingFinds", "2.5.1", "drek932")]
[assembly: MelonGame("Hinterland", "TheLongDark")]
[assembly: MelonOptionalDependencies("ModSettings")]

namespace FishingFinds
{
    /// <summary>
    /// Ice fishing sometimes brings up an item instead of a fish — an alternative to Beachcombing
    /// for regions without a coastline. See docs/DEVELOPMENT.md for how the mod works.
    ///
    ///  Core/     — loot table (LootData, DefaultTable), player settings (Config), the roll (LootRoller),
    ///              texts (Texts) and their lookup (Loc);
    ///  Settings/ — the ModSettings menu;
    ///  Patches/  — hooks into game functions (Harmony).
    /// </summary>
    public sealed class Main : MelonMod
    {
        internal static MelonLogger.Instance Log;

        private static bool settingsMenuCreated;

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;

            LootData.Load();
            Config.Load();
            Log.Msg("Settings: " + Config.Current.Describe(LootData.Current));
            Log.Msg("Category shares: " + Config.Current.DescribeShares(LootData.Current));

            try
            {
                RegisterSettings(HarmonyInstance);
            }
            catch (Exception e)
            {
                Log.Warning("Settings menu was not created (is ModSettings installed?): " + e);
            }
        }

        // Separate method so a missing ModSettings.dll throws here instead of when the mod is loaded.
        private static void RegisterSettings(HarmonyLib.Harmony harmony)
        {
            SettingsMenu.Register();
            ModSettingsPatches.Apply(harmony);
            settingsMenuCreated = true;
        }

        public override void OnLateUpdate()
        {
            if (!settingsMenuCreated) return;
            ModSettingsPatches.LateUpdate();
            SettingsMenu.LateUpdate();
        }
    }
}
