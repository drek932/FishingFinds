# FishingFinds — how it works

A MelonLoader mod for The Long Dark (Il2Cpp, net6). This page is for people reading or changing the code;
players only need the [README](../README.md).

## The idea in one paragraph

When the game creates a caught fish (`IceFishingHole.InstantiateFish`), the mod may return a different `GearItem`
instead. The player then pulls that item out of the hole exactly like a fish. Everything else — fishing time,
skill, tackle, tip-ups — is the game's own logic.

## The roll

```
catch ─► which kind of catch? ─► chance roll ─► category (by share) ─► item (by rarity weight) ─► create it
```

1. **Kind of catch** (`Patches/FishingPatches.cs`)
   - *targeted*: rod without bait or lure;
   - *casual*: rod with bait/lure (setting "Items when fishing with bait") or any tip-up (setting "Works with tip-ups").
     Tip-ups don't check bait: the game uses the bait up when the tip-up is set, so it is gone when the fish is created.
2. **Chance** (`LootRoller.RollChance`, values from `Config`)
   - targeted: `base + bonus × (level − 1)` — 25% + 5% per ice fishing level by default;
   - casual: `base × casual%` — 50% of the base by default, no level bonus.
3. **Rarity limit** (`LootRoller.TierLimit`): exceptional items need the set ice fishing level (3 by default);
   tip-ups also have their own limit (Rare by default).
4. **Category, then item** (`LootRoller.Draw`): a category is picked by its share, then an item inside it by the weight
   of its rarity — Frequent 10, Common 5, Rare 2, Very rare 0.5, Exceptional 0.1. So a rarity only compares items
   **inside one category**; how often a category comes up is its share.
5. **"Like Interloper/Misery"** (`LootRoller.Pick`): if the drawn item is one the game disables on those modes,
   the catch simply stays a fish. All other rates stay exactly as they are.
6. **Create** (`LootRoller.CreateItem`): `GearItem.InstantiateGearItem(prefab)`. If an item can't be created
   (e.g. a DLC item without the DLC), another one is drawn, up to 5 times. A jerry can gets 10–50% kerosene.

Every catch writes one line to the MelonLoader log with the kind of catch, level, chance, roll and result.

## Code layout

| Folder / file | What it does |
|---|---|
| `Main.cs` | Entry point: loads the table and settings, creates the menu |
| `Core/LootData.cs` | Loot table model; reads/writes `Mods/FishingFinds_Loot.json` |
| `Core/DefaultTable.cs` | **Generated** built-in table (see below) — don't edit by hand |
| `Core/Config.cs` | Player settings (`Mods/FishingFinds.json`) and the values in effect right now |
| `Core/LootRoller.cs` | The roll described above |
| `Core/Texts.cs` | All texts, Russian and English |
| `Core/Loc.cs` | Text lookup by game language; texts built from the table (item names etc.) |
| `Settings/SettingsMenu*.cs` | ModSettings menu: built at startup from the table (one field per item), three levels (general / deep / extreme) |
| `Patches/FishingPatches.cs` | The catch hook |
| `Patches/SkillPatches.cs` | "Chance to catch valuables: X%" in the ice fishing skill description |
| `Patches/LocalizationPatches.cs`, `ModSettingsPatches.cs` | Serve the mod's texts to the game and keep menu descriptions in the current language |

### Settings, briefly

- Three levels: general settings always apply; **deep** settings replace some of them while turned on;
  **extreme** settings (only with deep) replace the added/removed item switches with a rarity for every item.
- Turning a level on copies the values it replaces into it (e.g. the exceptional choice → the level slider),
  and turning deep settings on resets the chances to the defaults.
- Only values that differ from the defaults are saved, so changes of the built-in table reach existing players.
- ModSettings quirk: a control that becomes visible can write its stale value back into its field.
  `SettingsMenu.Behavior.cs` protects values written by code (and values of just-shown controls) for ~60 frames.

## The loot table

The design source is in `docs/data/`:

| File | Content |
|---|---|
| `beachcombing.tsv` | Items from Beachcombing: category, share, rarity, English and Russian name, prefab name(s) |
| `added.tsv` | Items this mod adds (not in Beachcombing) |
| `removed.tsv` | Beachcombing items left out by default (can be turned back on), with their home category and rarity |
| `rejected.tsv` | Items considered and not added, with the reason |
| `high_difficulty.txt` | Prefabs the game disables on Interloper/Misery; made from `xp-report.txt` (a dump of `DisableObjectForXPMode` on each prefab) |
| `archive/` | Older table versions, kept for history |

Generated from it:

```bash
cd docs/data
awk -f gen_cs.awk high_difficulty.txt beachcombing.tsv added.tsv removed.tsv > ../../Core/DefaultTable.cs
awk -f gen.awk beachcombing.tsv added.tsv removed.tsv rejected.tsv > ../loot-table.md
```

```bash
cd tools/LootWorkbook
dotnet run -c Release
```

The last command writes `docs/FishingFinds-loot.xlsx` — every item with live formulas, handy for trying out a balance change.

When the table changes, raise `LootData.CurrentVersion`: players' `FishingFinds_Loot.json` is then rewritten
(the old one is kept as `.bak`), and saved per-item/per-category values are reset.
Category ids end up in player settings — give a category a new id when its meaning changes.

## Building

- .NET SDK 8 (the mod targets net6.0), the game with MelonLoader 0.7.x and ModSettings installed.
  The game path is `C:\Games\The Long Dark` by default; override with `-p:TLDPath=...`.
- `dotnet build -c Release` — the DLL goes to `bin/Release`, and a copy to `releases/FishingFinds-<version>.dll`.
  Copy it into the game's `Mods` folder yourself, with the game closed.

## Checking the roll without the game

```bash
cd tools/Simulation
dotnet run -c Release [draws]
```

This runs the mod's own drawing code a million times (default) on the built-in table, in normal and in
"Like Interloper/Misery" mode, and compares every item with the rate the table promises. Expect deviations of a few
percent for rare items (that's sampling noise; it shrinks with more draws), no flagged items in hard mode, and about 18% fish there.
