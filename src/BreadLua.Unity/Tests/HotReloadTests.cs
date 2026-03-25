#if !UNITY_ANDROID && !UNITY_IOS
using System.IO;
using NUnit.Framework;
using BreadPack.NativeLua;

namespace BreadPack.NativeLua.Unity.Tests
{
    [TestFixture]
    public class HotReloadTests
    {
        [Test]
        public void Reload_UpdatesGlobalState()
        {
            using var lua = new LuaState();
            var tempDir = Path.Combine(Path.GetTempPath(), "breadlua_test_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var scriptPath = Path.Combine(tempDir, "test.lua");

                File.WriteAllText(scriptPath, "version = 1");
                lua.DoFile(scriptPath);
                Assert.That(lua.Eval<int>("version"), Is.EqualTo(1));

                File.WriteAllText(scriptPath, "version = 2");
                lua.Reload(scriptPath);
                Assert.That(lua.Eval<int>("version"), Is.EqualTo(2));
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
#endif
