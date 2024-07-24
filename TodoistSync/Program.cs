using System.CommandLine;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
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
    var days = parseResult.GetValueForOption(RootOptions.Days);

    var apiKey = parseResult.GetValueForOption(RootOptions.ApiKey) ?? Configuration["TodoistSettings:ApiKey"];
    apiKey ??= Environment.GetEnvironmentVariable("TODOIST_API_KEY");

    // Determine Database Path
    var dbPath = parseResult.GetValueForOption(RootOptions.DbPath) ?? Configuration["TodoistSettings:DatabasePath"];
    dbPath ??= Environment.GetEnvironmentVariable("TODOIST_DB");

    if (string.IsNullOrEmpty(apiKey) && string.IsNullOrEmpty(dbPath))
    {
        Console.WriteLine("Either API key or database path must be provided.");
        return;
    }

    var fromDate = parseResult.GetValueForOption(RootOptions.FromDate);
    var limit = parseResult.GetValueForOption(RootOptions.Limit);
    var vaultPath = parseResult.GetValueForOption(RootOptions.Vault);
    var source = parseResult.GetValueForOption(RootOptions.Source);
    var noSync = parseResult.GetValueForOption(RootOptions.NoSync);

    var useApi = !string.IsNullOrEmpty(apiKey) && (string.IsNullOrEmpty(dbPath) || source == "todoist");

    if (useApi)
    {
        Console.WriteLine("Fetching tasks from Todoist API...");
        await FetchTasksFromApi(apiKey, fromDate, days, limit, vaultPath);

        if (!noSync && !string.IsNullOrEmpty(dbPath))
        {
            Console.WriteLine("Synchronizing with database...");
            SyncDatabaseWithApi(apiKey, dbPath);
        }
    }
    else
    {
        Console.WriteLine("Fetching tasks from database...");
        FetchTasksFromDatabase(dbPath, fromDate, days, limit, vaultPath);
    }
});


return await rootCommand.InvokeAsync(args);


async Task FetchTasksFromApi(string? s, string? fromDate1, int i, int limit1, string? vaultPath1)
{
}

void FetchTasksFromDatabase(string? s, string? fromDate1, int i, int limit1, string? vaultPath1)
{
}

void SyncDatabaseWithApi(string? s, string dbPath1)
{
}