﻿using System;
using ScriptCs.Contracts;

namespace ScriptCs.Hosting
{
    public class ScriptConsole : IConsole
    {
        private readonly bool _hookCancelKeyPress;

        public ScriptConsole(bool hookCancelKeyPress = true)
        {
            _hookCancelKeyPress = hookCancelKeyPress;
            if (hookCancelKeyPress)
            {
                CancelKeyPress += HandleCancelKeyPress;
            }
        }

        public event ConsoleCancelEventHandler CancelKeyPress
        {
            add { Console.CancelKeyPress += value; }
            remove { Console.CancelKeyPress -= value; }
        }

        public ConsoleColor ForegroundColor
        {
            get { return Console.ForegroundColor; }
            set { Console.ForegroundColor = value; }
        }

        public void Write(string value)
        {
            Console.Write(value);
        }

        public void WriteLine()
        {
            Console.WriteLine();
        }

        public void WriteLine(string value)
        {
            Console.WriteLine(value);
        }

        public string ReadLine()
        {
            return Console.ReadLine();
        }

        public void Clear()
        {
            Console.Clear();
        }

        public void Exit()
        {
            ResetColor();
            if (_hookCancelKeyPress)
            {
                CancelKeyPress -= HandleCancelKeyPress;
            }
        }

        public void ResetColor()
        {
            Console.ResetColor();
        }
      
        private void HandleCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            ResetColor();
        }
    }
}