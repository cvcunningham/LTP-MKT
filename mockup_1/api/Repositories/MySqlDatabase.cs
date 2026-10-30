using api.Models;
using MySqlConnector;

namespace api.Repositories;

// The MySQL database, shared by the three MySql...Repository classes.
// The connection string ("ConnectionStrings:LowTide") is kept in dotnet user-secrets, never in git.
// When the API starts, Setup() creates the database and tables if they're missing, and the first
// time (all tables empty) copies everything in from the CSV files in Data/.
public class MySqlDatabase
{
    private readonly string _connectionString;

    public MySqlDatabase(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("LowTide") ?? "";
        if (_connectionString == "")
            throw new InvalidOperationException(
                "The MySQL connection string is missing. From inside mockup_1/api run: " +
                "dotnet user-secrets set \"ConnectionStrings:LowTide\" \"Server=localhost;Port=3306;" +
                "Database=low_tide;User ID=root;Password=YOUR_PASSWORD\" " +
                "(or set \"Database:Provider\" to \"Csv\" in appsettings.json to use the CSV files).");
    }

    // A new, open connection. MySqlConnector reuses connections behind the scenes, so opening one
    // per request is fast. Always use it with `await using`, so it's closed afterwards.
    public async Task<MySqlConnection> OpenAsync()
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    // A command with @name parameters filled in. Parameters (never pasting values into the SQL text)
    // keep user-typed text from being run as SQL ("SQL injection").
    public static MySqlCommand Command(MySqlConnection connection, string sql, object? parameters = null,
        MySqlTransaction? transaction = null)
    {
        var command = new MySqlCommand(sql, connection, transaction);
        if (parameters != null)
            foreach (var property in parameters.GetType().GetProperties())
                command.Parameters.AddWithValue("@" + property.Name, property.GetValue(parameters) ?? DBNull.Value);
        return command;
    }

    // Reading columns. Text columns may be NULL; MySQL's DATETIME has no time zone, and the app always
    // saves UTC, so it's marked as UTC when read (so the pages get "...Z" times like before).
    public static string Text(MySqlDataReader r, string column) =>
        r.IsDBNull(r.GetOrdinal(column)) ? "" : r.GetString(column);

    public static int? OptionalInt(MySqlDataReader r, string column) =>
        r.IsDBNull(r.GetOrdinal(column)) ? null : r.GetInt32(column);

    public static DateTime Utc(MySqlDataReader r, string column) =>
        DateTime.SpecifyKind(r.GetDateTime(column), DateTimeKind.Utc);

    public static DateTime? OptionalUtc(MySqlDataReader r, string column) =>
        r.IsDBNull(r.GetOrdinal(column)) ? null : Utc(r, column);

    public static DateOnly? OptionalDate(MySqlDataReader r, string column) =>
        r.IsDBNull(r.GetOrdinal(column)) ? null : DateOnly.FromDateTime(r.GetDateTime(column));

    // ----- Setup (run once when the API starts) -----------------------------------------------

    // The tables. Each one matches a CSV file in Data/ (same columns, snake_case names).
    // Employee ids in ideas / production_items have no foreign key on purpose: a deleted employee's
    // work keeps their id and shows "(former employee #N)".
    private const string CreateTables = """
        CREATE TABLE IF NOT EXISTS employees (
            id            INT PRIMARY KEY,                 -- chosen in the Intern Directory
            name          VARCHAR(255) NOT NULL,
            email         VARCHAR(255) NOT NULL DEFAULT '',
            phone         VARCHAR(50)  NOT NULL DEFAULT '',
            campus        VARCHAR(50)  NOT NULL DEFAULT '', -- Alabama / Ole Miss
            semester      VARCHAR(3)   NOT NULL DEFAULT '', -- 1-2 / 3-4 / 5-6 / 7-8 (information only)
            access_level  TINYINT      NULL,                -- 1-4 (decides access)
            role          VARCHAR(30)  NOT NULL DEFAULT '', -- Lead for Level 4
            password_hash VARCHAR(255) NOT NULL DEFAULT ''  -- '' = the starting password
        );

        CREATE TABLE IF NOT EXISTS employee_page_access (   -- only pages set differently from the level
            id          INT AUTO_INCREMENT PRIMARY KEY,
            employee_id INT NOT NULL,
            page        VARCHAR(50) NOT NULL,
            allowed     BOOLEAN NOT NULL,
            UNIQUE (employee_id, page),
            FOREIGN KEY (employee_id) REFERENCES employees(id) ON DELETE CASCADE ON UPDATE CASCADE
        );

        CREATE TABLE IF NOT EXISTS ideas (
            id               INT AUTO_INCREMENT PRIMARY KEY,
            name             VARCHAR(255) NOT NULL DEFAULT '', -- typed name, older ideas only
            title            TEXT NOT NULL,                    -- content name
            concept          TEXT NOT NULL,                    -- concept and why it could work
            caption          TEXT NOT NULL,
            created_at       DATETIME(6) NOT NULL,             -- UTC
            review_status    VARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending / Approved / Rejected
            denial_reason    TEXT NOT NULL,                    -- older ideas only
            reviewed_at      DATETIME(6) NULL,                 -- UTC
            campus           VARCHAR(50)  NOT NULL DEFAULT '', -- Alabama / Ole Miss / Either
            content_type     VARCHAR(255) NOT NULL DEFAULT '',
            content_category VARCHAR(50)  NOT NULL DEFAULT '',
            submitted_by_id  INT NULL                          -- employees.id
        );

        CREATE TABLE IF NOT EXISTS idea_links (
            id            INT AUTO_INCREMENT PRIMARY KEY,
            idea_id       INT NOT NULL,
            type          VARCHAR(10) NOT NULL,              -- Photo / Video
            label         VARCHAR(255) NOT NULL DEFAULT '',
            url           TEXT NOT NULL,
            drive_item_id VARCHAR(255) NOT NULL DEFAULT '',  -- OneDrive file id; '' for pasted links
            FOREIGN KEY (idea_id) REFERENCES ideas(id) ON DELETE CASCADE -- deleting an idea deletes its links
        );

        CREATE TABLE IF NOT EXISTS production_items (
            id               INT AUTO_INCREMENT PRIMARY KEY,
            idea_id          INT NOT NULL UNIQUE,             -- one item per approved idea
            filmer           VARCHAR(255) NOT NULL DEFAULT '', -- typed names, older rows only
            editor           VARCHAR(255) NOT NULL DEFAULT '',
            reviewer         VARCHAR(255) NOT NULL DEFAULT '',
            poster           VARCHAR(255) NOT NULL DEFAULT '',
            target_post_date DATE NULL,
            status           VARCHAR(30) NOT NULL DEFAULT 'Needs filming',
            updated_at       DATETIME(6) NOT NULL,            -- UTC
            filmer_id        INT NULL,                        -- employees.id
            editor_id        INT NULL,
            reviewer_id      INT NULL,
            poster_id        INT NULL,
            FOREIGN KEY (idea_id) REFERENCES ideas(id) ON DELETE CASCADE -- deleting an idea deletes its item
        );

        CREATE TABLE IF NOT EXISTS promos (
            id           INT AUTO_INCREMENT PRIMARY KEY,
            name         VARCHAR(255) NOT NULL,
            partner_type VARCHAR(30)  NOT NULL,              -- Sorority / Student org / Business
            campus       VARCHAR(50)  NOT NULL,              -- Alabama / Ole Miss
            date         DATE NOT NULL,
            promo_code   VARCHAR(100) NOT NULL DEFAULT '',
            status       VARCHAR(30)  NOT NULL,              -- Reaching out / Scheduled / Completed / Cancelled
            contact_name VARCHAR(255) NOT NULL DEFAULT '',
            contact_info VARCHAR(255) NOT NULL DEFAULT '',
            created_at   DATETIME(6) NOT NULL,               -- UTC
            updated_at   DATETIME(6) NOT NULL                -- UTC
        );

        CREATE TABLE IF NOT EXISTS promo_scripts (            -- one row per partner type
            id              INT AUTO_INCREMENT PRIMARY KEY,
            partner_type    VARCHAR(30) NOT NULL,
            message         TEXT NOT NULL,
            forward_message TEXT NOT NULL,
            updated_at      DATETIME(6) NULL                 -- UTC
        );
        """;

    private static readonly string[] Tables =
        { "employees", "employee_page_access", "ideas", "idea_links", "production_items", "promos", "promo_scripts" };

    public async Task SetupAsync(IWebHostEnvironment env, ILogger logger)
    {
        await CreateDatabaseIfMissingAsync();

        await using var connection = await OpenAsync();
        await Command(connection, CreateTables).ExecuteNonQueryAsync();

        foreach (var table in Tables)
            if (Convert.ToInt64(await Command(connection, $"SELECT COUNT(*) FROM {table}").ExecuteScalarAsync()) > 0)
                return; // already has data: never copy the CSV files in again

        logger.LogInformation("The MySQL tables are empty, so the CSV files in Data/ are being copied in.");
        await ImportCsvAsync(connection, env);
        logger.LogInformation("Finished copying the CSV files into MySQL.");
    }

    // Connects without a database first, so `CREATE DATABASE` works on a brand-new MySQL server.
    private async Task CreateDatabaseIfMissingAsync()
    {
        var builder = new MySqlConnectionStringBuilder(_connectionString);
        var name = builder.Database;
        if (name == "")
            throw new InvalidOperationException("The MySQL connection string needs a Database=... name (e.g. Database=low_tide).");
        builder.Database = "";

        await using var connection = new MySqlConnection(builder.ConnectionString);
        await connection.OpenAsync();
        await Command(connection, $"CREATE DATABASE IF NOT EXISTS `{name.Replace("`", "``")}` CHARACTER SET utf8mb4")
            .ExecuteNonQueryAsync();
    }

    // Copies every CSV file into the empty tables, keeping each row's Id. Reads them through the CSV
    // repositories, so older files are upgraded exactly as before (e.g. old status names).
    // All or nothing: if anything fails, nothing is saved and the API stops with the error.
    private static async Task ImportCsvAsync(MySqlConnection connection, IWebHostEnvironment env)
    {
        var ideas = new CsvIdeaRepository(env);
        var employees = new CsvEmployeeRepository(env);
        var promos = new CsvPromoRepository(env);

        await using var transaction = await connection.BeginTransactionAsync();

        var employeeList = await employees.GetAllAsync();
        foreach (var e in employeeList)
        {
            await Command(connection, """
                INSERT INTO employees (id, name, email, phone, campus, semester, access_level, role, password_hash)
                VALUES (@Id, @Name, @Email, @Phone, @Campus, @Semester, @AccessLevel, @Role, @PasswordHash)
                """, new
                {
                    e.Id, e.Name, e.Email, e.Phone, e.Campus, e.Semester,
                    AccessLevel = e.AccessLevel > 0 ? (int?)e.AccessLevel : null, e.Role, e.PasswordHash
                },
                transaction).ExecuteNonQueryAsync();
            foreach (var (page, allowed) in e.PageAccess)
                await Command(connection,
                    "INSERT INTO employee_page_access (employee_id, page, allowed) VALUES (@Id, @page, @allowed)",
                    new { e.Id, page, allowed }, transaction).ExecuteNonQueryAsync();
        }

        foreach (var i in await ideas.GetAllAsync())
        {
            await MySqlIdeaRepository.InsertIdeaAsync(connection, transaction, i, keepId: true);
            foreach (var l in i.Links)
                await MySqlIdeaRepository.InsertLinkAsync(connection, transaction, l, keepId: true);
        }

        foreach (var p in await ideas.GetProductionAsync())
            await Command(connection, """
                INSERT INTO production_items (id, idea_id, filmer, editor, reviewer, poster, target_post_date,
                    status, updated_at, filmer_id, editor_id, reviewer_id, poster_id)
                VALUES (@Id, @IdeaId, @Filmer, @Editor, @Reviewer, @Poster, @TargetPostDate,
                    @Status, @UpdatedAt, @FilmerId, @EditorId, @ReviewerId, @PosterId)
                """, new
                {
                    p.Id, p.IdeaId, p.Filmer, p.Editor, p.Reviewer, p.Poster, p.TargetPostDate,
                    p.Status, p.UpdatedAt, p.FilmerId, p.EditorId, p.ReviewerId, p.PosterId
                }, transaction).ExecuteNonQueryAsync();

        foreach (var p in await promos.GetAllAsync())
            await Command(connection, """
                INSERT INTO promos (id, name, partner_type, campus, date, promo_code, status,
                    contact_name, contact_info, created_at, updated_at)
                VALUES (@Id, @Name, @PartnerType, @Campus, @Date, @PromoCode, @Status,
                    @ContactName, @ContactInfo, @CreatedAt, @UpdatedAt)
                """, new
                {
                    p.Id, p.Name, p.PartnerType, p.Campus, p.Date, p.PromoCode, p.Status,
                    p.ContactName, p.ContactInfo, p.CreatedAt, p.UpdatedAt
                }, transaction).ExecuteNonQueryAsync();

        foreach (var s in await promos.GetScriptsAsync())
            await Command(connection, """
                INSERT INTO promo_scripts (id, partner_type, message, forward_message, updated_at)
                VALUES (@Id, @PartnerType, @Message, @ForwardMessage, @UpdatedAt)
                """, new { s.Id, s.PartnerType, s.Message, s.ForwardMessage, s.UpdatedAt }, transaction)
                .ExecuteNonQueryAsync();

        await transaction.CommitAsync();
    }
}
