using UnityEngine;
using BreadPack.NativeLua;

public class BreadLuaDemo : MonoBehaviour
{
    void Start()
    {
        using var lua = new LuaState();

        // Basic execution
        lua.DoString("print('Hello from BreadLua!')");

        // Eval
        int result = lua.Eval<int>("10 + 20 + 30");
        Debug.Log("[BreadLua] 10 + 20 + 30 = " + result);

        // Tinker bind
        lua.Tinker.Bind("log", (string msg) => Debug.Log("[Lua] " + msg));
        lua.DoString("log('Runtime binding works!')");

        Debug.Log("[BreadLua] Demo complete!");
    }
}
