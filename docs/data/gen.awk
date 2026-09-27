# Generates the human-readable loot table with rates from the design tables, in English or Russian.
# Usage (from docs/data):
#   awk -v lang=en -f gen.awk beachcombing.tsv added.tsv removed.tsv rejected.tsv > ../loot-table.md
#   awk -v lang=ru -f gen.awk beachcombing.tsv added.tsv removed.tsv rejected.tsv > ../loot-table.ru.md
BEGIN {
  FS = "\t"
  if (lang != "ru") lang = "en"
  w["Частый"] = 10; w["Обычный"] = 5; w["Редкий"] = 2; w["Очень редкий"] = 0.5; w["Исключительный"] = 0.1
  tierEn["Частый"] = "Frequent"; tierEn["Обычный"] = "Common"; tierEn["Редкий"] = "Rare"
  tierEn["Очень редкий"] = "Very rare"; tierEn["Исключительный"] = "Exceptional"
  catEn["Хлам, дерево и материалы"] = "Junk, wood & materials"; catEn["Природное"] = "Nature"
  catEn["Рыбалка и стрелы"] = "Fishing gear & arrows"; catEn["Огонь, сигналы и боеприпасы"] = "Fire, flares & ammo"
  catEn["Еда"] = "Food"; catEn["Инструменты и снаряжение"] = "Tools & equipment"; catEn["Медицина"] = "Medicine"
  catEn["Мелкая одежда и обувь"] = "Accessories & footwear"; catEn["Одежда и верхняя одежда"] = "Clothing & outerwear"
  base = 0.25; top = 0.45; fishPerDay = 25
}
FILENAME ~ /beachcombing|added/ {
  n++; cat[n] = $1; share[$1] = $2; tier[n] = $3; en[n] = $4; ru[n] = $5; gear[n] = $6
  added[n] = (FILENAME ~ /added/)
  sum[$1] += w[$3]
  if (!($1 in seen)) { seen[$1] = 1; order[++nc] = $1 }
  next
}
FILENAME ~ /removed/ { rn++; rline[rn] = $0; next }
FILENAME ~ /rejected/ { jn++; jline[jn] = $0; next }
function C(c) { return lang == "en" ? catEn[c] : c }
function TR(t) { return lang == "en" ? tierEn[t] : t }
function L(ruText, enText) { return lang == "en" ? enText : ruText }
# Item name column(s): English only, or English + Russian translation.
function NAME(e, r) { return lang == "en" ? e : e " | " r }
function NAMEHEAD() { return lang == "en" ? "Item" : "Предмет | Перевод" }
function NAMESEP() { return lang == "en" ? "---" : "---|---" }
END {
  print L("# FishingFinds — таблица добычи", "# FishingFinds — loot table")
  print ""
  print L("*[English version](loot-table.md)*", "*[Русская версия](loot-table.ru.md)*")
  print ""
  print L("Выбор в два шага: сначала категория по её доле, затем предмет внутри категории по весу его редкости.",
          "An item is picked in two steps: first a category by its share, then an item inside the category by the weight of its rarity.")
  print L("Веса редкости: Частый 10 · Обычный 5 · Редкий 2 · Очень редкий 0.5 · Исключительный 0.1.",
          "Rarity weights: Frequent 10 · Common 5 · Rare 2 · Very rare 0.5 · Exceptional 0.1.")
  print ""
  print L("- **1 раз на N рыб** — при шансе 25% (1 уровень рыбалки, пустой крючок).",
          "- **Once per N fish** — at a 25% chance (ice fishing level 1, empty hook).")
  print L("- **≈ раз в N дней** — на 5 уровне рыбалки (шанс 45%), 25 рыб за 12 часов; «день» = 12 часов непрерывной рыбалки.",
          "- **≈ once per N days** — at ice fishing level 5 (45% chance), 25 fish per 12 hours; a \"day\" = 12 hours of continuous fishing.")
  print L("- ➕ — предмет добавлен модом (его нет в Beachcombing). DLC — Tales from the Far Territory.",
          "- ➕ — added by this mod (not part of Beachcombing). DLC — Tales from the Far Territory.")
  print L("- С наживкой, приманкой или на жерлице шанс ниже (12.5%), так что всё попадается реже. Жерлицы не ловят вещи редче «Редкого».",
          "- With bait, a lure or on a tip-up the chance is lower (12.5%), so everything comes up less often. Tip-ups catch nothing rarer than Rare.")
  print ""
  total = 0; for (i = 1; i <= nc; i++) total += share[order[i]]
  print L("## Доли категорий", "## Category shares")
  print ""
  print L("| Категория | Доля | 1 раз на N рыб | ≈ раз в N дней |", "| Category | Share | Once per N fish | ≈ once per N days |")
  print "|---|---|---|---|"
  for (i = 1; i <= nc; i++) { c = order[i]; s = share[c] / 100; printf "| %s | %d%% | %s | %s |\n", C(c), share[c], fmtN(1 / (base * s)), fmtD(1 / (top * s) / fishPerDay) }
  printf "| **%s** | **%d%%** | | |\n\n", L("Итого", "Total"), total
  print L("## Предметы", "## Items")
  for (i = 1; i <= nc; i++) {
    c = order[i]
    printf "\n### %s — %d%%\n\n", C(c), share[c]
    printf "| %s | %s |\n", NAMEHEAD(), L("Редкость | Шанс в категории | 1 раз на N рыб | ≈ раз в N дней | Имя в игре",
                                        "Rarity | Chance in category | Once per N fish | ≈ once per N days | Game name")
    printf "|%s|---|---|---|---|---|\n", NAMESEP()
    for (j = 1; j <= n; j++) if (cat[j] == c) {
      p = w[tier[j]] / sum[c]; s = share[c] / 100 * p
      printf "| %s%s | %s | %.1f%% | %s | %s | `%s` |\n", (added[j] ? "➕ " : ""), NAME(en[j], ru[j]), TR(tier[j]), p * 100,
             fmtN(1 / (base * s)), fmtD(1 / (top * s) / fishPerDay), gear[j]
    }
  }
  print ""
  print L("## Добавленные предметы (➕)", "## Added items (➕)")
  print ""
  print L("Предметы, которых нет в Beachcombing. Они уже учтены в категориях выше. В настройках их можно выключить все сразу или по одному.",
          "Items that are not part of Beachcombing. They are already counted in the categories above. In the settings they can be turned off all at once or one by one.")
  print ""
  printf "| %s | %s |\n", NAMEHEAD(), L("Категория | Редкость | Имя в игре", "Category | Rarity | Game name")
  printf "|%s|---|---|---|\n", NAMESEP()
  for (j = 1; j <= n; j++) if (added[j]) printf "| %s | %s | %s | `%s` |\n", NAME(en[j], ru[j]), C(cat[j]), TR(tier[j]), gear[j]
  print ""
  print L("## Удалённые предметы (0%, можно включить в настройках)", "## Removed items (0%, can be turned on in the settings)")
  print ""
  print L("Предметы из Beachcombing, которые по умолчанию не попадаются. Если включить предмет, он вернётся в свою категорию с указанной редкостью.",
          "Beachcombing items that don't come up by default. A turned-on item goes back to its category with the rarity shown.")
  print ""
  printf "| %s | %s |\n", NAMEHEAD(), L("Имя в игре | Категория | Редкость | Почему убран", "Game name | Category | Rarity | Why")
  printf "|%s|---|---|---|---|\n", NAMESEP()
  for (k = 1; k <= rn; k++) {
    split(rline[k], f, "\t")
    printf "| %s | `%s` | %s | %s | %s |\n", NAME(f[1], f[2]), f[3], C(f[4]), TR(f[5]), L(f[6], f[7])
  }
  print ""
  print L("Не предметы, а объекты окружения, поэтому выловить их нельзя: лодки, каноэ, весло, поддоны, доски, туши оленя и волка.",
          "These are world objects, not items, so they can't be caught: boats, canoes, the paddle, pallets, planks, deer and wolf carcasses.")
  print ""
  print L("## Рассмотрено и не добавлено", "## Considered and not added")
  print ""
  printf "| %s | %s |\n", NAMEHEAD(), L("Почему", "Why")
  printf "|%s|---|\n", NAMESEP()
  for (k = 1; k <= jn; k++) { split(jline[k], f, "\t"); printf "| %s | %s |\n", NAME(f[1], f[2]), L(f[3], f[4]) }
}
function fmtN(x,   r) {
  if (x < 100) return sprintf("%d", int(x + 0.5))
  if (x < 1000) return sprintf("%d", int(x / 10 + 0.5) * 10)
  r = int(x / 100 + 0.5) * 100
  return sprintf(lang == "en" ? "%d,%03d" : "%d %03d", int(r / 1000), r % 1000)
}
function fmtD(x,   r) {
  if (x < 1) return "< 1"
  if (x < 10) return sprintf("%.1f", x)
  if (x < 1000) return sprintf("%d", int(x + 0.5))
  r = int(x / 10 + 0.5) * 10
  return sprintf(lang == "en" ? "%d,%03d" : "%d %03d", int(r / 1000), r % 1000)
}
