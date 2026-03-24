using System;
using System.Text;

namespace BreadPack.NativeLua;

public class Repl
{
    private readonly LuaState _state;

    internal Repl(LuaState state)
    {
        _state = state;
    }

    public void Start()
    {
        Console.WriteLine("BreadLua REPL (type 'exit' or 'quit' to leave)");
        Console.WriteLine("---");

        var buffer = new StringBuilder();

        while (true)
        {
            Console.Write(buffer.Length == 0 ? "> " : ">> ");
            string? line = Console.ReadLine();

            if (line == null || line.Trim() == "exit" || line.Trim() == "quit")
                break;

            buffer.AppendLine(line);
            string code = buffer.ToString().Trim();

            if (string.IsNullOrEmpty(code))
            {
                buffer.Clear();
                continue;
            }

            // Try to evaluate as expression first (for printing results)
            try
            {
                _state.DoString("__repl_result = " + code);
                _state.DoString("if __repl_result ~= nil then print(__repl_result) end");
                _state.DoString("__repl_result = nil");
                buffer.Clear();
                continue;
            }
            catch (LuaException)
            {
                // Not an expression, try as statement
            }

            try
            {
                _state.DoString(code);
                buffer.Clear();
            }
            catch (LuaException ex)
            {
                // Check if it's an incomplete statement (multi-line)
                if (ex.Message != null && (ex.Message.Contains("<eof>") || ex.Message.Contains("'end' expected")))
                {
                    // Incomplete -- wait for more input
                    continue;
                }

                Console.Error.WriteLine("[error] " + ex.Message);
                buffer.Clear();
            }
        }

        Console.WriteLine("Goodbye!");
    }
}
