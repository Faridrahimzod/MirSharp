using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace MirSharp
{
    internal class HistoryChecking
    {
        private readonly string _connectionString;

        public HistoryChecking(string dbPath)
        {
            _connectionString = $"Data Source={dbPath};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            // Проверяем, существует ли файл базы данных
            if (!File.Exists(_connectionString.Split('=')[1]))
            {
                SQLiteConnection.CreateFile(_connectionString.Split('=')[1]);
            }

            // Создаем таблицу, если её нет
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand(@"
                CREATE TABLE IF NOT EXISTS CheckHistory (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FileName TEXT NOT NULL,
                    CheckDate DATETIME NOT NULL,
                    Result TEXT NOT NULL
                );", connection);
                command.ExecuteNonQuery();
            }
        }

        public void AddCheckResult(string fileName, string result)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand(@"
                INSERT INTO CheckHistory (FileName, CheckDate, Result)
                VALUES (@fileName, @checkDate, @result);", connection);
                command.Parameters.AddWithValue("@fileName", fileName);
                command.Parameters.AddWithValue("@checkDate", DateTime.Now);
                command.Parameters.AddWithValue("@result", result);
                command.ExecuteNonQuery();
            }
        }

        public List<CheckHistoryEntry> GetCheckHistory()
        {
            var history = new List<CheckHistoryEntry>();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand("SELECT * FROM CheckHistory ORDER BY CheckDate DESC;", connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        history.Add(new CheckHistoryEntry
                        {
                            Id = reader.GetInt32(0),
                            FileName = reader.GetString(1),
                            CheckDate = reader.GetDateTime(2),
                            Result = reader.GetString(3)
                        });
                    }
                }
            }
            return history;
        }
        public void ClearHistory()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand("DELETE FROM CheckHistory;", connection);
                command.ExecuteNonQuery();
            }
        }
    }
    public class CheckHistoryEntry
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public DateTime CheckDate { get; set; }
        public string Result { get; set; }
    }
}
