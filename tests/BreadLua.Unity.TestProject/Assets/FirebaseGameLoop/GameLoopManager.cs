using System;
using System.Collections;
using UnityEngine;

namespace BreadPack.NativeLua.Unity.Tests.GameLoop
{
    public class GameLoopManager : MonoBehaviour
    {
        private static bool _isGameLoop;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void DetectGameLoop()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using var intent = activity.Call<AndroidJavaObject>("getIntent");
                var action = intent.Call<string>("getAction");
                _isGameLoop = action == "com.google.intent.action.TEST_LOOP";
            }
            catch (Exception)
            {
                _isGameLoop = false;
            }
#elif UNITY_IOS && !UNITY_EDITOR
            _isGameLoop = !string.IsNullOrEmpty(Application.absoluteURL)
                && Application.absoluteURL.StartsWith("firebase-game-loop");
#else
            _isGameLoop = false;
#endif

            if (_isGameLoop)
            {
                var go = new GameObject("GameLoopManager");
                DontDestroyOnLoad(go);
                go.AddComponent<GameLoopManager>();
            }
        }

        private void Start()
        {
            StartCoroutine(RunAllTests());
        }

        private IEnumerator RunAllTests()
        {
            Debug.Log("[BREADLUA_TEST] Game Loop started — running all tests");
            yield return null;

            RunTest("ReversePInvokeCallback", TestReversePInvokeCallback);
            RunTest("GenericReadValue_AllTypes", TestGenericReadValueAllTypes);
            RunTest("GenericCallWithParams_BoxingRoundTrip", TestGenericCallWithParamsBoxing);
            RunTest("BufferGenericPointer_MultipleTypes", TestBufferGenericPointerMultipleTypes);
            RunTest("GCHandleRoundTrip", TestGCHandleRoundTrip);
            RunTest("DelegateTypePatternMatching", TestDelegateTypePatternMatching);
            RunTest("SourceGeneratedCode_NotStripped", TestSourceGeneratedCodeNotStripped);
            RunTest("UnicodeStringMarshalling", TestUnicodeStringMarshalling);
            RunTest("LuaState_CreateAndDispose", TestLuaStateCreateAndDispose);
            RunTest("LuaState_DoString", TestLuaStateDoString);
            RunTest("Buffer_CreateAndAccess", TestBufferCreateAndAccess);
            RunTest("Tinker_BindAndCall", TestTinkerBindAndCall);

            TestResultWriter.WriteSummary();

            yield return new WaitForSeconds(1f);

#if UNITY_ANDROID && !UNITY_EDITOR
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call("finish");
#elif UNITY_IOS && !UNITY_EDITOR
            Application.OpenURL("firebase-game-loop-complete://");
#endif

            Application.Quit();
        }

        private void RunTest(string name, Action testAction)
        {
            TestResultWriter.StartTest(name);
            try
            {
                testAction();
                TestResultWriter.Pass(name);
            }
            catch (Exception ex)
            {
                TestResultWriter.Fail(name, ex.Message);
            }
        }

        private void TestReversePInvokeCallback()
        {
            using var lua = new BreadPack.NativeLua.LuaState();
            lua.Tinker.Bind("cb_test", (Func<int, int, int>)((a, b) => a + b));
            lua.DoString("result = cb_test(10, 20)");
            Assert(lua.Eval<int>("result") == 30, "Expected 30");
        }

        private void TestGenericReadValueAllTypes()
        {
            using var lua = new BreadPack.NativeLua.LuaState();
            Assert(lua.Eval<int>("42") == 42, "int failed");
            Assert(lua.Eval<long>("9999999999") == 9999999999L, "long failed");
            Assert(Math.Abs(lua.Eval<float>("1.5") - 1.5f) < 0.01f, "float failed");
            Assert(Math.Abs(lua.Eval<double>("3.14159") - 3.14159) < 0.00001, "double failed");
            Assert(lua.Eval<bool>("true"), "bool failed");
            Assert(lua.Eval<string>("'hello'") == "hello", "string failed");
        }

        private void TestGenericCallWithParamsBoxing()
        {
            using var lua = new BreadPack.NativeLua.LuaState();
            lua.DoString("function add(a, b) return a + b end");
            Assert(lua.Call<int>("add", 10, 20) == 30, "int boxing failed");
            Assert(Math.Abs(lua.Call<double>("add", 1.5, 2.5) - 4.0) < 0.01, "double boxing failed");
        }

        private void TestBufferGenericPointerMultipleTypes()
        {
            using var intBuf = new BreadPack.NativeLua.Buffer<int>(4);
            intBuf.Count = 1;
            intBuf[0] = 42;
            Assert(intBuf[0] == 42, "Buffer<int> failed");

            using var floatBuf = new BreadPack.NativeLua.Buffer<float>(4);
            floatBuf.Count = 1;
            floatBuf[0] = 1.5f;
            Assert(Math.Abs(floatBuf[0] - 1.5f) < 0.001f, "Buffer<float> failed");

            using var doubleBuf = new BreadPack.NativeLua.Buffer<double>(4);
            doubleBuf.Count = 1;
            doubleBuf[0] = 3.14;
            Assert(Math.Abs(doubleBuf[0] - 3.14) < 0.01, "Buffer<double> failed");
        }

        private void TestGCHandleRoundTrip()
        {
            var obj = "test_object";
            var ptr = BreadPack.NativeLua.ObjectHandle.Alloc(obj);
            try
            {
                var result = BreadPack.NativeLua.ObjectHandle.Get<string>(ptr);
                Assert(result == "test_object", $"Expected 'test_object', got '{result}'");
            }
            finally
            {
                BreadPack.NativeLua.ObjectHandle.Free(ptr);
            }
        }

        private void TestDelegateTypePatternMatching()
        {
            using var lua = new BreadPack.NativeLua.LuaState();
            lua.Tinker.Bind("dm_int", (Func<int, int, int>)((a, b) => a - b));
            lua.Tinker.Bind("dm_action", (Action)(() => { }));
            lua.Tinker.Bind("dm_double", (Func<double>)(() => 2.718));

            lua.DoString("r1 = dm_int(10, 3)");
            Assert(lua.Eval<int>("r1") == 7, "int delegate failed");
            lua.DoString("dm_action()");
            lua.DoString("r2 = dm_double()");
            Assert(Math.Abs(lua.Eval<double>("r2") - 2.718) < 0.001, "double delegate failed");
        }

        private void TestSourceGeneratedCodeNotStripped()
        {
            using var lua = new BreadPack.NativeLua.LuaState();
            lua.CreateMetatable("GameLoopTestMeta");
            var obj = "meta_test";
            var handle = BreadPack.NativeLua.ObjectHandle.Alloc(obj);
            try
            {
                lua.PushObject(handle, "GameLoopTestMeta");
            }
            finally
            {
                BreadPack.NativeLua.ObjectHandle.Free(handle);
            }
        }

        private void TestUnicodeStringMarshalling()
        {
            using var lua = new BreadPack.NativeLua.LuaState();
            lua.SetGlobal("korean", "한글테스트");
            var result = lua.Eval<string>("korean");
            Assert(result == "한글테스트", $"Korean failed: got '{result}'");

            lua.SetGlobal("emoji", "🎮");
            var emojiResult = lua.Eval<string>("emoji");
            Assert(emojiResult == "🎮", $"Emoji failed: got '{emojiResult}'");
        }

        private void TestLuaStateCreateAndDispose()
        {
            var lua = new BreadPack.NativeLua.LuaState();
            Assert(lua.Handle != IntPtr.Zero, "Handle is zero");
            lua.Dispose();
        }

        private void TestLuaStateDoString()
        {
            using var lua = new BreadPack.NativeLua.LuaState();
            lua.DoString("x = 1 + 2");
            Assert(lua.Eval<int>("x") == 3, "DoString failed");
        }

        private void TestBufferCreateAndAccess()
        {
            using var buffer = new BreadPack.NativeLua.Buffer<int>(10);
            buffer.Count = 2;
            buffer[0] = 100;
            buffer[1] = 200;
            Assert(buffer[0] == 100 && buffer[1] == 200, "Buffer access failed");
        }

        private void TestTinkerBindAndCall()
        {
            using var lua = new BreadPack.NativeLua.LuaState();
            lua.Tinker.Bind("gl_add", (Func<int, int, int>)((a, b) => a + b));
            lua.DoString("result = gl_add(5, 7)");
            Assert(lua.Eval<int>("result") == 12, "Tinker bind failed");
        }

        private static void Assert(bool condition, string message = "Assertion failed")
        {
            if (!condition) throw new Exception(message);
        }
    }
}
