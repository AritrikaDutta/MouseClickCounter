using System;
using System.IO;

namespace MouseClickCounter
{
    /// <summary>
    /// Thread-safe in-memory day record with durable JSON files under LocalAppData.
    /// </summary>
    public sealed class ClickStore
    {
        private readonly object _gate = new object();
        private readonly string _dataDir;
        private DayRecord _today;
        private DateTime _todayDate;
        private bool _dirty;
        private int _unsavedClicks;

        public ClickStore(string dataDir)
        {
            _dataDir = dataDir;
            Directory.CreateDirectory(_dataDir);
            _todayDate = DateTime.Now.Date;
            _today = LoadOrCreate(_todayDate);
        }

        public string DataDirectory
        {
            get { return _dataDir; }
        }

        public DayRecord Snapshot()
        {
            lock (_gate)
            {
                EnsureTodayLocked(DateTime.Now);
                // Return a JSON round-trip clone via serialize for UI safety is heavy;
                // instead copy essential fields.
                return CloneRecord(_today);
            }
        }

        public long TodayTotal
        {
            get
            {
                lock (_gate)
                {
                    EnsureTodayLocked(DateTime.Now);
                    return _today.Daily.Total;
                }
            }
        }

        public long TodayKeys
        {
            get
            {
                lock (_gate)
                {
                    EnsureTodayLocked(DateTime.Now);
                    return _today.Daily.Keys;
                }
            }
        }

        public long TodayScrolls
        {
            get
            {
                lock (_gate)
                {
                    EnsureTodayLocked(DateTime.Now);
                    return _today.Daily.Scrolls;
                }
            }
        }

        public void RecordClick(MouseButton button)
        {
            var now = DateTime.Now;
            lock (_gate)
            {
                EnsureTodayLocked(now);
                _today.RecordClick(now, button);
                MarkDirtyLocked();
            }
        }

        public void RecordKey(KeyKind kind)
        {
            var now = DateTime.Now;
            lock (_gate)
            {
                EnsureTodayLocked(now);
                _today.RecordKey(now, kind);
                MarkDirtyLocked();
            }
        }

        public void RecordScroll()
        {
            var now = DateTime.Now;
            lock (_gate)
            {
                EnsureTodayLocked(now);
                _today.RecordScroll(now);
                MarkDirtyLocked();
            }
        }

        private void MarkDirtyLocked()
        {
            _dirty = true;
            _unsavedClicks++;
            if (_unsavedClicks >= 25)
            {
                SaveLocked();
            }
        }

        public void Flush()
        {
            lock (_gate)
            {
                EnsureTodayLocked(DateTime.Now);
                if (_dirty) SaveLocked();
            }
        }

        public string TodayFilePath
        {
            get { return PathFor(_todayDate); }
        }

        private void EnsureTodayLocked(DateTime now)
        {
            var date = now.Date;
            if (date == _todayDate) return;

            if (_dirty) SaveLocked();
            _todayDate = date;
            _today = LoadOrCreate(date);
            _dirty = false;
            _unsavedClicks = 0;
        }

        private DayRecord LoadOrCreate(DateTime date)
        {
            var path = PathFor(date);
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var loaded = DayRecord.FromJsonOrNull(json, date);
                    if (loaded != null) return loaded;
                }
                catch
                {
                    // fall through to empty
                }
            }
            return DayRecord.CreateEmpty(date);
        }

        private void SaveLocked()
        {
            var path = PathFor(_todayDate);
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, _today.ToJson());
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            File.Move(tmp, path);
            _dirty = false;
            _unsavedClicks = 0;
        }

        private string PathFor(DateTime date)
        {
            return Path.Combine(_dataDir, date.ToString("yyyy-MM-dd") + ".json");
        }

        private static DayRecord CloneRecord(DayRecord src)
        {
            var json = src.ToJson();
            return DayRecord.FromJsonOrNull(json, DateTime.Parse(src.Date));
        }
    }
}
