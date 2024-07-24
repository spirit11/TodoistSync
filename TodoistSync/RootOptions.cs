using System.CommandLine;
static class RootOptions
{
    public static readonly Option<string> ApiKey = new(
        "--api-key",
        "The Todoist API key");

    public static readonly Option<string> DbPath = new(
        "--db-path",
        "The path to the SQLite database");

    public static readonly Option<string> FromDate = new(
        "--from-date",
        "Fetch tasks from a specific date (YYYY-MM-DD)");

    public static readonly Option<int> Days = new(
        "--days",
        "Fetch tasks completed in the last X days");

    public static readonly Option<int> Limit = new(
        "--limit",
        "Fetch a limited number of tasks");

    public static readonly Option<string> Vault = new(
        "--vault-path",
        "Path to the Obsidian vault");

    public static readonly Option<string> Source = new(
        "--source",
        "Specify the data source (todoist|database)");

    public static readonly Option<bool> NoSync = new(
        "--no-sync",
        "Do not sync the database with Todoist API");
}