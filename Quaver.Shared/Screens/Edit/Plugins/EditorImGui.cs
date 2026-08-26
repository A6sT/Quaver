using ImGuiNET;

namespace Quaver.Shared.Screens.Edit.Plugins;

public static class EditorImGui
{
    public static bool Begin(IEditorPlugin plugin, string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    {
        var isOpen = plugin.IsActive;
        return Begin(plugin, name, ref isOpen, flags);
    }

    public static bool Begin(IEditorPlugin plugin, string name, bool isOpen, ImGuiWindowFlags flags)
    {
        return Begin(plugin, name, ref isOpen, flags);
    }

    private static bool Begin(IEditorPlugin plugin, string name, ref bool isOpen, ImGuiWindowFlags flags)
    {
        var isVisible = ImGui.Begin(name, ref isOpen, flags);
        plugin.IsActive = isOpen;
        return isVisible;
    }
}
