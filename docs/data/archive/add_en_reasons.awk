# One-off (2026-09-28): appends an English "reason" column to removed.tsv and rejected.tsv.
# Usage: awk -v kind=removed -f add_en_reasons.awk removed.tsv ; awk -v kind=rejected -f add_en_reasons.awk rejected.tsv
BEGIN {
  FS = OFS = "\t"
  r["размокнет"] = "would be soaked through"
  r["решено не добавлять"] = "left out by choice"
  r["решено удалить"] = "left out by choice"
  r["нет в Beachcombing по вики"] = "not in Beachcombing according to the wiki"
  r["в 2.x это жидкость"] = "a liquid in 2.x, not an item"
  r["рыба и так ловится"] = "you catch fish anyway"
  r["топливо даёт рыба"] = "fish already give fuel"
  r["легко добыть силками"] = "easy to get with snares"
  r["крафтится из ткани"] = "crafted from cloth"
  r["крафтится из палки"] = "crafted from a stick"
  r["нет"] = "left out by choice"
  r["одна пуля бесполезна, свинец и гильзы уже есть"] = "a single bullet is useless; scrap lead and casings already drop"
  r["крафтятся из того, что и так ловится"] = "crafted from things that already drop"
  r["бесполезна"] = "useless"
  r["больше одной за игру не нужно"] = "one per game is enough"
  r["тяжёлый, не пролезет в лунку"] = "too heavy, won't fit through the hole"
  r["под водой её съела бы рыба"] = "fish would have eaten it"
  r["растворилась бы"] = "would dissolve"
  r["слишком сильные"] = "too strong"
  r["сюжетные"] = "story items"
  r["легко сделать самому, всегда в избытке"] = "easy to make, always plenty"
  r["не выбрано"] = "not chosen"
  r["ключевой сюжетный предмет Tales"] = "key story item of Tales"
  r["бумажная упаковка размокнет"] = "paper packaging would be soaked through"
  r["перья убраны"] = "feathers are left out"
  r["большой, не пролезет в лунку"] = "too big for the hole"
  r["похоже на вырезанный контент"] = "looks like cut content"
}
{
  if ($1 ~ /^Handheld Shortwave/) $1 = "Handheld Shortwave, notes, keys, polaroids"
  col = (kind == "removed") ? 6 : 3
  if (!($col in r)) { print "NO TRANSLATION: " $col > "/dev/stderr"; exit 1 }
  print $0, r[$col]
}
