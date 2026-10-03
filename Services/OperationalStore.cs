using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TippSendApp.Data;
using TippSendApp.Models;
namespace TippSendApp.Services;

public class OperationalStore(IDbContextFactory<ApplicationDbContext> factory)
{
    public T Execute<TState,T>(string key, Func<TState> initial, Func<TState,T> operation, bool write = false) where TState : class
    {
        using var db = factory.CreateDbContext();
        using var transaction = db.Database.BeginTransaction();
        // Serializes reads/writes across processes, including route capacity reservations.
        db.Database.ExecuteSqlInterpolated($"SELECT pg_advisory_xact_lock(hashtext({key}))");
        var document = db.OperationalDocuments.Find(key);
        var state = document is null ? initial() : JsonSerializer.Deserialize<TState>(document.Json)
            ?? throw new InvalidDataException("Operational record cannot be read.");
        var result = operation(state);
        if (write || document is null)
        {
            if (document is null) { document = new OperationalDocument { Key = key }; db.Add(document); }
            document.Json = JsonSerializer.Serialize(state);
            db.SaveChanges();
        }
        transaction.Commit();
        return result;
    }
}
