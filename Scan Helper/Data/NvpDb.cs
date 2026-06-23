using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using ScanHelper.Models;

namespace ScanHelper.Data
{
    public static class NvpDb
    {
        private static string DbPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "ScanHelper", "nvp.db");

        private static string ConnStr => $"Data Source={DbPath}";

        public static void Init()
        {
            string dir = Path.GetDirectoryName(DbPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using var con = new SqliteConnection(ConnStr);
            con.Open();

            var cmd = con.CreateCommand();
            cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Nvp (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS NvpItem (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    NvpId INTEGER NOT NULL,
    Barcode TEXT NOT NULL,
    Expected INTEGER NOT NULL,
    Scanned INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY(NvpId) REFERENCES Nvp(Id) ON DELETE CASCADE
);
";
            cmd.ExecuteNonQuery();
        }

        public static List<(int Id, string Name)> GetAllNvps()
        {
            var list = new List<(int, string)>();

            using var con = new SqliteConnection(ConnStr);
            con.Open();

            var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT Id, Name FROM Nvp ORDER BY Id DESC;";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add((r.GetInt32(0), r.GetString(1)));

            return list;
        }

        public static int CreateNvp(string name, List<NVPItem> items)
        {
            using var con = new SqliteConnection(ConnStr);
            con.Open();
            using var tr = con.BeginTransaction();

            var cmd = con.CreateCommand();
            cmd.Transaction = tr;
            cmd.CommandText = "INSERT INTO Nvp(Name) VALUES ($name); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$name", name);
            int nvpId = Convert.ToInt32(cmd.ExecuteScalar());

            foreach (var it in items)
            {
                var c2 = con.CreateCommand();
                c2.Transaction = tr;
                c2.CommandText = @"
INSERT INTO NvpItem(NvpId, Barcode, Expected, Scanned)
VALUES ($nvpId, $barcode, $expected, $scanned);";
                c2.Parameters.AddWithValue("$nvpId", nvpId);
                c2.Parameters.AddWithValue("$barcode", it.Barcode);
                c2.Parameters.AddWithValue("$expected", it.ExpectedCount);
                c2.Parameters.AddWithValue("$scanned", it.ScannedCount);
                c2.ExecuteNonQuery();
            }

            tr.Commit();
            return nvpId;
        }

        public static void UpdateNvp(int nvpId, string name, List<NVPItem> items)
        {
            using var con = new SqliteConnection(ConnStr);
            con.Open();
            using var tr = con.BeginTransaction();

            var cmd = con.CreateCommand();
            cmd.Transaction = tr;
            cmd.CommandText = "UPDATE Nvp SET Name=$name WHERE Id=$id;";
            cmd.Parameters.AddWithValue("$name", name);
            cmd.Parameters.AddWithValue("$id", nvpId);
            cmd.ExecuteNonQuery();

            // проще удалить позиции и вставить заново
            var del = con.CreateCommand();
            del.Transaction = tr;
            del.CommandText = "DELETE FROM NvpItem WHERE NvpId=$id;";
            del.Parameters.AddWithValue("$id", nvpId);
            del.ExecuteNonQuery();

            foreach (var it in items)
            {
                var c2 = con.CreateCommand();
                c2.Transaction = tr;
                c2.CommandText = @"
INSERT INTO NvpItem(NvpId, Barcode, Expected, Scanned)
VALUES ($nvpId, $barcode, $expected, $scanned);";
                c2.Parameters.AddWithValue("$nvpId", nvpId);
                c2.Parameters.AddWithValue("$barcode", it.Barcode);
                c2.Parameters.AddWithValue("$expected", it.ExpectedCount);
                c2.Parameters.AddWithValue("$scanned", it.ScannedCount);
                c2.ExecuteNonQuery();
            }

            tr.Commit();
        }

        public static void DeleteNvp(int nvpId)
        {
            using var con = new SqliteConnection(ConnStr);
            con.Open();

            var cmd = con.CreateCommand();
            cmd.CommandText = "DELETE FROM Nvp WHERE Id=$id;";
            cmd.Parameters.AddWithValue("$id", nvpId);
            cmd.ExecuteNonQuery();
        }

        public static List<NVPItem> GetItems(int nvpId)
        {
            var list = new List<NVPItem>();

            using var con = new SqliteConnection(ConnStr);
            con.Open();

            var cmd = con.CreateCommand();
            cmd.CommandText = @"
SELECT Barcode, Expected, Scanned
FROM NvpItem
WHERE NvpId=$id
ORDER BY Id;";
            cmd.Parameters.AddWithValue("$id", nvpId);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new NVPItem
                {
                    Barcode = r.GetString(0),
                    ExpectedCount = r.GetInt32(1),
                    ScannedCount = r.GetInt32(2)
                });
            }
            return list;
        }

        public static void UpdateScanned(int nvpId, string barcode, int scanned)
        {
            using var con = new SqliteConnection(ConnStr);
            con.Open();

            var cmd = con.CreateCommand();
            cmd.CommandText = @"
UPDATE NvpItem SET Scanned=$scanned
WHERE NvpId=$id AND Barcode=$barcode;";
            cmd.Parameters.AddWithValue("$scanned", scanned);
            cmd.Parameters.AddWithValue("$id", nvpId);
            cmd.Parameters.AddWithValue("$barcode", barcode);
            cmd.ExecuteNonQuery();
        }

        public static string GetDbPathForDebug() => DbPath;
    }
}
