using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Npgsql;
using Planora.Infrastructure.Persistence;

namespace Planora.DataMigration;

internal static class Program
{
    private const int BatchSize = 200;

    private static readonly string[] TableOrder =
    [
        "Permissions",
        "Users",
        "SubscriptionPlans",
        "Projects",
        "ProjectFolders",
        "ProjectRoles",
        "ProjectMembers",
        "Sprints",
        "ProjectTasks",
        "ProjectInvitations",
        "ProjectMemberRoles",
        "ProjectRolePermissions",
        "ProjectFiles",
        "ProjectDocuments",
        "FileVersions",
        "DocumentVersions",
        "FolderAccessRules",
        "TaskAcceptanceCriteria",
        "TaskAssignees",
        "TaskExtensionRequests",
        "TaskDeadlineChanges",
        "TaskSubmissions",
        "TaskSubmissionLinks",
        "TaskSubmissionFiles",
        "RefreshTokens",
        "ExternalLogins",
        "PasswordResetTokens",
        "UserGmailLinks",
        "UserNotifications",
        "Feedbacks",
        "SystemSettings",
        "SupportConversations",
        "SupportMessages",
        "PaymentTransactions",
        "UserSubscriptions",
        "AuditLogs",
    ];

    private static readonly IReadOnlyDictionary<string, string[]> DeferredColumns =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["ProjectFolders"] = ["ParentFolderId"],
            ["ProjectFiles"] = ["CurrentVersionId"],
            ["ProjectDocuments"] = ["CurrentVersionId"],
            ["PaymentTransactions"] = ["SubscriptionId"],
        };

    public static async Task<int> Main(string[] args)
    {
        var execute = args.Contains("--execute", StringComparer.Ordinal);
        var verify = args.Contains("--verify", StringComparer.Ordinal);
        if (execute == verify)
        {
            Console.Error.WriteLine("Specify exactly one mode: --execute or --verify.");
            return 2;
        }

        var sourceConnectionString = GetRequiredEnvironmentVariable("PLANORA_SOURCE_SQLSERVER");
        var targetConnectionString = PostgreSqlConnectionString.Normalize(
            GetRequiredEnvironmentVariable("ConnectionStrings__DefaultConnection"));

        await using var source = new SqlConnection(sourceConnectionString);
        await using var target = new NpgsqlConnection(targetConnectionString);
        await source.OpenAsync();
        await target.OpenAsync();

        using var sourceTransaction = source.BeginTransaction(IsolationLevel.RepeatableRead);
        await using var targetTransaction = await target.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        try
        {
            var sourceColumns = await LoadSourceColumnsAsync(source, sourceTransaction);
            var targetColumns = await LoadTargetColumnsAsync(target, targetTransaction);
            ValidateSchemas(sourceColumns, targetColumns);

            if (verify)
            {
                long verifiedRows = 0;
                foreach (var table in TableOrder)
                {
                    var columns = sourceColumns[table]
                        .Where(column => targetColumns[table].Contains(column, StringComparer.Ordinal))
                        .ToArray();
                    var count = await VerifyTableAsync(
                        source, sourceTransaction, target, targetTransaction, table, columns);
                    verifiedRows += count;
                    Console.WriteLine($"VERIFIED {table} {count}");
                }

                await targetTransaction.RollbackAsync();
                sourceTransaction.Rollback();
                Console.WriteLine($"VERIFICATION_OK TABLES={TableOrder.Length} ROWS={verifiedRows}");
                return 0;
            }

            await ValidateCleanTargetAsync(target, targetTransaction);

            await ExecuteTargetAsync(target, targetTransaction, "DELETE FROM \"Permissions\";");

            var sourceCounts = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (var table in TableOrder)
            {
                var deferred = DeferredColumns.TryGetValue(table, out var columns)
                    ? columns.ToHashSet(StringComparer.Ordinal)
                    : [];
                var copyColumns = sourceColumns[table]
                    .Where(column => targetColumns[table].Contains(column, StringComparer.Ordinal) && !deferred.Contains(column))
                    .ToArray();
                var copied = await CopyTableAsync(source, sourceTransaction, target, targetTransaction, table, copyColumns);
                sourceCounts[table] = copied;
                Console.WriteLine($"COPIED {table} {copied}");
            }

            foreach (var entry in DeferredColumns)
            {
                foreach (var column in entry.Value)
                {
                    var updated = await RestoreDeferredColumnAsync(
                        source, sourceTransaction, target, targetTransaction, entry.Key, column);
                    Console.WriteLine($"RESTORED {entry.Key}.{column} {updated}");
                }
            }

            var backfilledPayments = await BackfillLegacyPaymentOrderIdsAsync(target, targetTransaction);
            Console.WriteLine($"BACKFILLED PaymentTransactions.ProviderOrderId {backfilledPayments}");

            foreach (var table in TableOrder)
            {
                var targetCount = await CountTargetRowsAsync(target, targetTransaction, table);
                if (targetCount != sourceCounts[table])
                {
                    throw new InvalidOperationException(
                        $"Row-count mismatch for {table}: source={sourceCounts[table]}, target={targetCount}.");
                }
            }

            await targetTransaction.CommitAsync();
            sourceTransaction.Rollback();
            Console.WriteLine($"MIGRATION_COMMITTED TABLES={TableOrder.Length} ROWS={sourceCounts.Values.Sum()}");
            return 0;
        }
        catch
        {
            await targetTransaction.RollbackAsync();
            sourceTransaction.Rollback();
            throw;
        }
    }

    private static string GetRequiredEnvironmentVariable(string name) =>
        Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Required environment variable {name} is missing.");

    private static async Task<Dictionary<string, string[]>> LoadSourceColumnsAsync(
        SqlConnection connection,
        SqlTransaction transaction)
    {
        const string sql = """
            SELECT TABLE_NAME, COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME <> '__EFMigrationsHistory'
            ORDER BY TABLE_NAME, ORDINAL_POSITION;
            """;
        using var command = new SqlCommand(sql, connection, transaction);
        using var reader = await command.ExecuteReaderAsync();
        var columns = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        while (await reader.ReadAsync())
        {
            var table = reader.GetString(0);
            if (!columns.TryGetValue(table, out var tableColumns))
            {
                tableColumns = [];
                columns.Add(table, tableColumns);
            }

            tableColumns.Add(reader.GetString(1));
        }

        return columns.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal);
    }

    private static async Task<Dictionary<string, string[]>> LoadTargetColumnsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        const string sql = """
            SELECT table_name, column_name
            FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name <> '__EFMigrationsHistory'
            ORDER BY table_name, ordinal_position;
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync();
        var columns = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        while (await reader.ReadAsync())
        {
            var table = reader.GetString(0);
            if (!columns.TryGetValue(table, out var tableColumns))
            {
                tableColumns = [];
                columns.Add(table, tableColumns);
            }

            tableColumns.Add(reader.GetString(1));
        }

        return columns.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal);
    }

    private static void ValidateSchemas(
        IReadOnlyDictionary<string, string[]> sourceColumns,
        IReadOnlyDictionary<string, string[]> targetColumns)
    {
        var expected = TableOrder.ToHashSet(StringComparer.Ordinal);
        var missingFromSource = expected.Except(sourceColumns.Keys, StringComparer.Ordinal).ToArray();
        var missingFromTarget = expected.Except(targetColumns.Keys, StringComparer.Ordinal).ToArray();
        var unexpectedSource = sourceColumns.Keys.Except(expected, StringComparer.Ordinal).ToArray();
        if (missingFromSource.Length != 0 || missingFromTarget.Length != 0 || unexpectedSource.Length != 0)
        {
            throw new InvalidOperationException(
                $"Schema table mismatch. Missing source=[{string.Join(',', missingFromSource)}], " +
                $"missing target=[{string.Join(',', missingFromTarget)}], " +
                $"unexpected source=[{string.Join(',', unexpectedSource)}].");
        }

        foreach (var table in TableOrder)
        {
            var targetSet = targetColumns[table].ToHashSet(StringComparer.Ordinal);
            var sourceOnly = sourceColumns[table].Where(column => !targetSet.Contains(column)).ToArray();
            if (sourceOnly.Length != 0)
            {
                throw new InvalidOperationException(
                    $"Target is missing source columns for {table}: {string.Join(',', sourceOnly)}.");
            }
        }
    }

    private static async Task ValidateCleanTargetAsync(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        var migrationCount = await CountTargetRowsAsync(connection, transaction, "__EFMigrationsHistory");
        if (migrationCount != 1)
        {
            throw new InvalidOperationException($"Expected exactly one PostgreSQL migration, found {migrationCount}.");
        }

        foreach (var table in TableOrder)
        {
            var count = await CountTargetRowsAsync(connection, transaction, table);
            var expected = table == "Permissions" ? 29 : 0;
            if (count != expected)
            {
                throw new InvalidOperationException(
                    $"Target is not clean: {table} contains {count} rows (expected {expected}).");
            }
        }
    }

    private static async Task<long> CopyTableAsync(
        SqlConnection source,
        SqlTransaction sourceTransaction,
        NpgsqlConnection target,
        NpgsqlTransaction targetTransaction,
        string table,
        string[] columns)
    {
        var selectSql = $"SELECT {string.Join(",", columns.Select(QuoteSqlServer))} FROM {QuoteSqlServer("dbo")}.{QuoteSqlServer(table)};";
        using var select = new SqlCommand(selectSql, source, sourceTransaction);
        using var reader = await select.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
        var batch = new List<object?[]>(BatchSize);
        long copied = 0;
        while (await reader.ReadAsync())
        {
            var values = new object?[columns.Length];
            for (var index = 0; index < columns.Length; index++)
            {
                values[index] = reader.IsDBNull(index) ? null : NormalizeValue(reader.GetValue(index));
            }

            batch.Add(values);
            if (batch.Count == BatchSize)
            {
                await InsertBatchAsync(target, targetTransaction, table, columns, batch);
                copied += batch.Count;
                batch.Clear();
            }
        }

        if (batch.Count != 0)
        {
            await InsertBatchAsync(target, targetTransaction, table, columns, batch);
            copied += batch.Count;
        }

        return copied;
    }

    private static async Task<long> VerifyTableAsync(
        SqlConnection source,
        SqlTransaction sourceTransaction,
        NpgsqlConnection target,
        NpgsqlTransaction targetTransaction,
        string table,
        string[] columns)
    {
        var sourceSql = $"SELECT {string.Join(",", columns.Select(QuoteSqlServer))} FROM {QuoteSqlServer("dbo")}.{QuoteSqlServer(table)};";
        using var sourceCommand = new SqlCommand(sourceSql, source, sourceTransaction);
        using var sourceReader = await sourceCommand.ExecuteReaderAsync();
        var sourceHashes = await ReadRowHashesAsync(sourceReader, columns.Length);

        var targetSql = $"SELECT {string.Join(",", columns.Select(QuotePostgreSql))} FROM {QuotePostgreSql(table)};";
        await using var targetCommand = new NpgsqlCommand(targetSql, target, targetTransaction);
        await using var targetReader = await targetCommand.ExecuteReaderAsync();
        var targetHashes = await ReadRowHashesAsync(targetReader, columns.Length);

        sourceHashes.Sort(StringComparer.Ordinal);
        targetHashes.Sort(StringComparer.Ordinal);
        if (!sourceHashes.SequenceEqual(targetHashes, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"Content verification failed for {table}: source={sourceHashes.Count}, target={targetHashes.Count}.");
        }

        return sourceHashes.Count;
    }

    private static async Task<List<string>> ReadRowHashesAsync(IDataReader reader, int fieldCount)
    {
        var hashes = new List<string>();
        while (reader is System.Data.Common.DbDataReader dataReader && await dataReader.ReadAsync())
        {
            var canonical = new StringBuilder();
            for (var index = 0; index < fieldCount; index++)
            {
                var value = dataReader.IsDBNull(index) ? null : dataReader.GetValue(index);
                var formatted = FormatCanonical(value);
                canonical.Append(formatted.Length.ToString(CultureInfo.InvariantCulture))
                    .Append(':')
                    .Append(formatted);
            }

            hashes.Add(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()))));
        }

        return hashes;
    }

    private static string FormatCanonical(object? value) => value switch
    {
        null => "N",
        DateTimeOffset timestamp => $"T:{timestamp.UtcTicks / 10}",
        DateTime timestamp => $"T:{timestamp.ToUniversalTime().Ticks / 10}",
        Guid id => $"G:{id:N}",
        bool flag => flag ? "B:1" : "B:0",
        decimal number => $"D:{number.ToString("G29", CultureInfo.InvariantCulture)}",
        string text => $"S:{Convert.ToBase64String(Encoding.UTF8.GetBytes(text))}",
        IFormattable formattable => $"V:{formattable.ToString(null, CultureInfo.InvariantCulture)}",
        _ => $"V:{value}",
    };

    private static async Task InsertBatchAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string table,
        string[] columns,
        IReadOnlyList<object?[]> rows)
    {
        var parameterNames = new List<string[]>(rows.Count);
        await using var command = new NpgsqlCommand { Connection = connection, Transaction = transaction };
        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var names = new string[columns.Length];
            for (var columnIndex = 0; columnIndex < columns.Length; columnIndex++)
            {
                var name = $"p{rowIndex}_{columnIndex}";
                names[columnIndex] = $"@{name}";
                command.Parameters.AddWithValue(name, rows[rowIndex][columnIndex] ?? DBNull.Value);
            }

            parameterNames.Add(names);
        }

        command.CommandText = $"INSERT INTO {QuotePostgreSql(table)} ({string.Join(",", columns.Select(QuotePostgreSql))}) VALUES " +
            string.Join(",", parameterNames.Select(names => $"({string.Join(",", names)})")) + ";";
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> RestoreDeferredColumnAsync(
        SqlConnection source,
        SqlTransaction sourceTransaction,
        NpgsqlConnection target,
        NpgsqlTransaction targetTransaction,
        string table,
        string column)
    {
        var sql = $"SELECT {QuoteSqlServer("Id")}, {QuoteSqlServer(column)} FROM {QuoteSqlServer("dbo")}.{QuoteSqlServer(table)} WHERE {QuoteSqlServer(column)} IS NOT NULL;";
        using var select = new SqlCommand(sql, source, sourceTransaction);
        using var reader = await select.ExecuteReaderAsync();
        long updated = 0;
        while (await reader.ReadAsync())
        {
            await using var update = new NpgsqlCommand(
                $"UPDATE {QuotePostgreSql(table)} SET {QuotePostgreSql(column)} = @value WHERE {QuotePostgreSql("Id")} = @id;",
                target,
                targetTransaction);
            update.Parameters.AddWithValue("id", reader.GetGuid(0));
            update.Parameters.AddWithValue("value", reader.GetGuid(1));
            updated += await update.ExecuteNonQueryAsync();
        }

        return updated;
    }

    private static async Task<long> CountTargetRowsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string table)
    {
        await using var command = new NpgsqlCommand(
            $"SELECT COUNT(*) FROM {QuotePostgreSql(table)};",
            connection,
            transaction);
        return Convert.ToInt64(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<int> BackfillLegacyPaymentOrderIdsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        const string sql = """
            UPDATE "PaymentTransactions"
            SET "ProviderOrderId" = CASE
                WHEN "Provider" = 'Momo' THEN 'PLN-' || replace("Id"::text, '-', '')
                WHEN "Provider" = 'BankTransfer' THEN 'PLN' || replace("Id"::text, '-', '')
                ELSE "ProviderOrderId"
            END
            WHERE "ProviderOrderId" IS NULL
              AND "Status" = 'Pending'
              AND "Provider" IN ('Momo', 'BankTransfer');
            """;
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        return await command.ExecuteNonQueryAsync();
    }

    private static async Task ExecuteTargetAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync();
    }

    private static object NormalizeValue(object value) => value is DateTimeOffset timestamp
        ? timestamp.ToUniversalTime()
        : value;

    private static string QuoteSqlServer(string identifier) => $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";

    private static string QuotePostgreSql(string identifier) => $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
