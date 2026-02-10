using Runtime.Logging;
using Runtime.UI;

namespace SampleProject;

public sealed class UI : IUiActionModule
{
    public UI()
    {
        RunnerLogger.Info("Sample.UI", "UI.cs instantiated (action module loaded via reflection).");
    }

    public void Configure(UiActionDispatcher dispatcher)
    {
        RunnerLogger.Info("Sample.UI", "Configure() called. Registering observers and handlers.");

        dispatcher.RegisterObserver(ctx =>
        {
            RunnerLogger.Info(
                "Sample.UI",
                $"Observer event -> sourceId='{ctx.SourceId}', action='{ctx.Action}', clicked='{ctx.Clicked}', value={(ctx.NumericValue?.ToString("0.###") ?? "-")}" );
        });

        dispatcher.RegisterActionHandler("save", ctx =>
        {
            LogIncomingEvent(ctx, "save");
            RunnerLogger.Info("Sample.UI", $"save action executed (sourceId='{ctx.SourceId}').");
        });

        dispatcher.RegisterActionHandler("open", ctx =>
        {
            LogIncomingEvent(ctx, "open");
            RunnerLogger.Info("Sample.UI", $"open action executed (sourceId='{ctx.SourceId}').");
        });

        dispatcher.RegisterActionHandler("saveAs", ctx =>
        {
            LogIncomingEvent(ctx, "saveAs");
            RunnerLogger.Info("Sample.UI", $"saveAs action executed (sourceId='{ctx.SourceId}').");
        });

        dispatcher.RegisterIdHandler("saveBtn", ctx =>
        {
            LogIncomingEvent(ctx, "saveBtn(id)");
            RunnerLogger.Info("Sample.UI", "saveBtn id handler executed.");
        });
    }

    private static void LogIncomingEvent(UiActionContext ctx, string resolvedHandler)
    {
        RunnerLogger.Info(
            "Sample.UI",
            $"Incoming event -> handler='{resolvedHandler}', sourceId='{ctx.SourceId}', action='{ctx.Action}', clicked='{ctx.Clicked}', value={(ctx.NumericValue?.ToString("0.###") ?? "-")}"
        );
    }
}
