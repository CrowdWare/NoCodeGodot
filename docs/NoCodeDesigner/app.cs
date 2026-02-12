using Runtime.Logging;
using Runtime.UI;

namespace DefaultProject;

/// <summary>
/// Beispiel-Action-Module für docs/Default/app.sml
/// - treeviewItemSelected(...)
/// - treeviewItemToggle(..., bool isOn)
/// </summary>
public sealed class App : IUiActionModule
{
    public void Configure(UiActionDispatcher dispatcher)
    {
        RunnerLogger.Info("Default.App", "App action module configured.");

        // Optional: globale Beobachtung aller Action-Events (Buttons, Slider, etc.)
        dispatcher.RegisterObserver(ctx =>
        {
            RunnerLogger.Info(
                "Default.App",
                $"Observer -> sourceId='{ctx.SourceId}', action='{ctx.Action}', clicked='{ctx.Clicked}', value={(ctx.NumericValue?.ToString("0.###") ?? "-")}, itemId={ctx.ItemId.Value}, toggleId={ctx.ToggleIdValue.Value}, isOn={(ctx.BoolValue?.ToString() ?? "-")}"
            );
        });

        dispatcher.RegisterActionHandler("treeItemSelected", ctx =>
        {
            var item = ctx.TreeItem;
            RunnerLogger.Info(
                "Default.App",
                $"Dispatcher treeItemSelected -> itemId={ctx.ItemId.Value}, text='{item?.Text ?? "<null>"}'"
            );
        });

        dispatcher.RegisterActionHandler("treeItemToggle", ctx =>
        {
            var item = ctx.TreeItem;
            RunnerLogger.Info(
                "Default.App",
                $"Dispatcher treeItemToggle -> itemId={ctx.ItemId.Value}, text='{item?.Text ?? "<null>"}', toggleId={ctx.ToggleIdValue.Value}, isOn={(ctx.BoolValue?.ToString() ?? "-")}"
            );
        });
    }

    /// <summary>
    /// Wird aufgerufen, wenn ein TreeView-Item selektiert wird
    /// (Fallback-Handlername, da TreeView in app.sml die id 'treeview' hat)
    /// </summary>
    private void treeviewItemSelected(Id itemId, TreeViewItem item)
    {
        RunnerLogger.Info(
            "Default.App",
            $"Tree selection -> id={itemId.Value}, text='{item.Text}', children={item.Children.Count}"
        );
    }

    /// <summary>
    /// Wird aufgerufen, wenn ein Toggle in einem TreeView-Item geklickt wird.
    /// Signatur mit isOn ist wichtig.
    /// </summary>
    private void treeviewItemToggle(Id itemId, TreeViewItem item, ToggleId toggleId, bool isOn)
    {
        RunnerLogger.Info(
            "Default.App",
            $"Tree toggle -> itemId={itemId.Value}, itemText='{item.Text}', toggleId={toggleId.Value}, isOn={isOn}"
        );

        // Beispiel: Reagiere gezielt auf den Toggle mit id: showObject
        // (id wird intern zu ToggleId aufgelöst; zur Demo prüfen wir über den Namen in item.Toggles)
        var toggled = item.Toggles.Find(t => t.ToggleId == toggleId);
        if (toggled is not null)
        {
            RunnerLogger.Info(
                "Default.App",
                $"Toggle '{toggled.Name}' switched to {(isOn ? "ON" : "OFF")}"
            );
        }
    }
}
