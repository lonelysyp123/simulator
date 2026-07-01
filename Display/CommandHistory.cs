namespace EssSimulator.Display
{
    /// <summary>命令行历史：Enter 提交，↑/↓ 翻阅。</summary>
    internal sealed class CommandHistory
    {
        private const int MaxEntries = 100;
        private readonly List<string> _entries = new();
        private int _browseIndex = -1;
        private string _scratch = string.Empty;

        public int Count => _entries.Count;

        public void Commit(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            if (_entries.Count > 0 && _entries[^1] == line)
            {
                ResetBrowse();
                return;
            }

            _entries.Add(line);
            if (_entries.Count > MaxEntries)
                _entries.RemoveAt(0);

            ResetBrowse();
        }

        public bool TryOlder(string currentLine, out string recalled)
        {
            recalled = currentLine;
            if (_entries.Count == 0)
                return false;

            if (_browseIndex < 0)
            {
                _scratch = currentLine;
                _browseIndex = _entries.Count;
            }

            if (_browseIndex <= 0)
                return false;

            _browseIndex--;
            recalled = _entries[_browseIndex];
            return true;
        }

        public bool TryNewer(out string recalled)
        {
            recalled = string.Empty;
            if (_browseIndex < 0)
                return false;

            _browseIndex++;
            if (_browseIndex >= _entries.Count)
            {
                recalled = _scratch;
                ResetBrowse();
                return true;
            }

            recalled = _entries[_browseIndex];
            return true;
        }

        public void ResetBrowse()
        {
            _browseIndex = -1;
            _scratch = string.Empty;
        }
    }
}
