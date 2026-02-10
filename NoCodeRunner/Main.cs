using Godot;
using Runtime.Logging;
using Runtime.UI;
using System;
using System.Threading.Tasks;

public partial class Main : Node
{
	[Export]
	public int UiDesignWidth { get; set; } = 1280;

	[Export]
	public int UiDesignHeight { get; set; } = 720;

	[Export]
	public string ManifestUrl { get; set; } = "https://example.com/manifest.sml";

	[Export]
	public bool EnableStartupSync { get; set; } = true;

	[Export]
	public bool LoadUiOnStartup { get; set; } = true;

	[Export]
	public string UiSmlUrl { get; set; } = "";

	private readonly Runtime.Manifest.ManifestLoader _manifestLoader = new();
	private readonly Runtime.Assets.AssetCacheManager _assetCacheManager = new();
	private readonly NodeFactoryRegistry _nodeFactoryRegistry = new();
	private readonly NodePropertyMapper _nodePropertyMapper = new();
	private Control? _runtimeUiHost;
	private Control? _runtimeUiRoot;
	private Theme? _runtimeTheme;

	public override async void _Ready()
	{
		ConfigureWindowContentScale();

		if (EnableStartupSync)
		{
			await RunStartupSync();
		}
		else
		{
			Runtime.Logging.RunnerLogger.Info("Startup", "Manifest sync disabled (EnableStartupSync=false).");
		}

		if (LoadUiOnStartup)
		{
			await RunUiStartup();
		}
	}

	private async Task RunStartupSync()
	{
		if (string.IsNullOrWhiteSpace(ManifestUrl))
		{
			Runtime.Logging.RunnerLogger.Warn("Startup", "ManifestUrl is empty. Skipping startup sync.");
			return;
		}

		try
		{
			Runtime.Logging.RunnerLogger.Info("Startup", $"Loading manifest from '{ManifestUrl}'.");

			var manifest = await _manifestLoader.LoadAsync(ManifestUrl);
			var syncResult = await _assetCacheManager.SyncAsync(manifest);

			Runtime.Logging.RunnerLogger.Info(
				"Startup",
				$"Manifest sync completed. Downloaded={syncResult.DownloadedCount}, Reused={syncResult.ReusedCount}, Failed={syncResult.FailedCount}."
			);

			if (!string.IsNullOrWhiteSpace(manifest.EntryPoint))
			{
				Runtime.Logging.RunnerLogger.Info("Startup", $"Manifest entry point: {manifest.EntryPoint}");
			}
		}
		catch (Exception ex)
		{
			Runtime.Logging.RunnerLogger.Error("Startup", $"Manifest sync failed: {ex.Message}");
		}
	}

	private async Task RunUiStartup()
	{
		var uiUrl = string.IsNullOrWhiteSpace(UiSmlUrl)
			? BuildDefaultSampleUiFileUrl()
			: UiSmlUrl;

		try
		{
			var loader = new SmlUiLoader(_nodeFactoryRegistry, _nodePropertyMapper);
			var rootControl = await loader.LoadFromUriAsync(uiUrl);
			AttachUi(rootControl);
			Runtime.Logging.RunnerLogger.Info("UI", $"UI loaded from '{uiUrl}'.");
		}
		catch (Exception ex)
		{
			Runtime.Logging.RunnerLogger.Error("UI", $"Failed to load UI from '{uiUrl}': {ex.Message}");
		}
	}

	private static string BuildDefaultSampleUiFileUrl()
	{
		var samplePath = ProjectSettings.GlobalizePath("res://SampleProject/UI.sml");
		return new Uri(samplePath).AbsoluteUri;
	}

	private void AttachUi(Control rootControl)
	{
		var layer = new CanvasLayer
		{
			Name = "RuntimeUiLayer"
		};

		var host = new Control
		{
			Name = "RuntimeUiHost"
		};
		host.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		host.SetOffsetsPreset(Control.LayoutPreset.FullRect);
		host.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		host.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

		rootControl.Name = "SmlRoot";
		rootControl.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		rootControl.SetOffsetsPreset(Control.LayoutPreset.FullRect);
		rootControl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		rootControl.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

		host.AddChild(rootControl);
		layer.AddChild(host);
		AddChild(layer);

		_runtimeUiHost = host;
		_runtimeUiRoot = rootControl;
		ApplyUiScaleAndTheme(rootControl);

		if (GetWindow() is { } window)
		{
			window.SizeChanged += OnViewportSizeChanged;
		}
		if (GetViewport() is { } viewport)
		{
			viewport.SizeChanged += OnViewportSizeChanged;
		}

		ResizeUiRootToViewport();
	}

	private void OnViewportSizeChanged()
	{
		ApplyUiScaleAndTheme(_runtimeUiRoot);
		ResizeUiRootToViewport();
	}

	private void ResizeUiRootToViewport()
	{
		if (_runtimeUiHost is null || _runtimeUiRoot is null)
		{
			return;
		}

		var viewport = GetViewport();
		if (viewport is null)
		{
			return;
		}

		var size = viewport.GetVisibleRect().Size;
		_runtimeUiHost.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_runtimeUiHost.SetOffsetsPreset(Control.LayoutPreset.FullRect);
		_runtimeUiHost.Position = Vector2.Zero;
		_runtimeUiHost.Size = size;

		_runtimeUiRoot.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_runtimeUiRoot.SetOffsetsPreset(Control.LayoutPreset.FullRect);
		_runtimeUiRoot.Position = Vector2.Zero;
		_runtimeUiRoot.Size = size;

		LogUiScalingState(size);
	}

	private void ConfigureWindowContentScale()
	{
		var window = GetWindow();
		if (window is null)
		{
			return;
		}

		window.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
		window.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
	}

	private void ApplyUiScaleAndTheme(Control? rootControl)
	{
		if (rootControl is null)
		{
			return;
		}

		var scale = GetDisplayScale();
		if (_runtimeTheme is null)
		{
			_runtimeTheme = new Theme();
		}

		_runtimeTheme.DefaultBaseScale = scale;
		rootControl.Theme = _runtimeTheme;
	}

	private float GetDisplayScale()
	{
		var screenIndex = DisplayServer.WindowGetCurrentScreen();
		var scale = DisplayServer.ScreenGetScale(screenIndex);
		if (scale <= 0f)
		{
			return 1f;
		}

		return scale;
	}

	private void LogUiScalingState(Vector2 viewportSize)
	{
		if (_runtimeUiRoot is null)
		{
			return;
		}

		var window = GetWindow();
		var windowSize = window?.Size ?? Vector2I.Zero;
		var screenScale = GetDisplayScale();
		var rootSize = _runtimeUiRoot.Size;
		var anchors = $"L={_runtimeUiRoot.AnchorLeft:0.##}, T={_runtimeUiRoot.AnchorTop:0.##}, R={_runtimeUiRoot.AnchorRight:0.##}, B={_runtimeUiRoot.AnchorBottom:0.##}";

		RunnerLogger.Info(
			"UI",
			$"Scaling state: window={windowSize.X}x{windowSize.Y}, viewport={viewportSize.X:0}x{viewportSize.Y:0}, screenScale={screenScale:0.##}, rootRect={rootSize.X:0}x{rootSize.Y:0}, anchors={anchors}"
		);
	}
}
