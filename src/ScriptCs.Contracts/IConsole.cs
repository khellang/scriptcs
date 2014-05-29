﻿using System;

namespace ScriptCs.Contracts
{
    public interface IConsole
    {
        void Write(string value);

        void WriteLine();

        void WriteLine(string value);

        string ReadLine();

        void Clear();

        void Exit();

        void ResetColor();

        event ConsoleCancelEventHandler CancelKeyPress;

        ConsoleColor ForegroundColor { get; set; }
    }
}