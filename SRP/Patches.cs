using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Discord;

// TODO: Rename this namespace to your mod's name.
namespace SRP
{
    /// <summary>
    /// Add all of your <see cref="HarmonyPatch"/> classes here. If you find
    /// this file getting too large, you may want to consider separating the
    /// patches into several different classes.
    /// </summary>
    internal static class Patches
    {
        /// <summary>
        /// Example patch that logs anytime the user presses a key.
        /// </summary>
        [HarmonyPatch(typeof(scrController), "CountValidKeysPressed")]
        private static class KeyPatch
        {
            
            public static void Postfix(int __result) {
                
                //MainClass.Logger.Log($"User pressed {__result} key(s).");
            }
        }

        [HarmonyPatch(typeof(ADOBase), nameof(ADOBase.LoadScene))]
        private static class ScenePatch
        {

            private static void Prefix(string scene)
            {
                
            }

            private static void Postfix(string scene)
            {
                
            }
        }

        // 监听同步加载场景
        [HarmonyPatch]
        private static class SceneManagerLoadScenePatch
        {
            static MethodBase TargetMethod()
            {
                // 获取 LoadScene(string sceneName, LoadSceneMode mode) 方法
                return AccessTools.Method(
                    typeof(SceneManager),
                    nameof(SceneManager.LoadScene),
                    new[] { typeof(string), typeof(LoadSceneMode) }
                );
            }

            static void Prefix(string sceneName, LoadSceneMode mode)
            {
                
            }

            static void Postfix(string sceneName, LoadSceneMode mode)
            {
                
            }
        }

        [HarmonyPatch]
        private static class SceneManagerLoadSceneAsyncPatch
        {
            static MethodBase TargetMethod()
            {
                return AccessTools.Method(
                    typeof(SceneManager),
                    nameof(SceneManager.LoadSceneAsync),
                    new[] { typeof(string), typeof(LoadSceneMode) }
                );
            }

            static void Prefix(string sceneName, LoadSceneMode mode)
            {
                
            }

            static void Postfix(string sceneName, LoadSceneMode mode, ref AsyncOperation __result)
            {
                // 对于异步加载，我们需要等待加载完成
                if (__result != null)
                {
                    __result.completed += (op) =>
                    {
                        
                    };
                }
            }
        }

        // 也可以添加场景卸载的监听
        [HarmonyPatch(typeof(SceneManager), nameof(SceneManager.UnloadSceneAsync), new[] { typeof(string) })]
        private static class SceneManagerUnloadPatch
        {
            static void Prefix(string sceneName)
            {
                
            }
        }

        [HarmonyPatch(typeof(ADOBase), nameof(ADOBase.LoadScene))]
        private static class ADOFAISenceLoaded
        {
            static void Postfix(string scene)
            {
                
            }
        }
    }
}
