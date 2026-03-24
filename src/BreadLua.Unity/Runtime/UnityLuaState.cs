using UnityEngine;

namespace BreadPack.NativeLua.Unity
{
    /// <summary>
    /// MonoBehaviour wrapper for LuaState with Unity lifecycle integration.
    /// </summary>
    public class UnityLuaState : MonoBehaviour
    {
        [SerializeField] private TextAsset[] startupScripts;
        [SerializeField] private bool enableRepl = false;

        private NativeLua.LuaState _state;

        public NativeLua.LuaState State => _state;

        private void Awake()
        {
            _state = new NativeLua.LuaState();

            if (startupScripts != null)
            {
                foreach (var script in startupScripts)
                {
                    if (script != null)
                    {
                        try
                        {
                            _state.DoString(script.text);
                        }
                        catch (NativeLua.LuaException ex)
                        {
                            Debug.LogError("[BreadLua] Script error in " + script.name + ": " + ex.Message);
                        }
                    }
                }
            }
        }

        private void Update()
        {
            try
            {
                // Call Lua update function if it exists
                _state.DoString("if on_update then on_update() end");
            }
            catch (NativeLua.LuaException ex)
            {
                Debug.LogError("[BreadLua] Update error: " + ex.Message);
                enabled = false;
            }
        }

        private void OnDestroy()
        {
            _state?.Dispose();
            _state = null;
        }
    }
}
