using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;

namespace SparkNET.Session
{
    public class SparkSession(IHttpContextAccessor context, SessionOption option)
    {
        private Dictionary<string, string> session = [];
        private bool loaded = false;
        private readonly string? auth = context.HttpContext.Request.Headers[option.Name];

        public string? Get(string key)
        {
            if (!loaded)
            {
                session = GetData(auth);
                loaded = true;
            }
            return session.TryGetValue(key, out string? value) ? value : null;
        }

        public void Set(string key, string? value)
        {
            if (!loaded)
            {
                session = GetData(auth);
                loaded = true;
            }
            if (string.IsNullOrEmpty(value)) return;
            session[key] = value;
        }

        public void Remove(string key)
        {
            if (!loaded)
            {
                session = GetData(auth);
                loaded = true;
            }
            session.Remove(key);
        }

        public void SaveChanges()
        {
            UpdateData(auth, session);
        }

        public void Destroy()
        {
            session.Clear();
            DeleteData(auth);
        }

        public string Create(string user, string? cred, int timeout)
        {
            return CreateData(user, cred, timeout, session);
        }

        private static string provider = "Data Source=.session";

        public static void Initialize(string pvd)
        {
            provider = $"Data Source={pvd}";
            using var db = new SqliteConnection(provider);
            db.Execute("CREATE TABLE IF NOT EXISTS sessions (id TEXT PRIMARY KEY, user TEXT NOT NULL, cred TEXT NULL, data TEXT NOT NULL, timeout INT NOT NULL, expires DATETIME NOT NULL)");
            db.Execute("DELETE FROM sessions WHERE expires < datetime('now', 'localtime')");
        }

        public static bool IsValid(string? auth, string? cred)
        {
            if (string.IsNullOrEmpty(auth)) return false;
            using var db = new SqliteConnection(provider);
            int i = db.Execute("UPDATE sessions SET expires = datetime('now', 'localtime', '+' || timeout || ' minutes') WHERE id = @id AND cred = @cred AND expires > datetime('now', 'localtime')", new {id = auth, cred});
            return i > 0;
        }

        public static void ClearExpires()
        {
            using var db = new SqliteConnection(provider);
            db.Execute("DELETE FROM sessions WHERE expires < datetime('now', 'localtime')");
        }

        private static Dictionary<string, string> GetData(string? auth)
        {
            if (string.IsNullOrEmpty(auth)) return [];
            using var db = new SqliteConnection(provider);
            var session = db.QueryFirstOrDefault<string>("SELECT data FROM sessions WHERE id = @id", new { id = auth });
            if (string.IsNullOrEmpty(session)) return [];
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(session) ?? [];
        }

        private static string CreateData(string user, string? cred, int timeout, Dictionary<string, string> session)
        {
            string auth = Guid.NewGuid().ToString("N");
            string data = JsonConvert.SerializeObject(session);
            using var db = new SqliteConnection(provider);
            db.Execute("INSERT INTO sessions VALUES (@id, @user, @cred, @data, @timeout, @expire)", new { id = auth, user, cred, data, timeout, expire = DateTime.Now.AddMinutes(timeout) });
            return auth;
        }

        private static void UpdateData(string? auth, Dictionary<string, string> session)
        {
            if (string.IsNullOrEmpty(auth)) return;
            string data = JsonConvert.SerializeObject(session);
            using var db = new SqliteConnection(provider);
            db.Execute("UPDATE sessions SET data = @data WHERE id = @id", new { id = auth, data });
        }

        public static void DeleteData(string? auth)
        {
            if (string.IsNullOrEmpty(auth)) return;
            using var db = new SqliteConnection(provider);
            db.Execute("DELETE FROM sessions WHERE id = @id", new { id = auth });
        }

        public static string? GetUserId(string? auth)
        {
            if (string.IsNullOrEmpty(auth)) return null;
            using var db = new SqliteConnection(provider);
            return db.QueryFirstOrDefault<string>("SELECT user FROM sessions WHERE id = @id", new { id = auth });
        }

        public static string? GetCred(string? auth)
        {
            if (string.IsNullOrEmpty(auth)) return null;
            using var db = new SqliteConnection(provider);
            return db.QueryFirstOrDefault<string>("SELECT cred FROM sessions WHERE id = @id", new { id = auth });
        }

        public static List<dynamic> GetList(string? user, string? cred)
        {
            using var db = new SqliteConnection(provider);
            return db.Query("SELECT id, user, cred, timeout, expires FROM sessions WHERE user = @user AND cred = @cred AND expires > datetime('now', 'localtime')", new { user, cred }).ToList();
        }
    }
}
