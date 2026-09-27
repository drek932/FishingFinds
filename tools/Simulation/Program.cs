using System.Reflection;

// Runs FishingFinds' own item drawing (LootRoller.Pick) on the built-in table and compares the result
// with the rates the table promises: category share × tier weight / sum of weights in the category.
// Usage: dotnet run -c Release [draws]   (build the mod in Release first)

int draws = args.Length > 0 ? int.Parse(args[0]) : 1_000_000;
var asm = typeof(FishingFinds.Main).Assembly;
Type T(string name) => asm.GetType("FishingFinds." + name);
object Prop(object o, string name) => o.GetType().GetProperty(name).GetValue(o);

var data = Activator.CreateInstance(T("LootData"), true);
T("LootData").GetProperty("Categories").SetValue(data, T("DefaultTable").GetMethod("Categories").Invoke(null, null));
var items = ((System.Collections.IEnumerable)T("DefaultTable").GetMethod("Items").Invoke(null, null)).Cast<object>().ToList();
var itemList = Activator.CreateInstance(typeof(List<>).MakeGenericType(T("ItemData")));
foreach (var i in items) itemList.GetType().GetMethod("Add").Invoke(itemList, new[] { i });
T("LootData").GetProperty("Items").SetValue(data, itemList);

var pick = T("LootRoller").GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Single(m => m.Name == "Pick");
var weight = T("LootRoller").GetMethod("Weight");
var maxTier = Enum.Parse(T("Tier"), "Exceptional");

foreach (var hardMode in new[] { false, true })
{
    var config = Activator.CreateInstance(T("Config"), true);
    T("Config").GetProperty("HardModeLoot").SetValue(config, hardMode);
    var effTier = T("Config").GetMethod("EffectiveTier");
    var share = T("Config").GetMethod("Share");

    // Expected rate of each item (hard mode: blocked items become fish, the rest keep their rate).
    var cats = ((System.Collections.IEnumerable)Prop(data, "Categories")).Cast<object>().ToList();
    double shareSum = cats.Sum(c => (double)(int)share.Invoke(config, new[] { c }));
    var expected = new Dictionary<string, double>();
    foreach (var c in cats)
    {
        var inCat = items.Where(i => (string)Prop(i, "Category") == (string)Prop(c, "Id")).ToList();
        double w = inCat.Sum(i => (double)(float)weight.Invoke(null, new[] { effTier.Invoke(config, new[] { i }) }));
        foreach (var i in inCat)
            expected[(string)Prop(i, "NameEn")] = (int)share.Invoke(config, new[] { c }) / shareSum *
                (float)weight.Invoke(null, new[] { effTier.Invoke(config, new[] { i }) }) / w;
    }

    var got = new Dictionary<string, int>();
    int blocked = 0, blockedFlagged = 0;
    for (int n = 0; n < draws; n++)
    {
        var a = new object[] { config, data, maxTier, new HashSet<string>(), null, null };
        var item = pick.Invoke(null, a);
        var name = (string)Prop(item, "NameEn");
        if ((bool)a[5]) { blocked++; if ((bool)Prop(item, "HighDifficulty")) blockedFlagged++; continue; }
        got[name] = got.GetValueOrDefault(name) + 1;
    }

    double worst = 0; string worstName = "";
    foreach (var (name, p) in expected)
    {
        if (p < 0.002) continue; // too rare to measure with this many draws
        bool flagged = hardMode && (bool)Prop(items.First(i => (string)Prop(i, "NameEn") == name), "HighDifficulty");
        double rate = (double)got.GetValueOrDefault(name) / draws;
        double dev = flagged ? rate : Math.Abs(rate / p - 1);
        if (dev > worst) { worst = dev; worstName = name; }
    }
    Console.WriteLine($"hardMode={hardMode}: {draws:N0} draws, fish instead of item {100.0 * blocked / draws:0.00}% " +
                      $"(all flagged: {blocked == blockedFlagged}), worst item deviation {worst:P1} ({worstName})");
}
