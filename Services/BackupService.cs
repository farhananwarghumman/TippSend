using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using TippSendApp.Data;
namespace TippSendApp.Services;

public class BackupService(ApplicationDbContext db,IConfiguration config)
{
    private static string Q(string value)=>"\""+value.Replace("\"","\"\"")+"\"";
    public async Task<byte[]> ExportAsync()
    {
        var script=db.GetService<IMigrator>().GenerateScript();
        var connection=(NpgsqlConnection)db.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var tx=await connection.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
        var names=new List<string>();
        await using(var command=new NpgsqlCommand("SELECT tablename FROM pg_tables WHERE schemaname='public'",connection,tx))
        await using(var reader=await command.ExecuteReaderAsync())
            while(await reader.ReadAsync())names.Add(reader.GetString(0));
        // Principal records before dependent records for normal FK validation on restore.
        string[] order=["__EFMigrationsHistory","AspNetRoles","AspNetUsers","Shops","B2BAccounts","Orders","PaymentDrafts"];
        names=names.OrderBy(n=>Array.IndexOf(order,n)<0?100:Array.IndexOf(order,n)).ThenBy(n=>n).ToList();
        var sql=new StringBuilder(script).AppendLine("BEGIN;");
        sql.Append("TRUNCATE ").Append(string.Join(",",names.Select(Q))).AppendLine(" RESTART IDENTITY CASCADE;");
        foreach(var name in names) {
            await using var command=new NpgsqlCommand($"SELECT COALESCE(json_agg(t),'[]'::json)::text FROM {Q(name)} t",connection,tx);
            var json=(string)(await command.ExecuteScalarAsync())!;
            var delimiter="$backup_"+Guid.NewGuid().ToString("N")+"$";
            sql.Append("INSERT INTO ").Append(Q(name)).Append(" SELECT * FROM json_populate_recordset(NULL::")
                .Append(Q(name)).Append(',').Append(delimiter).Append(json).Append(delimiter).AppendLine("::json);");
        }
        await using(var command=new NpgsqlCommand("SELECT table_name,column_name FROM information_schema.columns WHERE table_schema='public' AND (is_identity='YES' OR column_default LIKE 'nextval(%')",connection,tx))
        await using(var reader=await command.ExecuteReaderAsync())
            while(await reader.ReadAsync()) {
                var table=reader.GetString(0);var column=reader.GetString(1);
                sql.Append("SELECT setval(pg_get_serial_sequence('").Append(Q(table).Replace("'","''")).Append("','")
                    .Append(column.Replace("'","''")).Append("'),COALESCE(MAX(").Append(Q(column)).Append("),1),MAX(")
                    .Append(Q(column)).Append(") IS NOT NULL) FROM ").Append(Q(table)).AppendLine(";");
            }
        sql.AppendLine("COMMIT;"); await tx.CommitAsync();
        using var memory=new MemoryStream();
        using(var zip=new ZipArchive(memory,ZipArchiveMode.Create,true)) {
            async Task Write(string name,string content){var e=zip.CreateEntry(name);await using var s=e.Open();await using var w=new StreamWriter(s,Encoding.UTF8);await w.WriteAsync(content);}
            await Write("restore.sql",sql.ToString());
            await Write("README.txt","Contains private customer data and authentication keys. Store securely outside Railway. Restore only into an EMPTY disposable PostgreSQL database first: psql -v ON_ERROR_STOP=1 -f restore.sql. Restore data/ files into the application persistent directory. Do not run against production without a separately reviewed restore plan.");
            await Write("manifest.json",JsonSerializer.Serialize(new {createdAt=DateTime.UtcNow,tables=names,formatVersion=1}));
            var directory=config["AppSettingsDir"];
            if(!string.IsNullOrWhiteSpace(directory)&&Directory.Exists(directory))
                foreach(var file in Directory.EnumerateFiles(directory,"*",SearchOption.AllDirectories)) {
                    var relative=Path.GetRelativePath(directory,file).Replace('\\','/');
                    if(relative.StartsWith("backups/")||relative.EndsWith(".tmp"))continue;
                    zip.CreateEntryFromFile(file,"data/"+relative);
                }
        }
        return memory.ToArray();
    }
}
