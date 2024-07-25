using System.CommandLine;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Todoist.Net;
using Todoist.Net.Models;
using TodoistSync;


var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
    .AddEnvironmentVariables();

var Configuration = builder.Build();

var rootCommand = new RootCommand
{
    RootOptions.ApiKey,
    RootOptions.DbPath,
    RootOptions.FromDate,
    RootOptions.Days,
    RootOptions.Limit,
    RootOptions.Vault,
    RootOptions.Source,
    RootOptions.NoSync
};

rootCommand.Description = "Todoist Task Fetcher";
rootCommand.SetHandler(async context =>
{
    var parseResult = context.ParseResult;

    var apiKey = parseResult.GetValueForOption(RootOptions.ApiKey)
                 ?? Configuration["TodoistSettings:ApiKey"]
                 ?? Environment.GetEnvironmentVariable("TODOIST_API_KEY");

    // Determine Database Path
    var dbPath = parseResult.GetValueForOption(RootOptions.DbPath)
                 ?? Configuration["TodoistSettings:DatabasePath"]
                 ?? Environment.GetEnvironmentVariable("TODOIST_DB");

    if (string.IsNullOrEmpty(apiKey) && string.IsNullOrEmpty(dbPath))
    {
        Console.WriteLine("Either API key or database path must be provided.");
        return;
    }

    var days = parseResult.GetValueForOption(RootOptions.Days);
    var fromDate = parseResult.GetValueForOption(RootOptions.FromDate);
    var limit = parseResult.GetValueForOption(RootOptions.Limit);
    var vaultPath = parseResult.GetValueForOption(RootOptions.Vault) ?? Configuration["TodoistSettings:VaultPath"];
    var filterOptions = new FilterOptions
    {
        Days = days,
        FromDate = fromDate,
        Limit = limit,
        VaultPath = vaultPath
    };
    var source = parseResult.GetValueForOption(RootOptions.Source);
    var noSync = parseResult.GetValueForOption(RootOptions.NoSync) || string.IsNullOrEmpty(dbPath);

    var useApi = !string.IsNullOrEmpty(apiKey) && source != "database";

    IEnumerable<CompletedItem> itemsToPrint = Array.Empty<CompletedItem>();

    if (useApi)
    {
        if (!noSync)
        {
            var sqliteDatabase = new SqliteDatabase(dbPath);
            var lastDbItem = sqliteDatabase.GetLastCompletedItem();
        }

        Console.WriteLine("Fetching tasks from Todoist API...");
        var completedTasks = await FetchTasksFromApi(apiKey, filterOptions.ToItemsQuery());
        itemsToPrint = completedTasks.Items;

        if (!noSync)
        {
            Console.WriteLine("Synchronizing with database...");
            SyncDatabaseWithApi(apiKey, dbPath, completedTasks);
        }
    }
    else
    {
        Console.WriteLine("Fetching tasks from database...");
        itemsToPrint = FetchTasksFromDatabase(dbPath, filterOptions.ToItemsQuery());
    }

    foreach (var element in itemsToPrint
                 .OrderBy(e => e.CompletedAt)
                 //.SkipWhile(e => e.TaskId.ToString() != lastCompleted.Value.taskId)
                 .Select(item =>
                     $"- [{item.Content}](https://todoist.com/app/task/{item.TaskId}) [done:: {item.CompletedAt.ToString("yyyy-MM-dd")}]"))
    {
        Console.WriteLine(element);
    }
});


return await rootCommand.InvokeAsync(args);


async Task<CompletedItemsInfo> FetchTasksFromApi(string apiKey, ItemQueryOptions itemQueryOptions)
{
    ITodoistClient client = new TodoistClient(apiKey);
    var completed = await client.Items.GetCompletedAsync(
        new ItemFilter
        {
            Limit = itemQueryOptions.Limit,
            Since = itemQueryOptions.From,
        }
    );
    return completed;
}

IEnumerable<CompletedItem> FetchTasksFromDatabase(string dbPath, ItemQueryOptions itemQueryOptions)
{
    return new SqliteDatabase(dbPath).GetCompletedItemsByCompletedAtRange(itemQueryOptions.From, DateTime.Now);
}

void SyncDatabaseWithApi(string apiKey, string dbPath, CompletedItemsInfo completedTasks)
{
}

public class FilterOptions
{
    public int Days { get; set; }
    public string? VaultPath { get; set; }
    public int Limit { get; set; }
    public string? FromDate { get; set; }

    public ItemQueryOptions ToItemsQuery()
    {
        return new ItemQueryOptions
        {
            From = FromDate is null ? DateTime.Now.AddDays(-Math.Clamp(Days, 0, 100)) : DateTime.Parse(FromDate),
            Limit = Limit,
            LastTaskInfo = string.IsNullOrEmpty(VaultPath) ? null : GetLastCompleted()
        };
    }


    (string taskId, string date)? GetLastCompleted()
    {
        var regex = new Regex(@"https://todoist.com/app/task/(\d+).+(\d{4}-\d{2}-\d{2})");

        foreach (var weekly in Directory.GetFiles(VaultPath).OrderDescending())
        {
            var lines = File.ReadAllLines(weekly);
            var lastTask = lines.Select(l => regex.Match(l)).Where(m => m.Success).LastOrDefault();
            if (lastTask != null)
            {
                return (lastTask.Groups[1].Value, lastTask.Groups[2].Value);
            }
        }

        return null;
    }
}

public class ItemQueryOptions
{
    public DateTime From { get; set; }
    public int? Limit { get; set; }
    public (string taskId, string date)? LastTaskInfo { get; set; }
}