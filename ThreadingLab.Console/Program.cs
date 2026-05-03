using System.Globalization;
using System.Text;

static readonly object ConsoleLock = new();

for (;;)
{
    Console.WriteLine();
    Console.WriteLine("=== Багатопотоковість (консоль) ===");
    Console.WriteLine("1 — Завдання 1: один потік, числа від 0 до 50");
    Console.WriteLine("2 — Завдання 2: один потік, діапазон задає користувач");
    Console.WriteLine("3 — Завдання 3: кілька потоків і діапазон задає користувач");
    Console.WriteLine("4 — Завдання 4: 10000 чисел — максимум, мінімум, середнє (окремі потоки)");
    Console.WriteLine("5 — Завдання 5: як 4 + окремий потік записує числа й результати у файл");
    Console.WriteLine("0 — Вихід");
    Console.Write("Вибір: ");
    var k = Console.ReadLine()?.Trim();
    if (k == "0") break;
    switch (k)
    {
        case "1": Task1(); break;
        case "2": Task2(); break;
        case "3": Task3(); break;
        case "4": Task4(writeFile: false); break;
        case "5": Task4(writeFile: true); break;
        default: SafeWriteLine("Невідома команда."); break;
    }
}

static void SafeWriteLine(string s)
{
    lock (ConsoleLock) Console.WriteLine(s);
}

static int ReadInt(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        var t = Console.ReadLine()?.Trim();
        if (int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)) return v;
        SafeWriteLine("Введіть ціле число.");
    }
}

static void Task1()
{
    var th = new Thread(() =>
    {
        for (var i = 0; i <= 50; i++) SafeWriteLine(i.ToString(CultureInfo.InvariantCulture));
    }) { IsBackground = false };
    th.Start();
    th.Join();
    SafeWriteLine("Завдання 1 завершено.");
}

static void Task2()
{
    var a = ReadInt("Початок діапазону: ");
    var b = ReadInt("Кінець діапазону: ");
    var lo = Math.Min(a, b);
    var hi = Math.Max(a, b);
    var th = new Thread(() =>
    {
        for (var i = lo; i <= hi; i++) SafeWriteLine(i.ToString(CultureInfo.InvariantCulture));
    }) { IsBackground = false };
    th.Start();
    th.Join();
    SafeWriteLine("Завдання 2 завершено.");
}

static void Task3()
{
    var a = ReadInt("Початок діапазону: ");
    var b = ReadInt("Кінець діапазону: ");
    var lo = Math.Min(a, b);
    var hi = Math.Max(a, b);
    var n = ReadInt("Кількість потоків (>=1): ");
    if (n < 1)
    {
        SafeWriteLine("Кількість потоків має бути не менше 1.");
        return;
    }
    var range = hi - lo + 1;
    var chunk = (range + n - 1) / n;
    var threads = new List<Thread>();
    for (var ti = 0; ti < n; ti++)
    {
        var start = lo + ti * chunk;
        var end = Math.Min(hi, start + chunk - 1);
        if (start > hi) break;
        var s = start;
        var e = end;
        var id = ti + 1;
        var th = new Thread(() =>
        {
            for (var i = s; i <= e; i++)
                SafeWriteLine($"[потік {id}] {i.ToString(CultureInfo.InvariantCulture)}");
        }) { IsBackground = false };
        threads.Add(th);
    }
    foreach (var t in threads) t.Start();
    foreach (var t in threads) t.Join();
    SafeWriteLine("Завдання 3 завершено.");
}

static void Task4(bool writeFile)
{
    var rnd = new Random();
    var arr = new int[10000];
    for (var i = 0; i < arr.Length; i++) arr[i] = rnd.Next(int.MinValue / 4, int.MaxValue / 4);

    var maxHolder = new int[1];
    var minHolder = new int[1];
    var meanHolder = new double[1];

    var tMax = new Thread(() =>
    {
        var m = arr[0];
        for (var i = 1; i < arr.Length; i++)
            if (arr[i] > m) m = arr[i];
        maxHolder[0] = m;
    }) { IsBackground = false };

    var tMin = new Thread(() =>
    {
        var m = arr[0];
        for (var i = 1; i < arr.Length; i++)
            if (arr[i] < m) m = arr[i];
        minHolder[0] = m;
    }) { IsBackground = false };

    var tMean = new Thread(() =>
    {
        long s = 0;
        foreach (var x in arr) s += x;
        meanHolder[0] = (double)s / arr.Length;
    }) { IsBackground = false };

    tMax.Start();
    tMin.Start();
    tMean.Start();
    tMax.Join();
    tMin.Join();
    tMean.Join();

    SafeWriteLine($"Максимум: {maxHolder[0].ToString(CultureInfo.InvariantCulture)}");
    SafeWriteLine($"Мінімум: {minHolder[0].ToString(CultureInfo.InvariantCulture)}");
    SafeWriteLine($"Середнє арифметичне: {meanHolder[0].ToString("G17", CultureInfo.InvariantCulture)}");

    if (!writeFile) { SafeWriteLine("Завдання 4 завершено."); return; }

    Console.Write("Шлях до файлу (Enter — за замовчуванням у каталозі програми): ");
    var path = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(path))
        path = Path.Combine(AppContext.BaseDirectory, "stats_output.txt");

    var fileThread = new Thread(() =>
    {
        var sb = new StringBuilder();
        sb.AppendLine("Набір чисел (10000 елементів):");
        for (var i = 0; i < arr.Length; i++)
        {
            sb.Append(arr[i].ToString(CultureInfo.InvariantCulture));
            if (i < arr.Length - 1) sb.Append(',');
            if ((i + 1) % 20 == 0) sb.AppendLine();
        }
        if (arr.Length % 20 != 0) sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("Результати:");
        sb.Append("Максимум: ").AppendLine(maxHolder[0].ToString(CultureInfo.InvariantCulture));
        sb.Append("Мінімум: ").AppendLine(minHolder[0].ToString(CultureInfo.InvariantCulture));
        sb.Append("Середнє арифметичне: ").AppendLine(meanHolder[0].ToString("G17", CultureInfo.InvariantCulture));
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }) { IsBackground = false };

    fileThread.Start();
    fileThread.Join();
    SafeWriteLine($"Файл записано: {path}");
    SafeWriteLine("Завдання 5 завершено.");
}
