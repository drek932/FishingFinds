using System.Collections.Generic;

namespace FishingFinds
{
    /// <summary>
    /// All texts of the mod, Russian and English. Item and category names come from the loot table instead.
    /// "{0}", "{1}" are filled in by Loc (see Loc.Dynamic for the texts built from the loot table).
    /// Edit texts here; the keys are used in Settings/SettingsMenu.cs and Patches/SkillPatches.cs.
    /// </summary>
    internal static class Texts
    {
        /// <summary>Key -> (Russian, English).</summary>
        public static readonly Dictionary<string, (string ru, string en)> All = new()
        {
            // --- general settings ---
            ["SECTION_GENERAL"] = ("Основные", "General"),
            ["ENABLED"] = ("Мод включён", "Mod enabled"),
            ["ENABLED_DESC"] = (
                "Да — при рыбалке без наживки и приманки вместо рыбы иногда попадаются полезные вещи. Нет — обычная рыбалка.",
                "Yes — when fishing without bait or lure, a useful item sometimes comes up instead of a fish. No — vanilla fishing."),
            ["EXCEPTIONAL"] = ("Исключительные предметы", "Exceptional items"),
            ["EXCEPTIONAL_DESC"] = (
                "Самые редкие и ценные находки: экспедиционная парка, муклуки, сигнальный пистолет, экстренный стимулятор, нож выживания и т. п. " +
                "«С 3 уровня рыбалки» — пока навык ниже 3, они не попадаются.",
                "The rarest and most valuable finds: the expedition parka, mukluks, the distress pistol, the emergency stim, the survival knife, etc. " +
                "\"From ice fishing level 3\" — they don't drop while the skill is below 3."),
            ["EXCEPTIONAL_ALWAYS"] = ("Всегда доступны", "Always available"),
            ["EXCEPTIONAL_LEVEL3"] = ("С 3 уровня рыбалки", "From ice fishing level 3"),
            ["TIPUPS"] = ("Работает с жерлицами", "Works with tip-ups"),
            ["TIPUPS_DESC"] = (
                "Да — жерлица тоже иногда ловит вещи, с наживкой и без неё, но реже: половина базового шанса, " +
                "без бонуса за уровень рыбалки, и не выше «Редкой» редкости. Нет — жерлица ловит только рыбу.",
                "Yes — a tip-up can also catch items, with or without bait, but less often: half the base chance, " +
                "no ice fishing level bonus, and nothing rarer than Rare. No — a tip-up only catches fish."),
            ["HARDMODE"] = ("Как на Interloper/Misery", "Like Interloper/Misery"),
            ["HARDMODE_DESC"] = (
                "Да — не ловятся вещи, которые сама игра убирает на Interloper и Misery (ножи, топор, ножовка, леска, фальшфейеры, " +
                "часть одежды и еды). Если выпала такая вещь, остаётся рыба, поэтому вещей в целом примерно на 18% меньше. " +
                "При включении также выключаются добавленные предметы (их можно включить обратно). Нет — обычный улов.",
                "Yes — items the game itself removes on Interloper and Misery (knives, hatchet, hacksaw, line, flares, " +
                "some clothing and food) never come up. If such an item is drawn, you keep the fish, so there are about 18% fewer items overall. " +
                "Turning this on also turns off the added items (you can turn them back on). No — normal catch."),
            ["BAIT_CATCHES"] = ("Вещи и при рыбалке с наживкой", "Items when fishing with bait"),
            ["BAIT_CATCHES_DESC"] = (
                "Да — при рыбалке удочкой с наживкой или приманкой тоже иногда попадаются вещи, но реже: половина базового шанса, " +
                "без бонуса за уровень рыбалки. Без наживки и приманки вещи попадаются намного чаще. Нет — с наживкой или приманкой ловится только рыба.",
                "Yes — fishing with bait or a lure can also bring up items, but less often: half the base chance, " +
                "no ice fishing level bonus. Without bait or lure items come up much more often. No — with bait or a lure you only catch fish."),

            // --- added / removed ---
            ["SECTION_ADDED"] = ("Добавленные предметы", "Added items"),
            ["DISABLE_ADDED"] = ("Выключить все добавленные", "Turn off all added items"),
            ["DISABLE_ADDED_DESC"] = (
                "Добавленные — вещи, которых нет в Beachcombing, например: тактическая куртка, нож выживания, консервированная ветчина, одежда торговца. " +
                "Да — ни одна из них не попадётся, независимо от списка ниже.",
                "Added items are not part of Beachcombing, for example: the tactical jacket, the survival knife, canned ham, trader clothing. " +
                "Yes — none of them drops, regardless of the list below."),
            ["SHOW_LIST_COUNT"] = ("Показать список ({0})", "Show list ({0})"),
            ["SHOW_LIST_DESC"] = ("Раскрывает список ниже. Сам по себе ничего не меняет в игре.", "Expands the list below. Changes nothing in game by itself."),
            ["SECTION_REMOVED"] = ("Удалённые предметы", "Removed items"),
            ["ADDED_DESC"] = (
                "Да — может попасться на крючок. Нет — не попадётся. Категория: {0}, редкость: {1}.",
                "Yes — can be caught. No — never drops. Category: {0}, rarity: {1}."),
            ["REMOVED_DESC"] = (
                "Эта вещь есть в Beachcombing, но по умолчанию убрана из улова. Да — вернуть её в улов. Категория: {0}, редкость: {1}.",
                "This item is part of Beachcombing but is left out of the catch by default. Yes — bring it back. Category: {0}, rarity: {1}."),

            // --- deep ---
            ["SECTION_DEEP"] = ("⚠ Глубокие настройки", "⚠ Deep settings"),
            ["DEEP"] = ("⚠ Включить глубокие настройки", "⚠ Enable deep settings"),
            ["DEEP_DESC"] = (
                "⚠ Для опытных игроков: меняют основной баланс мода. Пока выключено, ничего из этого раздела не действует. " +
                "Внимание: при каждом включении ползунки «Базовый шанс замены» и «Бонус за уровень рыбалки» ниже возвращаются " +
                "к стандартным значениям: 25% и 5%. Это шанс, что рыба заменится вещью: 25% на 1 уровне рыбалки и +5% за каждый " +
                "следующий уровень (45% на 5 уровне). Случайный улов (наживка, жерлицы) тоже возвращается к стандартному: " +
                "50% от базового шанса, жерлица — не выше «Редкого».",
                "⚠ For experienced players: these change the mod's core balance. While off, nothing in this section applies. " +
                "Note: every time this is turned on, the \"Base chance\" and \"Bonus per ice fishing level\" sliders below go back " +
                "to the standard values: 25% and 5%. That is the chance a fish is replaced with an item: 25% at ice fishing level 1 " +
                "and +5% for every level above (45% at level 5). The casual catch (bait, tip-ups) also goes back to the standard: " +
                "50% of the base chance, tip-ups nothing rarer than Rare."),
            ["CHANCE"] = ("Базовый шанс замены (1 уровень рыбалки), %", "Base chance to replace a fish (ice fishing level 1), %"),
            ["CHANCE_DESC"] = (
                "Шанс, что пойманная рыба заменится вещью, на 1 уровне рыбалки. С каждым следующим уровнем добавляется бонус. По умолчанию 25. {0}",
                "The chance a caught fish is replaced with an item, at ice fishing level 1. The bonus is added for every level above. Default 25. {0}"),
            ["BONUS"] = ("Бонус за уровень рыбалки, %", "Bonus per ice fishing level, %"),
            ["BONUS_DESC"] = (
                "Прибавляется к шансу за каждый уровень выше первого. По умолчанию 5 (на 5 уровне: 25 + 4 × 5 = 45%). {0}",
                "Added to the chance for every level above 1. Default 5 (at level 5: 25 + 4 × 5 = 45%). {0}"),
            ["CHANCE_NOW"] = ("Сейчас: {0} уровень рыбалки → шанс {1}%.", "Now: ice fishing level {0} → chance {1}%."),
            ["CHANCE_NOW_UNKNOWN"] = (
                "Текущий шанс будет показан здесь, когда загружена игра.",
                "The current chance is shown here once a game is loaded."),
            ["SKILL_CHANCE"] = (
                "Шанс выловить ценности: {0}%",
                "Chance to catch valuables: {0}%"),
            ["EXCEPTIONAL_MIN"] = ("Исключительные: с уровня рыбалки", "Exceptional items: from ice fishing level"),
            ["EXCEPTIONAL_MIN_DESC"] = (
                "С какого уровня рыбалки начинают попадаться исключительные вещи. 1 — с самого начала. " +
                "Заменяет выбор «Исключительные предметы» из основных настроек.",
                "From which ice fishing level exceptional items start to drop. 1 — from the start. " +
                "Replaces the \"Exceptional items\" choice in the general settings."),
            ["CASUAL_CHANCE"] = ("Случайный улов: шанс, % от базового", "Casual catch: chance, % of the base chance"),
            ["CASUAL_CHANCE_DESC"] = (
                "Случайный улов — это рыбалка удочкой с наживкой или приманкой и любые жерлицы. Здесь задаётся шанс, что рыба " +
                "заменится вещью, в процентах от «Базового шанса замены». Бонус за уровень рыбалки не действует. " +
                "По умолчанию 50: при базовом шансе 25% это 12.5% на любом уровне. Действует, только если это включено в основных настройках.",
                "Casual catch means fishing with bait or a lure, and any tip-up. This sets the chance that a fish is replaced with " +
                "an item, as a percentage of the \"Base chance\". The ice fishing level bonus does not apply. " +
                "Default 50: with a 25% base chance that is 12.5% at any level. Only works if it is turned on in the general settings."),
            ["TIPUP_MAXTIER"] = ("Жерлица: самая высокая редкость", "Tip-up: rarest item"),
            ["TIPUP_MAXTIER_DESC"] = (
                "Вещи редче выбранной на жерлицу не попадаются — вместо них выпадают более простые вещи той же категории. " +
                "По умолчанию «Редкий»: жерлица не ловит очень редкие и исключительные вещи, они остаются наградой за активную рыбалку.",
                "Items rarer than this never come up on a tip-up — simpler items of the same category drop instead. " +
                "Default Rare: tip-ups don't catch very rare or exceptional items, those stay a reward for active fishing."),
            ["SHOW_SHARES"] = ("Доли категорий: показать ({0})", "Category shares: show ({0})"),
            ["SHARE_DESC"] = (
                "Как часто попадаются вещи из категории «{0}» по сравнению с другими категориями. Чем больше число, тем чаще. " +
                "Например, у «Хлама» 35, а у «Медицины» 2, поэтому хлам попадается примерно в 17 раз чаще. " +
                "По умолчанию {1}. Сумма всех долей не обязана быть 100. 0 — категория выключена.",
                "How often items from the \"{0}\" category come up compared to other categories. The bigger the number, the more often. " +
                "For example, Junk has 35 and Medicine has 2, so junk comes up about 17 times as often. " +
                "Default {1}. The shares don't have to add up to 100. 0 — category off."),

            // --- extreme ---
            ["SECTION_EXTREME"] = ("⛔ Экстремальные настройки", "⛔ Extreme settings"),
            ["EXTREME"] = ("⛔ Включить экстремальные настройки", "⛔ Enable extreme settings"),
            ["EXTREME_DESC"] = (
                "⛔ Только для тех, кто точно понимает, что делает. Здесь можно задать редкость каждой вещи отдельно и этим полностью " +
                "сломать баланс. Разработчик мода не рекомендует это трогать. Если что-то пошло не так — «Сбросить редкость». " +
                "Пока включено, списки «Добавленные» и «Удалённые» не действуют.",
                "⛔ Only if you know exactly what you are doing. Here you can set the rarity of every item separately and break the balance " +
                "completely. The mod author recommends leaving this alone. If something goes wrong — \"Reset rarity\". " +
                "While on, the Added and Removed lists have no effect."),
            ["RESET"] = ("Сбросить редкость всех вещей к значениям по умолчанию", "Reset the rarity of all items to defaults"),
            ["RESET_DESC"] = (
                "Да — сразу возвращает редкость каждой вещи к значению по умолчанию (удалённые — «Выключен»). Затем нажмите «Применить».",
                "Yes — immediately returns every item's rarity to its default (removed items — Off). Then press Apply."),
            ["SHOW_CATEGORY"] = ("{0}: показать предметы ({1})", "{0}: show items ({1})"),
            ["TIER_DESC"] = (
                "Редкость этой вещи внутри категории «{0}». По умолчанию: {1}. «Выключен» — никогда не попадётся.",
                "The rarity of this item inside the \"{0}\" category. Default: {1}. Off — never drops."),

            // --- tiers ---
            ["TIER_Off"] = ("Выключен", "Off"),
            ["TIER_Frequent"] = ("Частый", "Frequent"),
            ["TIER_Common"] = ("Обычный", "Common"),
            ["TIER_Rare"] = ("Редкий", "Rare"),
            ["TIER_VeryRare"] = ("Очень редкий", "Very rare"),
            ["TIER_Exceptional"] = ("Исключительный", "Exceptional"),
        };
    }
}
