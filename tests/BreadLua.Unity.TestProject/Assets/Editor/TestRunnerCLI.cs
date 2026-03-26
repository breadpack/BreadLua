using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class TestRunnerCLI
{
    [MenuItem("BreadLua/Run Tests")]
    public static void RunTests()
    {
        Debug.Log("[BREADLUA_CLI] === Starting Test Execution ===");

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        Debug.Log($"[BREADLUA_CLI] Loaded assemblies: {assemblies.Length}");

        int totalTests = 0;
        int passed = 0;
        int failed = 0;
        int skipped = 0;

        foreach (var asm in assemblies)
        {
            if (!asm.FullName.Contains("Tests") && !asm.FullName.Contains("NativeLua"))
                continue;

            Type[] types;
            try { types = asm.GetTypes(); }
            catch { continue; }

            foreach (var type in types)
            {
                var testFixture = type.GetCustomAttributes()
                    .Any(a => a.GetType().Name == "TestFixtureAttribute");
                if (!testFixture) continue;

                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttributes()
                        .Any(a => a.GetType().Name == "TestAttribute"));

                foreach (var method in methods)
                {
                    totalTests++;
                    string testName = $"{type.Name}.{method.Name}";

                    try
                    {
                        var instance = Activator.CreateInstance(type);
                        method.Invoke(instance, null);
                        passed++;
                        Debug.Log($"[BREADLUA_CLI] PASS: {testName}");
                    }
                    catch (TargetInvocationException ex)
                    {
                        var inner = ex.InnerException;
                        if (inner != null && inner.GetType().Name == "IgnoreException")
                        {
                            skipped++;
                            Debug.Log($"[BREADLUA_CLI] SKIP: {testName}");
                        }
                        else if (inner != null && inner.GetType().Name == "SuccessException")
                        {
                            passed++;
                            Debug.Log($"[BREADLUA_CLI] PASS: {testName}");
                        }
                        else
                        {
                            failed++;
                            Debug.LogError($"[BREADLUA_CLI] FAIL: {testName} — {inner?.Message ?? ex.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        Debug.LogError($"[BREADLUA_CLI] FAIL: {testName} — {ex.Message}");
                    }
                }
            }
        }

        Debug.Log($"[BREADLUA_CLI] === Results: {passed}/{totalTests} passed, {failed} failed, {skipped} skipped ===");

        if (failed > 0)
        {
            Debug.LogError($"[BREADLUA_CLI] {failed} test(s) FAILED");
            EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log("[BREADLUA_CLI] All tests PASSED");
            EditorApplication.Exit(0);
        }
    }
}
