using System.Text;

namespace EssSimulator.Display
{
    /// <summary>控制台单行编辑：光标移动、多行历史翻阅。</summary>
    internal sealed class ConsoleLineEditor
    {
        private readonly StringBuilder _buffer = new();
        private int _cursor;
        private int _renderedTail;

        public string Prompt { get; init; } = "> ";
        public CommandHistory History { get; init; } = new();

        public string ReadBlocking()
        {
            Console.Write(Prompt);
            Render();
            while (true)
            {
                if (TryProcessKey(Console.ReadKey(true), out var submitted))
                    return submitted;
            }
        }

        public bool TryProcessKey(ConsoleKeyInfo key, out string submittedLine)
        {
            submittedLine = string.Empty;

            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    Console.WriteLine();
                    submittedLine = _buffer.ToString();
                    if (!string.IsNullOrWhiteSpace(submittedLine))
                        History.Commit(submittedLine);
                    return true;

                case ConsoleKey.LeftArrow:
                    if (_cursor > 0)
                    {
                        _cursor--;
                        Render();
                    }
                    return false;

                case ConsoleKey.RightArrow:
                    if (_cursor < _buffer.Length)
                    {
                        _cursor++;
                        Render();
                    }
                    return false;

                case ConsoleKey.Home:
                    MoveCursor(0);
                    return false;

                case ConsoleKey.End:
                    MoveCursor(_buffer.Length);
                    return false;

                case ConsoleKey.UpArrow:
                {
                    string current = _buffer.ToString();
                    if (History.TryOlder(current, out var older))
                        ReplaceBuffer(older);
                    return false;
                }

                case ConsoleKey.DownArrow:
                {
                    if (History.TryNewer(out var newer))
                        ReplaceBuffer(newer);
                    return false;
                }

                case ConsoleKey.Backspace:
                    if (_cursor > 0)
                    {
                        _buffer.Remove(_cursor - 1, 1);
                        _cursor--;
                        History.ResetBrowse();
                        Render();
                    }
                    return false;

                case ConsoleKey.Delete:
                    if (_cursor < _buffer.Length)
                    {
                        _buffer.Remove(_cursor, 1);
                        History.ResetBrowse();
                        Render();
                    }
                    return false;

                case ConsoleKey.Escape:
                    _buffer.Clear();
                    _cursor = 0;
                    History.ResetBrowse();
                    Render();
                    return false;
            }

            if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                if (key.KeyChar == '\u0001')
                {
                    MoveCursor(0);
                    return false;
                }

                if (key.KeyChar == '\u0005')
                {
                    MoveCursor(_buffer.Length);
                    return false;
                }
            }

            if (!char.IsControl(key.KeyChar))
            {
                _buffer.Insert(_cursor, key.KeyChar);
                _cursor++;
                History.ResetBrowse();
                Render();
            }

            return false;
        }

        public void Render()
        {
            string text = _buffer.ToString();
            int lineLength = Prompt.Length + text.Length;

            Console.Write('\r');
            Console.Write(Prompt);
            Console.Write(text);

            int erase = _renderedTail - lineLength;
            if (erase > 0)
                Console.Write(new string(' ', erase));

            Console.Write('\r');
            Console.Write(Prompt);
            if (_cursor > 0)
                Console.Write(text.Substring(0, _cursor));

            _renderedTail = lineLength;
        }

        private void MoveCursor(int target)
        {
            target = Math.Clamp(target, 0, _buffer.Length);
            if (_cursor == target)
                return;

            _cursor = target;
            Render();
        }

        private void ReplaceBuffer(string text)
        {
            _buffer.Clear();
            _buffer.Append(text);
            _cursor = _buffer.Length;
            Render();
        }
    }

    internal static class ConsoleLineReader
    {
        public static string ReadLine(string prompt, CommandHistory history)
        {
            var editor = new ConsoleLineEditor
            {
                Prompt = prompt,
                History = history
            };
            return editor.ReadBlocking();
        }
    }
}
