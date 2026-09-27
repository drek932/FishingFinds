using System.Text.RegularExpressions;
using ClosedXML.Excel;

// Builds docs/FishingFinds-loot.xlsx from docs/data/*.tsv: every item with its rarity and rate, as live formulas
// (change a share or a rarity in the workbook and all rates update). Ends with a self-check of the formulas.
// Usage (from this folder): dotnet run -c Release [dataDir] [output.xlsx]
// Invariant culture: ClosedXML's evaluator misreads criteria like ">0" under a comma-decimal culture.
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
var dataDir = args.Length > 0 ? args[0] : Path.Combine("..", "..", "docs", "data");
var output = args.Length > 1 ? args[1] : Path.Combine("..", "..", "docs", "FishingFinds-loot.xlsx");

string[] Lines(string f) => File.ReadAllLines(Path.Combine(dataDir, f)).Where(l => l.Trim().Length > 0).ToArray();
var hd = new HashSet<string>(Lines("high_difficulty.txt").Select(l => l.Trim()));
var tierOrder = new[] { "Частый", "Обычный", "Редкий", "Очень редкий", "Исключительный" };
var tierWeight = new[] { 10.0, 5, 2, 0.5, 0.1 };

var items = new List<string[]>();
var categories = new List<(string name, int share)>();
foreach (var (file, origin) in new[] { ("beachcombing.tsv", "Beachcombing"), ("added.tsv", "Добавлен модом") })
    foreach (var l in Lines(file))
    {
        var c = l.Split('\t');
        if (!categories.Any(x => x.name == c[0])) categories.Add((c[0], int.Parse(c[1])));
        items.Add(new[] { c[0], c[2], c[3], c[4], c[5], origin });
    }
items = items.OrderBy(i => categories.FindIndex(c => c.name == i[0])).ThenBy(i => Array.IndexOf(tierOrder, i[1])).ToList();

string Clean(string ru) => Regex.Replace(ru, @" \(DLC\)| \(шутка\)|, 3 вида", "");
bool IsDlc(string ru) => ru.Contains("(DLC)");

var font = "Arial";
var input = XLColor.FromArgb(0, 0, 255);
var headerFill = XLColor.FromArgb(221, 229, 240);

using var wb = new XLWorkbook();
wb.Style.Font.FontName = font;
wb.Style.Font.FontSize = 10;

void Header(IXLWorksheet ws, params string[] names)
{
    for (int i = 0; i < names.Length; i++)
    {
        var cell = ws.Cell(1, i + 1);
        cell.Value = names[i];
        cell.Style.Font.Bold = true;
        cell.Style.Fill.BackgroundColor = headerFill;
        cell.Style.Alignment.WrapText = true;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
    }
    ws.Row(1).Height = 42;
    ws.SheetView.FreezeRows(1);
}

// ---------- Параметры ----------
var p = wb.AddWorksheet("Параметры");
p.Cell("A1").Value = "FishingFinds — параметры расчёта";
p.Cell("A1").Style.Font.Bold = true; p.Cell("A1").Style.Font.FontSize = 12;
p.Cell("A3").Value = "Базовый шанс замены рыбы на предмет (1 уровень рыбалки)"; p.Cell("B3").Value = 0.25;
p.Cell("A4").Value = "Бонус к шансу за каждый уровень выше первого"; p.Cell("B4").Value = 0.05;
p.Cell("A5").Value = "Уровень рыбалки для колонки «≈ дней»"; p.Cell("B5").Value = 5;
p.Cell("A6").Value = "Шанс замены на этом уровне"; p.Cell("B6").FormulaA1 = "MIN(1,B3+B4*(B5-1))";
p.Cell("A7").Value = "Рыб за 12 часов непрерывной рыбалки на этом уровне"; p.Cell("B7").Value = 25;
p.Cell("A8").Value = "Предметов за день рыбалки (12 ч)"; p.Cell("B8").FormulaA1 = "B6*B7";
p.Cell("A9").Value = "Как на Interloper/Misery (да/нет)"; p.Cell("B9").Value = "нет";
p.Cell("B9").CreateDataValidation().List("\"да,нет\"");
p.Cell("C9").Value = "«да» — предметы с пометкой «Нет на Interloper/Misery» не выпадают (вместо них рыба)";
foreach (var a in new[] { "B3", "B4", "B5", "B7", "B9" }) p.Cell(a).Style.Font.FontColor = input;
foreach (var a in new[] { "B3", "B4", "B6" }) p.Cell(a).Style.NumberFormat.Format = "0%";
p.Cell("B8").Style.NumberFormat.Format = "0.0";
p.Cell("C3").Value = "Значение мода по умолчанию";
p.Cell("C4").Value = "Значение мода по умолчанию";
p.Cell("C7").Value = "Оценка игрока: 20–30 рыб за 12 ч на 5 уровне";
p.Cell("A10").Value = "Как пользоваться";
p.Cell("A10").Style.Font.Bold = true;
p.Cell("A11").Value = "Синие ячейки можно менять: параметры здесь, «Доля (вес)» на листе «Категории», «Уровень» и веса на листах «Предметы» и «Уровни». Остальное пересчитается само.";
p.Cell("A12").Value = "Выбор предмета: сначала категория по доле, затем предмет внутри неё по весу уровня.";
p.Cell("A13").Value = "Не учтено: ограничение исключительных по уровню рыбалки (в моде по умолчанию — с 3 уровня) и отсутствующие DLC.";
p.Cell("A14").Value = "Рыбалка с наживкой или приманкой всегда даёт рыбу.";
p.Column(1).Width = 62; p.Column(2).Width = 10; p.Column(3).Width = 44;

// ---------- Уровни ----------
var t = wb.AddWorksheet("Уровни");
Header(t, "Уровень", "Вес");
for (int i = 0; i < tierOrder.Length; i++)
{
    t.Cell(i + 2, 1).Value = tierOrder[i];
    t.Cell(i + 2, 2).Value = tierWeight[i];
    t.Cell(i + 2, 2).Style.Font.FontColor = input;
}
t.Cell(7, 1).Value = "Выключен"; t.Cell(7, 2).Value = 0;
t.Cell(9, 1).Value = "Вес задаёт, во сколько раз чаще предмет выпадает по сравнению с соседями по категории.";
t.Column(1).Width = 18; t.Column(2).Width = 8;

// ---------- Предметы ----------
var s = wb.AddWorksheet("Предметы");
Header(s, "Предмет", "Перевод", "Категория", "Уровень", "Откуда", "DLC", "Нет на Interloper/ Misery", "Имя в игре",
    "Вес", "Шанс внутри категории", "Доля всего улова", "1 раз на N предметов", "1 раз на N рыб (1 уровень)", "≈ раз в N дней рыбалки (уровень из «Параметров»)");
int nItems = items.Count, last = nItems + 1;
for (int i = 0; i < nItems; i++)
{
    var it = items[i]; int r = i + 2;
    var gear = it[4].Split(',');
    s.Cell(r, 1).Value = it[2];
    s.Cell(r, 2).Value = Clean(it[3]);
    s.Cell(r, 3).Value = it[0];
    s.Cell(r, 4).Value = it[1]; s.Cell(r, 4).Style.Font.FontColor = input;
    s.Cell(r, 5).Value = it[5];
    s.Cell(r, 6).Value = IsDlc(it[3]) ? "да" : "";
    s.Cell(r, 7).Value = hd.Contains(gear[0]) ? "да" : "";
    s.Cell(r, 8).Value = string.Join(", ", gear);
    s.Cell(r, 9).FormulaA1 = $"IFERROR(INDEX(Уровни!$B$2:$B$7,MATCH(D{r},Уровни!$A$2:$A$7,0)),0)";
    s.Cell(r, 10).FormulaA1 = $"IF(I{r}=0,0,I{r}/SUMIFS($I$2:$I${last},$C$2:$C${last},C{r}))";
    s.Cell(r, 11).FormulaA1 = $"J{r}*IFERROR(INDEX(Категории!$C$2:$C$20,MATCH(C{r},Категории!$A$2:$A$20,0)),0)*IF(AND(Параметры!$B$9=\"да\",G{r}=\"да\"),0,1)";
    s.Cell(r, 12).FormulaA1 = $"IF(K{r}=0,\"—\",1/K{r})";
    s.Cell(r, 13).FormulaA1 = $"IF(K{r}=0,\"—\",1/(K{r}*Параметры!$B$3))";
    s.Cell(r, 14).FormulaA1 = $"IF(K{r}=0,\"—\",1/(K{r}*Параметры!$B$8))";
}
s.Range(2, 4, last, 4).CreateDataValidation().List("=Уровни!$A$2:$A$7");
s.Range(2, 9, last, 9).Style.NumberFormat.Format = "0.0";
s.Range(2, 10, last, 10).Style.NumberFormat.Format = "0.0%";
s.Range(2, 11, last, 11).Style.NumberFormat.Format = "0.00%";
s.Range(2, 12, last, 13).Style.NumberFormat.Format = "#,##0";
s.Range(2, 14, last, 14).Style.NumberFormat.Format = "#,##0.0";
s.Range(1, 1, last, 14).SetAutoFilter();
int[] widths = { 30, 34, 28, 15, 15, 6, 11, 38, 6, 10, 10, 11, 11, 16 };
for (int i = 0; i < widths.Length; i++) s.Column(i + 1).Width = widths[i];

// ---------- Категории ----------
var c2 = wb.AddWorksheet("Категории");
Header(c2, "Категория", "Доля (вес)", "Доля, %", "Предметов", "Сумма весов", "«Обычный» предмет, % улова", "Любой предмет категории: 1 раз на N рыб (1 уровень)", "Любой предмет категории: ≈ раз в N дней");
int nCat = categories.Count, lastCat = nCat + 1;
for (int i = 0; i < nCat; i++)
{
    int r = i + 2;
    c2.Cell(r, 1).Value = categories[i].name;
    c2.Cell(r, 2).Value = categories[i].share; c2.Cell(r, 2).Style.Font.FontColor = input;
    c2.Cell(r, 3).FormulaA1 = $"B{r}/SUM($B$2:$B${lastCat})";
    c2.Cell(r, 4).FormulaA1 = $"COUNTIFS(Предметы!$C$2:$C${last},A{r},Предметы!$I$2:$I${last},\">0\")";
    c2.Cell(r, 5).FormulaA1 = $"SUMIFS(Предметы!$I$2:$I${last},Предметы!$C$2:$C${last},A{r})";
    c2.Cell(r, 6).FormulaA1 = $"IF(E{r}=0,0,C{r}*INDEX(Уровни!$B$2:$B$7,MATCH(\"Обычный\",Уровни!$A$2:$A$7,0))/E{r})";
    c2.Cell(r, 7).FormulaA1 = $"IF(C{r}=0,\"—\",1/(C{r}*Параметры!$B$3))";
    c2.Cell(r, 8).FormulaA1 = $"IF(C{r}=0,\"—\",1/(C{r}*Параметры!$B$8))";
}
int tr = lastCat + 1;
c2.Cell(tr, 1).Value = "Итого"; c2.Cell(tr, 1).Style.Font.Bold = true;
c2.Cell(tr, 2).FormulaA1 = $"SUM(B2:B{lastCat})";
c2.Cell(tr, 3).FormulaA1 = $"SUM(C2:C{lastCat})";
c2.Cell(tr, 4).FormulaA1 = $"SUM(D2:D{lastCat})";
c2.Range(2, 3, tr, 3).Style.NumberFormat.Format = "0.0%";
c2.Range(2, 5, tr, 5).Style.NumberFormat.Format = "0.0";
c2.Range(2, 6, tr, 6).Style.NumberFormat.Format = "0.00%";
c2.Range(2, 7, tr, 7).Style.NumberFormat.Format = "#,##0";
c2.Range(2, 8, tr, 8).Style.NumberFormat.Format = "#,##0.0";
c2.Cell(tr + 2, 1).Value = "Доли не обязаны давать в сумме 100: считается соотношение.";
int[] cw = { 30, 10, 9, 10, 10, 13, 18, 16 };
for (int i = 0; i < cw.Length; i++) c2.Column(i + 1).Width = cw[i];

// ---------- Удалённые / Отклонённые ----------
var rm = wb.AddWorksheet("Удалённые");
Header(rm, "Предмет", "Перевод", "Имя в игре", "Родная категория", "Уровень, если включить", "Почему удалён");
var removed = Lines("removed.tsv");
for (int i = 0; i < removed.Length; i++)
{
    var c = removed[i].Split('\t');
    for (int k = 0; k < 6; k++) rm.Cell(i + 2, k + 1).Value = k == 1 ? Clean(c[1]) : c[k];
}
rm.Cell(removed.Length + 3, 1).Value = "По умолчанию не выпадают (0%). Каждый можно включить в настройках мода — тогда он вернётся в родную категорию с указанным уровнем.";
int[] rw = { 26, 30, 30, 28, 14, 34 };
for (int i = 0; i < rw.Length; i++) rm.Column(i + 1).Width = rw[i];

var rj = wb.AddWorksheet("Отклонённые");
Header(rj, "Предмет", "Перевод", "Почему не добавлен");
var rejected = Lines("rejected.tsv");
for (int i = 0; i < rejected.Length; i++)
{
    var c = rejected[i].Split('\t');
    for (int k = 0; k < 3; k++) rj.Cell(i + 2, k + 1).Value = c[k];
}
rj.Column(1).Width = 46; rj.Column(2).Width = 44; rj.Column(3).Width = 44;

// ---------- По категориям: one block per category, linked to "Предметы" ----------
var bc = wb.AddWorksheet("По категориям");
var tierFill = new Dictionary<string, XLColor>
{
    ["Частый"] = XLColor.FromArgb(214, 234, 248),
    ["Обычный"] = XLColor.FromArgb(226, 239, 218),
    ["Редкий"] = XLColor.FromArgb(255, 242, 204),
    ["Очень редкий"] = XLColor.FromArgb(252, 228, 214),
    ["Исключительный"] = XLColor.FromArgb(234, 209, 220),
};
string[] blockHeader = { "Предмет", "Item", "Уровень", "Шанс внутри категории", "Доля всего улова", "≈ раз в N дней рыбалки" };
int row = 1;
bc.Cell(row, 1).Value = "Все предметы по категориям. Значения берутся с листа «Предметы» — меняйте уровни там или доли на листе «Категории».";
bc.Cell(row, 1).Style.Font.Italic = true;
row += 2;
for (int ci = 0; ci < nCat; ci++)
{
    var cat = categories[ci].name;
    int catRow = ci + 2;
    bc.Cell(row, 1).Value = cat;
    bc.Cell(row, 1).Style.Font.Bold = true; bc.Cell(row, 1).Style.Font.FontSize = 12;
    bc.Cell(row, 2).Value = "доля категории:";
    bc.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    bc.Cell(row, 3).FormulaA1 = $"Категории!C{catRow}";
    bc.Cell(row, 3).Style.NumberFormat.Format = "0.0%";
    bc.Cell(row, 3).Style.Font.Bold = true;
    bc.Cell(row, 4).FormulaA1 = $"\"предметов: \"&Категории!D{catRow}";
    row++;
    for (int k = 0; k < blockHeader.Length; k++)
    {
        var h = bc.Cell(row, k + 1);
        h.Value = blockHeader[k];
        h.Style.Font.Bold = true;
        h.Style.Fill.BackgroundColor = headerFill;
    }
    row++;
    int first = row;
    for (int i = 0; i < nItems; i++)
    {
        if (items[i][0] != cat) continue;
        int src = i + 2;
        bc.Cell(row, 1).FormulaA1 = $"Предметы!B{src}";
        bc.Cell(row, 2).FormulaA1 = $"Предметы!A{src}";
        bc.Cell(row, 3).FormulaA1 = $"Предметы!D{src}";
        bc.Cell(row, 4).FormulaA1 = $"Предметы!J{src}";
        bc.Cell(row, 5).FormulaA1 = $"Предметы!K{src}";
        bc.Cell(row, 6).FormulaA1 = $"Предметы!N{src}";
        row++;
    }
    var tiers = bc.Range(first, 3, row - 1, 3);
    foreach (var (name, fill) in tierFill)
        tiers.AddConditionalFormat().WhenEquals($"\"{name}\"").Fill.SetBackgroundColor(fill);
    bc.Range(first, 4, row - 1, 4).Style.NumberFormat.Format = "0.0%";
    bc.Range(first, 5, row - 1, 5).Style.NumberFormat.Format = "0.00%";
    bc.Range(first, 6, row - 1, 6).Style.NumberFormat.Format = "#,##0.0";
    row += 2;
}
int[] bw = { 40, 30, 15, 12, 12, 14 };
for (int i = 0; i < bw.Length; i++) bc.Column(i + 1).Width = bw[i];
bc.Row(1).Style.Alignment.WrapText = false;

wb.Worksheet("Категории").Position = 2;
wb.Worksheet("По категориям").Position = 3;
wb.Worksheet("Предметы").Position = 4;
wb.Worksheet("Уровни").Position = 5;
wb.Worksheet("По категориям").SetTabActive();
wb.CalculateMode = XLCalculateMode.Auto;
wb.SaveAs(output);

// Self-check with ClosedXML's formula engine against a direct computation.
using var check = new XLWorkbook(output);
var sheet = check.Worksheet("Предметы");
var catShare = categories.ToDictionary(x => x.name, x => x.share / (double)categories.Sum(y => y.share));
var catSum = items.GroupBy(i => i[0]).ToDictionary(g => g.Key, g => g.Sum(i => tierWeight[Array.IndexOf(tierOrder, i[1])]));
double worst = 0; string worstName = "";
for (int r = 2; r <= last; r++)
{
    var cat = sheet.Cell(r, 3).GetString();
    var w = tierWeight[Array.IndexOf(tierOrder, sheet.Cell(r, 4).GetString())];
    double expected = catShare[cat] * w / catSum[cat];
    double got = sheet.Cell(r, 11).GetDouble();
    double d = Math.Abs(got - expected) / expected;
    if (d > worst) { worst = d; worstName = sheet.Cell(r, 1).GetString(); }
}
var cats = check.Worksheet("Категории");
Console.WriteLine($"items: {nItems}, categories: {nCat}, share total: {cats.Cell(tr, 3).GetDouble():P1}, worst formula deviation: {worst:E2} ({worstName})");
Console.WriteLine($"Hatchet share: {sheet.Search("Hatchet").First().WorksheetRow().Cell(11).GetDouble():P3}, days: {sheet.Search("Hatchet").First().WorksheetRow().Cell(14).GetDouble():F1}");
Console.WriteLine($"Params B8 (items/day): {check.Worksheet("Параметры").Cell("B8").GetDouble():F2}");

// Check the per-category sheet: every linked row must match the item it names.
var bsheet = check.Worksheet("По категориям");
int linked = 0, mismatched = 0;
foreach (var r in bsheet.RowsUsed())
{
    var f = r.Cell(2).FormulaA1;
    if (string.IsNullOrEmpty(f) || !f.StartsWith("Предметы!A")) continue;
    int src = int.Parse(f.Substring("Предметы!A".Length));
    linked++;
    bool ok = r.Cell(2).GetString() == sheet.Cell(src, 1).GetString()
           && r.Cell(3).GetString() == sheet.Cell(src, 4).GetString()
           && Math.Abs(r.Cell(5).GetDouble() - sheet.Cell(src, 11).GetDouble()) < 1e-12
           && r.Cell(1).GetString() == sheet.Cell(src, 2).GetString();
    if (!ok) mismatched++;
}
Console.WriteLine($"per-category sheet: {linked} linked rows, {mismatched} mismatches; first block header value: {bsheet.Cell(3, 3).GetDouble():P1} / {bsheet.Cell(3, 4).GetString()}");

// Hard-mode switch: flagged items must drop to 0, others unchanged.
check.Worksheet("Параметры").Cell("B9").Value = "да";
int zeroed = 0, flaggedTotal = 0, changed = 0;
for (int r = 2; r <= last; r++)
{
    bool flagged = sheet.Cell(r, 7).GetString() == "да";
    double v = sheet.Cell(r, 11).GetDouble();
    var cat = sheet.Cell(r, 3).GetString();
    var w = tierWeight[Array.IndexOf(tierOrder, sheet.Cell(r, 4).GetString())];
    double normal = catShare[cat] * w / catSum[cat];
    if (flagged) { flaggedTotal++; if (v == 0) zeroed++; }
    else if (Math.Abs(v - normal) > 1e-12) changed++;
}
Console.WriteLine($"hard-mode check: {zeroed}/{flaggedTotal} flagged items at 0%, {changed} other items changed");
