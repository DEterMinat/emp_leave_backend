using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EmployeeLeaveApi.Diagnostics;

public class MongoInspector
{
    public static async Task Run(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);

        Console.WriteLine($"--- MongoDB Inspection for Database: {databaseName} ---");

        // 1. List Collections
        var collections = await database.ListCollectionNames().ToListAsync();
        Console.WriteLine("\nCollections found:");
        foreach (var name in collections)
        {
            var count = await database.GetCollection<BsonDocument>(name).CountDocumentsAsync(new BsonDocument());
            Console.WriteLine($"- {name}: {count} documents");
        }

        // 2. Check Users Data Types
        Console.WriteLine("\nUser ID Details:");
        var users = await database.GetCollection<BsonDocument>("users").Find(new BsonDocument()).ToListAsync();
        foreach (var user in users)
        {
            var id = user["_id"];
            var username = user.Contains("username") ? user["username"].ToString() : "N/A";
            Console.WriteLine($"- Username: {username} | _id: {id} | Type: {id.BsonType}");
        }

        // 3. Check LeaveTypes (required for sync)
        Console.WriteLine("\nLeave Types:");
        var leaveTypes = await database.GetCollection<BsonDocument>("leaveTypes").Find(new BsonDocument()).ToListAsync();
        foreach (var type in leaveTypes)
        {
            var name = type.Contains("typeName") ? type["typeName"].ToString() : "N/A";
            Console.WriteLine($"- {name} ({type["_id"]})");
        }
    }
}
