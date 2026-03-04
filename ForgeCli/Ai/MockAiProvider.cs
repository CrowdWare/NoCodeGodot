namespace ForgeCli.Ai;

internal sealed class MockAiProvider : IAiProvider
{
    public Task<GenerationCandidate> GenerateAsync(string prompt, string feedback, CancellationToken cancellationToken = default)
    {
        var lower = prompt.ToLowerInvariant();
        var withDocking = lower.Contains("dock");
        var withViewport = lower.Contains("viewport3d") || lower.Contains("viewport");

        var sml = BuildSml(withDocking, withViewport);
        var sms =
"""
fun ready() {
    log.info("AI generated scene loaded")
}
""";

        return Task.FromResult(new GenerationCandidate(sml, sms, "mock"));
    }

    private static string BuildSml(bool docking, bool viewport)
    {
        if (docking && viewport)
        {
            return
"""
Window {
    id: mainWindow
    title: "Generated App"
    minSize: 900, 600
    size: 1280, 800

    DockingHost {
        id: mainDockHost
        anchors: left | top | right | bottom

        DockingContainer {
            id: leftDock
            dockSide: left
            fixedWidth: 280

            VBoxContainer {
                leftDock.title: "Project"
                Label { text: "Project Explorer" }
            }
        }

        DockingContainer {
            id: centerDock
            dockSide: center
            flex: true
            closeable: false

            CenterContainer {
                centerDock.title: "Viewport"
                anchors: left | top | right | bottom

                Viewport3D {
                    id: viewport
                    width: 960
                    height: 540
                    shrinkH: true
                    shrinkV: true
                }
            }
        }
    }
}
""";
        }

        return
"""
Window {
    id: mainWindow
    title: "Generated App"
    minSize: 900, 600
    size: 1200, 800

    VBoxContainer {
        anchors: left | top | right | bottom
        padding: 20, 20, 20, 20

        Label {
            text: "Generated from prompt"
            fontWeight: bold
        }

        Label {
            text: "Use a provider to refine this structure."
        }
    }
}
""";
    }
}
