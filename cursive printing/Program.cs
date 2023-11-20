using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using NUnit.Framework;

[Serializable]
public class UserData
{
    public string Name { get; set; }
    public int CharactersPerMinute { get; set; }
    public int CharactersPerSecond { get; set; }
}

public static class RecordsTable
{
    public static string RecordsFilePath { get; set; } = "records.json";
    private static List<UserData> records = new List<UserData>();

    static RecordsTable()
    {
        LoadRecords();
    }

    public static void AddRecord(UserData user)
    {
        records.Add(user);
        SaveRecords();
    }

    public static List<UserData> GetRecords()
    {
        return records.OrderByDescending(r => r.CharactersPerMinute).ToList();
    }

    public static void LoadRecords()
    {
        if (System.IO.File.Exists(RecordsFilePath))
        {
            var json = System.IO.File.ReadAllText(RecordsFilePath);
            records = Newtonsoft.Json.JsonConvert.DeserializeObject<List<UserData>>(json);
        }
        else
        {
            records = new List<UserData>();
        }
    }

    public static void SaveRecords()
    {
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(records);
        System.IO.File.WriteAllText(RecordsFilePath, json);
    }
}

public class TypingTest
{
    private static readonly List<string> TestTexts = new List<string>
    {
        "Соображения высшего порядка, а также повышение уровня гражданского сознания обеспечивает актуальность позиций, занимаемых участниками в отношении поставленных задач. Задача организации, в особенности же социально-экономическое развитие требует от нас анализа системы обучения кадров, соответствующей насущным потребностям!",
        "Мы вынуждены отталкиваться от того, что выбранный нами инновационный путь играет важную роль в формировании кластеризации усилий. Кстати, акционеры крупнейших компаний освещают чрезвычайно интересные особенности картины в целом, однако конкретные выводы, разумеется, своевременно верифицированы.",
        "Значимость этих проблем настолько очевидна, что сплочённость команды профессионалов играет важную роль в формировании модели развития. С другой стороны, реализация намеченных плановых заданий позволяет выполнить важные задания по разработке направлений прогрессивного развития."
    };

    private static CancellationTokenSource cts;

    public static void RunTest()
    {
        Console.OutputEncoding = Encoding.UTF8;

        Console.Write("Введите ваше имя: ");
        string playerName = Console.ReadLine();

        var random = new Random();
        int randomIndex = random.Next(TestTexts.Count);
        string testText = TestTexts[randomIndex];

        Console.Clear();
        Console.WriteLine($"Добро пожаловать, {playerName}! Нажмите Enter, когда будете готовы.");
        Console.ReadLine();

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        Console.Clear();
        Console.WriteLine($"Начинайте печатать:\n");

        int originalTop = Console.CursorTop;
        int originalLeft = Console.CursorLeft;

        Console.Write(testText);

        cts = new CancellationTokenSource();

        Task.Run(() => UpdateTimer(stopwatch, originalLeft, originalTop), cts.Token);

        int charactersTyped = 0;
        int currentLeft = originalLeft;
        int currentTop = originalTop;

        while (charactersTyped < testText.Length)
        {
            Console.SetCursorPosition(currentLeft, currentTop);

            ConsoleKeyInfo keyInfo = Console.ReadKey(true);

            if (keyInfo.KeyChar == testText[charactersTyped])
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(testText[charactersTyped]);
                Console.ResetColor();
                charactersTyped++;

                currentLeft++;
            }
            else
            {
                Console.ResetColor();
            }

           
            if (currentLeft >= Console.WindowWidth)
            {
                currentLeft = originalLeft;
                currentTop++;
            }
        }

        stopwatch.Stop();
        cts.Cancel();

        int charactersPerMinute = (int)(charactersTyped / stopwatch.Elapsed.TotalMinutes);
        int charactersPerSecond = (int)(charactersTyped / stopwatch.Elapsed.TotalSeconds);

        Console.WriteLine($"\nТест завершен! Символов в минуту: {charactersPerMinute}, Символов в секунду: {charactersPerSecond}");
        Console.WriteLine($"Время: {stopwatch.Elapsed.TotalSeconds:F2} сек");

        UserData userData = new UserData
        {
            Name = playerName,
            CharactersPerMinute = charactersPerMinute,
            CharactersPerSecond = charactersPerSecond
        };

        RecordsTable.AddRecord(userData);

        DisplayRecords();
    }

    public static void UpdateTimer(Stopwatch stopwatch, int originalLeft, int originalTop)
    {
        while (!cts.Token.IsCancellationRequested)
        {
            TimeSpan remainingTime = TimeSpan.FromSeconds(180 - stopwatch.Elapsed.TotalSeconds);

            if (remainingTime.TotalSeconds <= 0)
            {
                Console.SetCursorPosition(originalLeft, originalTop + 2);
                Console.Write("Время вышло! Тест завершен.");
                cts.Cancel();
                DisplayRecords();

                break;
            }

            int currentLeft = Console.CursorLeft;
            int currentTop = Console.CursorTop;

            Console.SetCursorPosition(originalLeft, originalTop + 3);

            Console.Write($"Оставшееся время: {remainingTime.Minutes:D2}:{remainingTime.Seconds:D2}  ");

            Console.SetCursorPosition(currentLeft, currentTop);

            Thread.Sleep(1000);
        }

        Console.SetCursorPosition(originalLeft, originalTop);
    }

    public static void DisplayRecords()
    {
        Console.WriteLine("\nТаблица рекордов:");
        var records = RecordsTable.GetRecords();
        foreach (var record in records)
        {
            Console.WriteLine($"{record.Name} - {record.CharactersPerMinute} Символов/Мин, {record.CharactersPerSecond} Символов/Сек");
        }
    }
}

[TestFixture]
public class TypingTestTests
{
    private const string TestFilePath = "test_records.json";

    [SetUp]
    public void SetUp()
    {
        RecordsTable.RecordsFilePath = TestFilePath;
        File.WriteAllText(TestFilePath, "[]");
    }

    [TearDown]
    public void TearDown()
    {
        RecordsTable.RecordsFilePath = "records.json";
        File.Delete(TestFilePath);
    }

    [Test]
    public void LoadRecords_LoadsDataFromFile()
    {
        RecordsTable.AddRecord(new UserData { Name = "TestUser", CharactersPerMinute = 100, CharactersPerSecond = 2 });

        RecordsTable.LoadRecords();

        var records = RecordsTable.GetRecords();
        Assert.AreEqual(1, records.Count);
        Assert.AreEqual("TestUser", records[0].Name);
        Assert.AreEqual(100, records[0].CharactersPerMinute);
        Assert.AreEqual(2, records[0].CharactersPerSecond);
    }

    [Test]
    public void SaveRecords_SavesDataToFile()
    {
        RecordsTable.RecordsFilePath = TestFilePath;
        var testRecord = new UserData { Name = "NewUser", CharactersPerMinute = 120, CharactersPerSecond = 2 };

        RecordsTable.AddRecord(testRecord);
        RecordsTable.SaveRecords();

        var json = File.ReadAllText(TestFilePath);
        var records = Newtonsoft.Json.JsonConvert.DeserializeObject<List<UserData>>(json);
        Assert.AreEqual(1, records.Count);
        Assert.AreEqual("NewUser", records[0].Name);
        Assert.AreEqual(120, records[0].CharactersPerMinute);
        Assert.AreEqual(2, records[0].CharactersPerSecond);
    }

    [Test]
    public void RunTest_CompletesWithoutException()
    {
        Assert.DoesNotThrow(() => TypingTest.RunTest());
    }

    [Test]
    public void DisplayRecords_PrintsRecordsToConsole()
    {
        using (StringWriter sw = new StringWriter())
        {
            Console.SetOut(sw);

            TypingTest.DisplayRecords();

            var expectedOutput = "Таблица рекордов:\r\n\r\n";
            Assert.AreEqual(expectedOutput, sw.ToString());
        }
    }

    [Test]
    public void UpdateTimer_StopsAfterTimeout()
    {
        var stopwatch = new Stopwatch();
        var cts = new CancellationTokenSource();
        var originalLeft = Console.CursorLeft;
        var originalTop = Console.CursorTop;

        Task.Run(() => TypingTest.UpdateTimer(stopwatch, originalLeft, originalTop), cts.Token);
        Thread.Sleep(2000);
        cts.Cancel();

        Assert.IsTrue(stopwatch.Elapsed.Seconds >= 2);
    }
}

class Program
{
    static void Main()
    {
        while (true)
        {
            TypingTest.RunTest();

            Console.WriteLine("Нажмите Enter, если хотите пройти тест еще раз");
            Console.ReadLine();

            Console.Clear();
        }
    }
}
