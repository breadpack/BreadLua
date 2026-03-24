using System;
using System.Text;
using UnityEngine;

namespace BreadPack.NativeLua.Unity
{
    /// <summary>
    /// Loads Lua scripts from Unity Resources folder.
    /// Lua files should be saved as .lua.txt (TextAsset) in Resources.
    /// </summary>
    public class UnityModuleLoader
    {
        private readonly string _basePath;

        public UnityModuleLoader(string basePath = "Lua")
        {
            _basePath = basePath;
        }

        public string Load(string moduleName)
        {
            string path = string.IsNullOrEmpty(_basePath)
                ? moduleName
                : _basePath + "/" + moduleName;

            var textAsset = Resources.Load<TextAsset>(path);
            if (textAsset == null)
            {
                Debug.LogWarning("[BreadLua] Module not found in Resources: " + path);
                return null;
            }

            return textAsset.text;
        }
    }
}
