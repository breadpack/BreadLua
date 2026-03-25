using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace BreadPack.NativeLua.Unity.Tests.GameLoop
{
    public class TestResult
    {
        public string Name;
        public bool Passed;
        public string Message;
        public long DurationMs;
    }

    public static class TestResultWriter
    {
        private const string Tag = "[BREADLUA_TEST]";
        private static readonly List<TestResult> _results = new();
        private static readonly System.Diagnostics.Stopwatch _stopwatch = new();

        public static void StartTest(string name)
        {
            Debug.Log($"{Tag} START: {name}");
            _stopwatch.Restart();
        }

        public static void Pass(string name)
        {
            _stopwatch.Stop();
            Debug.Log($"{Tag} PASS: {name}");
            _results.Add(new TestResult
            {
                Name = name, Passed = true, DurationMs = _stopwatch.ElapsedMilliseconds
            });
        }

        public static void Fail(string name, string message)
        {
            _stopwatch.Stop();
            Debug.LogError($"{Tag} FAIL: {name} — {message}");
            _results.Add(new TestResult
            {
                Name = name, Passed = false, Message = message, DurationMs = _stopwatch.ElapsedMilliseconds
            });
        }

        public static void WriteSummary()
        {
            int passed = 0, failed = 0;
            foreach (var r in _results)
            {
                if (r.Passed) passed++;
                else failed++;
            }

            Debug.Log($"{Tag} SUMMARY: {passed}/{_results.Count} passed, {failed} failed");
            WriteJsonFile();
        }

        private static void WriteJsonFile()
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"timestamp\": \"{DateTime.UtcNow:O}\",");
            sb.AppendLine($"  \"platform\": \"{Application.platform}\",");
            sb.AppendLine($"  \"total\": {_results.Count},");

            int passed = 0;
            foreach (var r in _results) if (r.Passed) passed++;
            sb.AppendLine($"  \"passed\": {passed},");
            sb.AppendLine($"  \"failed\": {_results.Count - passed},");
            sb.AppendLine("  \"results\": [");

            for (int i = 0; i < _results.Count; i++)
            {
                var r = _results[i];
                sb.Append($"    {{ \"name\": \"{Escape(r.Name)}\", \"status\": \"{(r.Passed ? "pass" : "fail")}\", \"duration_ms\": {r.DurationMs}");
                if (!r.Passed && r.Message != null)
                    sb.Append($", \"message\": \"{Escape(r.Message)}\"");
                sb.Append(" }");
                if (i < _results.Count - 1) sb.Append(",");
                sb.AppendLine();
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");

            var path = Path.Combine(Application.persistentDataPath, "test-results.json");
            File.WriteAllText(path, sb.ToString());
            Debug.Log($"{Tag} Results written to: {path}");
        }

        private static string Escape(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
        }
    }
}
