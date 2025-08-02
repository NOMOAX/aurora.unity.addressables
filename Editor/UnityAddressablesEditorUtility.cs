using System.Runtime.CompilerServices;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.AddressableAssets;

namespace Aurora.UnityEditor.Addressables.Editor
{
    /// <summary>
    /// 编辑器工具集。
    /// </summary>
    public static class UnityAddressablesEditorUtility
    {
        /// <summary>
        /// 播放模式索引。
        /// </summary>
        public static int PlayModeIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ProjectConfigData.ActivePlayModeIndex;
        }

        /// <summary>
        /// 播放模式名称。
        /// </summary>
        public static string PlayModeName
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetPlayModeName(AddressableAssetSettingsDefaultObject.Settings.GetDataBuilder(PlayModeIndex));
        }

        private static string ActivePlatformName
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => PlatformMappingService.GetPlatformPathSubFolder();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetPlayModeName(IDataBuilder dataBuilder)
        {
            return dataBuilder switch
            {
                null                      => "null",
                BuildScriptPackedPlayMode => $"Use Existing Build ({ActivePlatformName})",
                _                         => dataBuilder.Name
            };
        }
    }
}
