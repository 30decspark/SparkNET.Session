using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;

namespace SparkNET.Session
{
    public class SparkSession(IHttpContextAccessor context, SessionOption option)
    {
        private bool _loaded = false;
        private Dictionary<string, string> _session = [];
        private readonly string? _auth = context.HttpContext.Request.Headers[option.Name];

        public string? Get(string? key)
        {
            if (_loaded == false)
            {
                _session = GetData(_auth);
                _loaded = true;
            }

            if (string.IsNullOrWhiteSpace(key)) return null;
            return _session.TryGetValue(key, out string? value) ? value : null;
        }

        public T Get<T>(string? key, T fallback = default!)
        {
            if (_loaded == false)
            {
                _session = GetData(_auth);
                _loaded = true;
            }

            if (string.IsNullOrWhiteSpace(key) || !_session.TryGetValue(key, out string? value)) return fallback;
            try { return (T)Convert.ChangeType(value, typeof(T)); }
            catch { return fallback; }
        }

        public void Set(string? key, string? value)
        {
            if (_loaded == false)
            {
                _session = GetData(_auth);
                _loaded = true;
            }

            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrEmpty(value)) return;
            _session[key] = value;
        }

        public void Remove(string? key)
        {
            if (_loaded == false)
            {
                _session = GetData(_auth);
                _loaded = true;
            }

            if (string.IsNullOrWhiteSpace(key)) return;
            _session.Remove(key);
        }

        public void SaveChanges()
        {
            if (_loaded == false) return;
            UpdateData(_auth, _session);
        }

        public string Create(string uid, string? cred = null, string device = "", string app = "", string ip = "", int timeout = 30)
        {
            string? push = null;
            string? sock = null;
            return CreateData(_session, uid, cred, push, sock, device, app, ip, timeout);
        }

        public void Destroy()
        {
            _session = [];
            DeleteData(_auth);
        }

        private static string? _provider;

        public static void Initialize(string file = ":memory:")
        {
            _provider = $"Data Source={file}";
            using var db = new SqliteConnection(_provider);
            db.Execute("CREATE TABLE IF NOT EXISTS sessions (id TEXT PRIMARY KEY, uid TEXT NOT NULL, cred TEXT NULL, data TEXT NOT NULL, push TEXT NULL, sock TEXT NULL, device TEXT NOT NULL, app TEXT NOT NULL, ip TEXT NOT NULL, created DATETIME NOT NULL, updated DATETIME NOT NULL, timeout INT NOT NULL, expires DATETIME NOT NULL)");
            db.Execute("DELETE FROM sessions WHERE expires < @now", new { now = DateTime.Now });
        }

        public static void ClearExpires()
        {
            using var db = new SqliteConnection(_provider);
            db.Execute("DELETE FROM sessions WHERE expires < @now", new { now = DateTime.Now });
        }

        public static bool IsValid(string? id, string? cred = null, string device = "", string app = "", string ip = "")
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            using var db = new SqliteConnection(_provider);
            int i = db.Execute("UPDATE sessions SET device = @device, app = @app, ip = @ip, updated = @now, expires = datetime(@now, '+' || timeout || ' minutes') WHERE id = @id AND cred = @cred AND expires > @now", new { id, cred, device, app, ip, now = DateTime.Now });
            return i > 0;
        }

        public static string? GetUID(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            using var db = new SqliteConnection(_provider);
            return db.QueryFirstOrDefault<string>("SELECT uid FROM sessions WHERE id = @id", new { id });
        }

        public static string? GetCred(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            using var db = new SqliteConnection(_provider);
            return db.QueryFirstOrDefault<string>("SELECT cred FROM sessions WHERE id = @id", new { id });
        }

        private static Dictionary<string, string> GetData(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return [];
            using var db = new SqliteConnection(_provider);
            var session = db.QueryFirstOrDefault<string>("SELECT data FROM sessions WHERE id = @id", new { id });
            if (string.IsNullOrWhiteSpace(session)) return [];
            return JsonConvert.DeserializeObject<Dictionary<string, string>>(session) ?? [];
        }

        public static IEnumerable<SessionModel> GetDataList(string? uid, string? cred)
        {
            if (string.IsNullOrWhiteSpace(uid)) return [];
            using var db = new SqliteConnection(_provider);
            return db.Query<SessionModel>("SELECT id, device, app, ip, created, updated, expires FROM sessions WHERE uid = @uid AND cred = @cred AND expires > @now", new { uid, cred, now = DateTime.Now }).ToList();
        }

        private static string CreateData(Dictionary<string, string> session, string uid, string? cred = null, string? push = null, string? sock = null, string device = "", string app = "", string ip = "", int timeout = 30)
        {
            string id = Guid.NewGuid().ToString("N");
            string data = JsonConvert.SerializeObject(session);
            DateTime now = DateTime.Now;
            using var db = new SqliteConnection(_provider);
            db.Execute("INSERT INTO sessions VALUES (@id, @uid, @cred, @data, @push, @sock, @device, @app, @ip, @created, @updated, @timeout, @expires)", new { id, uid, cred, data, push, sock, device, app, ip, created = now, updated = now, timeout, expires = now.AddMinutes(timeout) });
            return id;
        }

        private static void UpdateData(string? id, Dictionary<string, string> session)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            string data = JsonConvert.SerializeObject(session);
            using var db = new SqliteConnection(_provider);
            db.Execute("UPDATE sessions SET data = @data WHERE id = @id", new { id, data });
        }

        public static void DeleteData(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            using var db = new SqliteConnection(_provider);
            db.Execute("DELETE FROM sessions WHERE id = @id", new { id });
        }
    }
}
