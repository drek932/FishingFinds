# Generates docs/loot-table.md (the human-readable loot table with rates) from the design tables.
# Usage: awk -f gen.awk beachcombing.tsv added.tsv removed.tsv rejected.tsv > ../loot-table.md
BEGIN {
  FS = "\t"
  w["Частый"] = 10; w["Обычный"] = 5; w["Редкий"] = 2; w["Очень редкий"] = 0.5; w["Исключительный"] = 0.1
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
END {
  print "# FishingFinds — итоговая таблица добычи"
  print ""
  print "Выбор в два шага: категория по доле → предмет по весу уровня внутри категории."
  print "Веса уровней: Частый 10 · Обычный 5 · Редкий 2 · Очень редкий 0.5 · Исключительный 0.1."
  print ""
  print "- **1 раз на N рыб** — при шансе замены 25% (1 уровень рыбалки)."
  print "- **≈ дней на 5 ур.** — при шансе 45% (25% + 5% за каждый уровень выше первого) и 25 рыбах за 12 часов непрерывной рыбалки; «день» = 12 часов рыбалки."
  print "- Всё считается для улова без наживки и приманки. ➕ — добавленный предмет (не из Beachcombing). DLC — Tales from the Far Territory."
  print ""
  total = 0; for (i = 1; i <= nc; i++) total += share[order[i]]
  print "## Доли категорий"
  print ""
  print "| Категория | Доля | 1 раз на N рыб | ≈ дней на 5 ур. |"
  print "|---|---|---|---|"
  for (i = 1; i <= nc; i++) { c = order[i]; s = share[c] / 100; printf "| %s | %d%% | %s | %s |\n", c, share[c], fmtN(1 / (base * s)), fmtD(1 / (top * s) / fishPerDay) }
  printf "| **Итого** | **%d%%** | | |\n\n", total
  print "## Предметы"
  for (i = 1; i <= nc; i++) {
    c = order[i]
    printf "\n### %s — %d%%\n\n", c, share[c]
    print "| Предмет | Перевод | Уровень | Шанс в категории | 1 раз на N рыб | ≈ дней на 5 ур. | Имя в игре |"
    print "|---|---|---|---|---|---|---|"
    for (j = 1; j <= n; j++) if (cat[j] == c) {
      p = w[tier[j]] / sum[c]; s = share[c] / 100 * p
      printf "| %s%s | %s | %s | %.1f%% | %s | %s | `%s` |\n", (added[j] ? "➕ " : ""), en[j], ru[j], tier[j], p * 100, fmtN(1 / (base * s)), fmtD(1 / (top * s) / fishPerDay), gear[j]
    }
  }
  print ""
  print "## Добавленное (➕)"
  print ""
  print "Предметы не из Beachcombing. Они уже учтены в категориях выше. В настройках: «Выключить все добавленные» и переключатель на каждый предмет."
  print ""
  print "| Предмет | Перевод | Категория | Уровень | Имя в игре |"
  print "|---|---|---|---|---|"
  for (j = 1; j <= n; j++) if (added[j]) printf "| %s | %s | %s | %s | `%s` |\n", en[j], ru[j], cat[j], tier[j], gear[j]
  print ""
  print "## Удалённое (0%, можно включить в настройках)"
  print ""
  print "Если предмет включить, он возвращается в «родную» категорию со своим уровнем."
  print ""
  print "| Предмет | Перевод | Имя в игре | Родная категория | Уровень | Почему удалён |"
  print "|---|---|---|---|---|---|"
  for (k = 1; k <= rn; k++) { split(rline[k], f, "\t"); printf "| %s | %s | `%s` | %s | %s | %s |\n", f[1], f[2], f[3], f[4], f[5], f[6] }
  print ""
  print "Не могут быть предметами (объекты окружения): лодки, каноэ, весло, поддоны, доски, туши оленя и волка."
  print ""
  print "## Рассмотрено и не добавлено"
  print ""
  print "| Предмет | Перевод | Почему |"
  print "|---|---|---|"
  for (k = 1; k <= jn; k++) { split(jline[k], f, "\t"); printf "| %s | %s | %s |\n", f[1], f[2], f[3] }
}
function fmtN(x,   r) {
  if (x < 100) return sprintf("%d", int(x + 0.5))
  if (x < 1000) return sprintf("%d", int(x / 10 + 0.5) * 10)
  r = int(x / 100 + 0.5) * 100
  return sprintf("%d %03d", int(r / 1000), r % 1000)
}
function fmtD(x,   r) {
  if (x < 1) return "< 1"
  if (x < 10) return sprintf("%.1f", x)
  if (x < 1000) return sprintf("%d", int(x + 0.5))
  r = int(x / 10 + 0.5) * 10
  return sprintf("%d %03d", int(r / 1000), r % 1000)
}
