﻿using System;
using System.IO;
using ScriptCs.Contracts;

namespace ScriptCs.Hosting
{
    public class FileConsole : IConsole
    {
        private readonly string _path;
        private readonly IConsole _innerConsole;

        public FileConsole(string path, IConsole innerConsole)
        {
            Guard.AgainstNullArgument("innerConsole", innerConsole);

            _path = path;
            _innerConsole = innerConsole;
        }

        public event ConsoleCancelEventHandler CancelKeyPress
        {
            add { _innerConsole.CancelKeyPress += value; }   
            remove { _innerConsole.CancelKeyPress -= value; }   
        }

        public ConsoleColor ForegroundColor
        {
            get { return _innerConsole.ForegroundColor; }
            set { _innerConsole.ForegroundColor = value; }
        }

        public void Write(string value)
        {
            _innerConsole.Write(value);
            Append(value);
        }

        public void WriteLine()
        {
            _innerConsole.WriteLine();
            AppendLine(string.Empty);
        }

        public void WriteLine(string value)
        {
            _innerConsole.WriteLine(value);
            AppendLine(value);
        }

        public string ReadLine()
        {
            var line = _innerConsole.ReadLine();
            AppendLine(line);
            return line;
        }

        public void Clear()
        {
            _innerConsole.Clear();
        }

        public void Exit()
        {
            _innerConsole.Exit();
        }

        public void ResetColor()
        {
            _innerConsole.ResetColor();
        }

        private void Append(string text)
        {
            using (var writer = new StreamWriter(_path, true))
            {
                writer.Write(text);
                writer.Flush();
            }
        }

        private void AppendLine(string text)
        {
            Append(text + Environment.NewLine);
        }
    }
}