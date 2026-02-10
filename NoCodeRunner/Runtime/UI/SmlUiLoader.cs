using Godot;
using Runtime.Logging;
using Runtime.Sml;
using Runtime.ThreeD;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Runtime.UI;

public sealed class SmlUiLoader
{
    private readonly NodeFactoryRegistry _registry;
    private readonly NodePropertyMapper _propertyMapper;
    private readonly AnimationControlApi _animationApi;
    private readonly Action<UiActionDispatcher>? _configureActions;

    public SmlUiLoader(
        NodeFactoryRegistry registry,
        NodePropertyMapper propertyMapper,
        AnimationControlApi? animationApi = null,
        Action<UiActionDispatcher>? configureActions = null)
    {
        _registry = registry;
        _propertyMapper = propertyMapper;
        _animationApi = animationApi ?? new AnimationControlApi();
        _configureActions = configureActions;
    }

    public async Task<Control> LoadFromUriAsync(string uri, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            throw new ArgumentException("UI URI must not be empty.", nameof(uri));
        }

        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri))
        {
            throw new InvalidOperationException($"Invalid UI URI: '{uri}'.");
        }

        string content;
        if (parsedUri.Scheme.Equals(Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
        {
            content = await LoadFromFileUriAsync(parsedUri, cancellationToken);
        }
        else
        {
            throw new NotSupportedException($"Unsupported URI scheme '{parsedUri.Scheme}'. Currently only file:/// is supported for UI loading.");
        }

        var schema = CreateDefaultSchema();
        var parser = new SmlParser(content, schema);
        var document = parser.ParseDocument();

        var builder = new SmlUiBuilder(_registry, _propertyMapper, _animationApi);
        _configureActions?.Invoke(builder.Actions);
        return builder.Build(document);
    }

    private static async Task<string> LoadFromFileUriAsync(Uri fileUri, CancellationToken cancellationToken)
    {
        var localPath = fileUri.LocalPath;
        if (!File.Exists(localPath))
        {
            throw new FileNotFoundException($"UI file not found: {localPath}", localPath);
        }

        RunnerLogger.Info("UI", $"Loading UI from file URI: {fileUri}");
        return await File.ReadAllTextAsync(localPath, cancellationToken);
    }

    private static SmlParserSchema CreateDefaultSchema()
    {
        var schema = new SmlParserSchema();

        schema.RegisterKnownNode("Window");
        schema.RegisterKnownNode("Label");
        schema.RegisterKnownNode("Button");
        schema.RegisterKnownNode("TextEdit");
        schema.RegisterKnownNode("Row");
        schema.RegisterKnownNode("Column");
        schema.RegisterKnownNode("Box");
        schema.RegisterKnownNode("Tabs");
        schema.RegisterKnownNode("Tab");
        schema.RegisterKnownNode("Slider");
        schema.RegisterKnownNode("Video");
        schema.RegisterKnownNode("Viewport3D");

        schema.RegisterIdentifierProperty("id");
        schema.RegisterIdentifierProperty("clicked");
        schema.RegisterEnumValue("action", "closeQuery", 1);
        schema.RegisterEnumValue("action", "open", 2);
        schema.RegisterEnumValue("action", "save", 3);
        schema.RegisterEnumValue("action", "saveAs", 4);
        schema.RegisterEnumValue("action", "animPlay", 10);
        schema.RegisterEnumValue("action", "animStop", 11);
        schema.RegisterEnumValue("action", "animRewind", 12);
        schema.RegisterEnumValue("action", "animScrub", 13);
        schema.RegisterEnumValue("action", "perspectiveNear", 14);
        schema.RegisterEnumValue("action", "perspectiveDefault", 15);
        schema.RegisterEnumValue("action", "perspectiveFar", 16);
        schema.RegisterEnumValue("action", "zoomIn", 17);
        schema.RegisterEnumValue("action", "zoomOut", 18);
        schema.RegisterEnumValue("action", "cameraReset", 19);
        schema.RegisterEnumValue("scaling", "layout", 1);
        schema.RegisterEnumValue("scaling", "fixed", 2);

        return schema;
    }
}
