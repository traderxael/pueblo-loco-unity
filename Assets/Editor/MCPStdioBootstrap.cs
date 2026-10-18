using UnityEditor;
using MCPForUnity.Editor.Services;
using MCPForUnity.Editor.Services.Transport.Transports;

namespace BajoTerra.EditorTools
{
    public static class MCPStdioBootstrap
    {
        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            var cache = EditorConfigurationCache.Instance;

            if (cache.UseHttpTransport)
            {
                cache.SetUseHttpTransport(false);
                UnityEngine.Debug.Log("[MCPStdioBootstrap] Flipped UseHttpTransport to false (stdio).");
            }

            if (StdioBridgeHost.IsRunning) return;

            StdioBridgeHost.StartAutoConnect();
            UnityEngine.Debug.Log("[MCPStdioBootstrap] Started stdio bridge on port " + StdioBridgeHost.GetCurrentPort());
        }
    }
}
