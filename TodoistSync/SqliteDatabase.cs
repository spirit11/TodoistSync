using System.Data.SQLite;
using Dapper;
using Todoist.Net.Models;

namespace TodoistSync;

public class SqliteDatabase
{
    private readonly string connectionString;

    public SqliteDatabase(string dbFilePath)
    {
        connectionString = $"Data Source={dbFilePath};Version=3;";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        DoWithConnection(connection =>
        {
            connection.Open();

            // Create the CompletedItems table if it doesn't exist
            const string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS CompletedItems (
                    CompletedAt DATETIME,
                    Content TEXT,
                    TaskId INTEGER,
                    Id INTEGER PRIMARY KEY,
                    ProjectId INTEGER
                );";
            connection.Execute(createTableQuery);
            return true;
        });
    }

    public void SaveCompletedItem(IEnumerable<CompletedItem> items)
    {
        DoWithConnection((connection) =>
        {
            const string insertQuery = @"
                INSERT OR REPLACE INTO CompletedItems (CompletedAt, Content, TaskId, Id, ProjectId)
                VALUES (@CompletedAt, @Content, @TaskId, @Id, @ProjectId);";

            foreach (var item in items)
            {
                connection.Execute(insertQuery, item);
            }

            return true;
        });
    }

    public CompletedItem? GetLastCompletedItem() =>
        DoWithConnection((connection) =>
        {
            const string selectQuery = @"
                SELECT * FROM CompletedItems
                ORDER by CompletedAt desc
                LIMIT 1;";
            return connection.QueryFirstOrDefault<CompletedItem>(selectQuery);
        });

    public List<CompletedItem> GetCompletedItemsByCompletedAtRange(DateTime startDate, DateTime endDate) =>
        DoWithConnection((connection) =>
        {
            const string selectQuery = @"
                SELECT * FROM CompletedItems
                WHERE CompletedAt BETWEEN @StartDate AND @EndDate;";
            return connection.Query<CompletedItem>(selectQuery, new { StartDate = startDate, EndDate = endDate })
                .ToList();
        });

    public CompletedItem? GetCompletedItemById(long id) =>
        DoWithConnection((connection) =>
        {
            string selectQuery = @"
                SELECT * FROM CompletedItems
                WHERE Id = @Id;";
            return connection.QueryFirstOrDefault<CompletedItem>(selectQuery, new { Id = id });
        });

    public IEnumerable<CompletedItem> GetCompletedItemsAfter(string taskId, string date) =>
        DoWithConnection((connection) =>
        {
            const string selectQuery1 = @"
                SELECT * FROM CompletedItems
                WHERE TaskId = @Id AND DATE(CompletedAt) = @Date";
            var completedItem =
                connection.QueryFirstOrDefault<CompletedItem>(selectQuery1, new { Id = taskId, Date = date });
            const string selectQuery = @"
                SELECT * FROM CompletedItems
                WHERE CompletedAt > @Date
                ORDER BY CompletedAt";
            return connection.Query<CompletedItem>(selectQuery,
                new { Date = completedItem.CompletedAt.ToUniversalTime() });
        });

    private T DoWithConnection<T>(Func<SQLiteConnection, T> func)
    {
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();
        return func(connection);
    }
}