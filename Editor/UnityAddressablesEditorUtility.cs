using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.AddressableAssets;

namespace Aurora.UnityEditor.Addressables.Editor
{
    /// <summary>
    /// Editor utility set.
    /// </summary>
    public static class UnityAddressablesEditorUtility
    {
        /// <summary>
        /// Play mode index.
        /// </summary>
        public static int PlayModeIndex => ProjectConfigData.ActivePlayModeIndex;

        /// <summary>
        /// Play mode name.
        /// </summary>
        public static string PlayModeName =>
            GetPlayModeName(AddressableAssetSettingsDefaultObject.Settings.GetDataBuilder(PlayModeIndex));

        private static string ActivePlatformName => PlatformMappingService.GetPlatformPathSubFolder();

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
