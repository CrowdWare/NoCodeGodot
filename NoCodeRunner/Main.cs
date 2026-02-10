using Godot;
using Runtime.Logging;
using Runtime.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

public partial class Main : Node
{
	private enum UiScalingMode
	{
		Layout,
		Fixed
	}

	private readonly record struct UiScalingConfig(UiScalingMode Mode, Vector2I DesignSize);

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
	private readonly List<IUiActionModule> _uiActionModules = [];
	private Control? _runtimeUiHost;
	private Control? _runtimeUiRoot;
	private SubViewport? _fixedUiViewport;
	private TextureRect? _fixedUiTexture;
	private UiScalingConfig _uiScalingConfig = new(UiScalingMode.Layout, Vector2I.Zero);
	private Vector2 _fixedExpectedRootSize = Vector2.Zero;
	private int _fixedRootResizeWarnings;

	public override async void _Ready()
	{
		DiscoverUiActionModules();
		ConfigureWindowContentScale(UiScalingMode.Layout);

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
			var loader = new SmlUiLoader(
				_nodeFactoryRegistry,
				_nodePropertyMapper,
				animationApi: null,
				configureActions: ConfigureProjectActions);
			var rootControl = await loader.LoadFromUriAsync(uiUrl);
			AttachUi(rootControl);
			Runtime.Logging.RunnerLogger.Info("UI", $"UI loaded from '{uiUrl}'.");
		}
		catch (Exception ex)
		{
			Runtime.Logging.RunnerLogger.Error("UI", $"Failed to load UI from '{uiUrl}': {ex.Message}");
		}
	}

	private void DiscoverUiActionModules()
	{
		_uiActionModules.Clear();

		var assembly = Assembly.GetExecutingAssembly();
		foreach (var type in assembly.GetTypes())
		{
			if (type.IsAbstract || type.IsInterface)
			{
				continue;
			}

			if (!typeof(IUiActionModule).IsAssignableFrom(type))
			{
				continue;
			}

			if (Activator.CreateInstance(type) is not IUiActionModule module)
			{
				continue;
			}

			_uiActionModules.Add(module);
			RunnerLogger.Info("UI", $"Loaded action module: {type.FullName}");
		}
	}

	private void ConfigureProjectActions(UiActionDispatcher dispatcher)
	{
		foreach (var module in _uiActionModules)
		{
			try
			{
				module.Configure(dispatcher);
			}
			catch (Exception ex)
			{
				RunnerLogger.Error("UI", $"Action module '{module.GetType().FullName}' failed during Configure: {ex.Message}");
			}
		}

		dispatcher.RegisterActionHandlerIfMissing("save", ctx => RunnerLogger.Info("UI", $"Fallback action executed: save (sourceId='{ctx.SourceId}')."));
		dispatcher.RegisterActionHandlerIfMissing("open", ctx => RunnerLogger.Info("UI", $"Fallback action executed: open (sourceId='{ctx.SourceId}')."));
		dispatcher.RegisterActionHandlerIfMissing("saveAs", ctx => RunnerLogger.Info("UI", $"Fallback action executed: saveAs (sourceId='{ctx.SourceId}')."));
		dispatcher.RegisterIdHandlerIfMissing("saveBtn", ctx => RunnerLogger.Info("UI", $"Fallback id handler executed for '{ctx.SourceId}'."));

		dispatcher.SetPageHandlerIfMissing(path =>
		{
			RunnerLogger.Warn("UI", $"page action requested ('{path}') but dynamic page loading is not implemented in Main yet.");
		});
	}

	private static string BuildDefaultSampleUiFileUrl()
	{
		var samplePath = ProjectSettings.GlobalizePath("res://SampleProject/UI.sml");
		return new Uri(samplePath).AbsoluteUri;
	}

	private void AttachUi(Control rootControl)
	{
		_uiScalingConfig = ResolveScalingConfig(rootControl);
		ConfigureWindowContentScale(_uiScalingConfig.Mode);

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

		if (_uiScalingConfig.Mode == UiScalingMode.Fixed)
		{
			SetupFixedUi(host, rootControl, _uiScalingConfig.DesignSize);
		}
		else
		{
			SetupLayoutUi(host, rootControl);
		}

		layer.AddChild(host);
		AddChild(layer);

		_runtimeUiHost = host;
		_runtimeUiRoot = rootControl;

		if (GetWindow() is { } window)
		{
			window.SizeChanged += OnViewportSizeChanged;
		}
		if (GetViewport() is { } viewport)
		{
			viewport.SizeChanged += OnViewportSizeChanged;
		}

		OnViewportSizeChanged();
		LogUiScalingState(GetViewport()?.GetVisibleRect().Size ?? Vector2.Zero);
	}

	private void OnViewportSizeChanged()
	{
		if (_uiScalingConfig.Mode == UiScalingMode.Fixed)
		{
			ResizeFixedPresentationToViewport();
		}
		else
		{
			ResizeUiRootToViewport();
		}

		LogUiScalingState(GetViewport()?.GetVisibleRect().Size ?? Vector2.Zero);
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
	}

	private void ResizeFixedPresentationToViewport()
	{
		if (_runtimeUiHost is null)
		{
			return;
		}

		if (_fixedUiViewport is not null)
		{
			_fixedUiViewport.Size = _uiScalingConfig.DesignSize;
		}

		_runtimeUiHost.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_runtimeUiHost.SetOffsetsPreset(Control.LayoutPreset.FullRect);
		_runtimeUiHost.Position = Vector2.Zero;
		_runtimeUiHost.Size = GetViewport()?.GetVisibleRect().Size ?? Vector2.Zero;

		if (_fixedUiTexture is not null)
		{
			_fixedUiTexture.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			_fixedUiTexture.SetOffsetsPreset(Control.LayoutPreset.FullRect);
			_fixedUiTexture.Position = Vector2.Zero;
			_fixedUiTexture.Size = _runtimeUiHost.Size;
		}
	}

	private void ConfigureWindowContentScale(UiScalingMode mode)
	{
		var window = GetWindow();
		if (window is null)
		{
			return;
		}

		if (mode == UiScalingMode.Fixed)
		{
			window.ContentScaleMode = Window.ContentScaleModeEnum.Disabled;
			window.ContentScaleAspect = Window.ContentScaleAspectEnum.Ignore;
			return;
		}

		window.ContentScaleMode = Window.ContentScaleModeEnum.Disabled;
		window.ContentScaleAspect = Window.ContentScaleAspectEnum.Ignore;
	}

	private void SetupLayoutUi(Control host, Control rootControl)
	{
		rootControl.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		rootControl.SetOffsetsPreset(Control.LayoutPreset.FullRect);
		rootControl.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		rootControl.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		host.AddChild(rootControl);
	}

	private void SetupFixedUi(Control host, Control rootControl, Vector2I designSize)
	{
		if (designSize.X <= 0 || designSize.Y <= 0)
		{
			throw new InvalidOperationException("SML scaling 'fixed' requires a valid designSize: x, y.");
		}

		var viewport = new SubViewport
		{
			Name = "FixedUiSubViewport",
			Size = designSize,
			RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
			Disable3D = false,
			HandleInputLocally = true
		};

		var texture = new TextureRect
		{
			Name = "FixedUiTexture",
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			MouseFilter = Control.MouseFilterEnum.Stop
		};

		texture.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		texture.SetOffsetsPreset(Control.LayoutPreset.FullRect);
		texture.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		texture.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

		rootControl.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		rootControl.SetOffsetsPreset(Control.LayoutPreset.TopLeft);
		rootControl.Position = Vector2.Zero;
		rootControl.Size = new Vector2(designSize.X, designSize.Y);

		viewport.AddChild(rootControl);
		texture.Texture = viewport.GetTexture();
		host.AddChild(viewport);
		host.AddChild(texture);

		_fixedUiViewport = viewport;
		_fixedUiTexture = texture;
		_fixedExpectedRootSize = new Vector2(designSize.X, designSize.Y);
		_fixedRootResizeWarnings = 0;
		rootControl.Resized += OnFixedRootResized;
		texture.GuiInput += OnFixedTextureGuiInput;
	}

	private void OnFixedTextureGuiInput(InputEvent @event)
	{
		if (_fixedUiViewport is null || _fixedUiTexture is null)
		{
			return;
		}

		var textureSize = _fixedUiTexture.Size;
		if (textureSize.X <= 0f || textureSize.Y <= 0f)
		{
			return;
		}

		var design = _uiScalingConfig.DesignSize;
		if (design.X <= 0 || design.Y <= 0)
		{
			return;
		}

		Vector2 MapToDesign(Vector2 localPosition)
		{
			return new Vector2(
				localPosition.X * design.X / textureSize.X,
				localPosition.Y * design.Y / textureSize.Y
			);
		}

		switch (@event)
		{
			case InputEventMouseButton mouseButton:
				mouseButton.Position = MapToDesign(mouseButton.Position);
				mouseButton.GlobalPosition = mouseButton.Position;
				_fixedUiViewport.PushInput(mouseButton, true);
				break;

			case InputEventMouseMotion mouseMotion:
				var oldPosition = mouseMotion.Position;
				var mappedPosition = MapToDesign(oldPosition);
				mouseMotion.Relative = mappedPosition - MapToDesign(oldPosition - mouseMotion.Relative);
				mouseMotion.Position = mappedPosition;
				mouseMotion.GlobalPosition = mappedPosition;
				_fixedUiViewport.PushInput(mouseMotion, true);
				break;

			default:
				_fixedUiViewport.PushInput(@event, true);
				break;
		}
	}

	private void LogUiScalingState(Vector2 viewportSize)
	{
		if (_runtimeUiRoot is null)
		{
			return;
		}

		var window = GetWindow();
		var windowSize = window?.Size ?? Vector2I.Zero;
		var rootSize = _runtimeUiRoot.Size;
		if (_uiScalingConfig.Mode == UiScalingMode.Fixed)
		{
			var design = _uiScalingConfig.DesignSize;
			var scaleX = design.X > 0 ? (float)windowSize.X / design.X : 0f;
			var scaleY = design.Y > 0 ? (float)windowSize.Y / design.Y : 0f;
			var proportionalScale = MathF.Min(scaleX, scaleY);

			RunnerLogger.Info(
				"UI",
				$"[fixed] window={windowSize.X}x{windowSize.Y}, viewport={viewportSize.X:0}x{viewportSize.Y:0}, designSize={design.X}x{design.Y}, renderResolution={_fixedUiViewport?.Size.X ?? 0}x{_fixedUiViewport?.Size.Y ?? 0}, rootRect={rootSize.X:0}x{rootSize.Y:0}, scaleX={scaleX:0.###}, scaleY={scaleY:0.###}, proportionalScale={proportionalScale:0.###}, layoutRecomputeWarnings={_fixedRootResizeWarnings}"
			);
			return;
		}

		var effectiveFontSize = FindFirstEffectiveFontSize(_runtimeUiRoot);
		RunnerLogger.Info(
			"UI",
			$"[layout] window={windowSize.X}x{windowSize.Y}, viewport={viewportSize.X:0}x{viewportSize.Y:0}, rootRect={rootSize.X:0}x{rootSize.Y:0}, effectiveFontSize={effectiveFontSize}"
		);
	}

	private UiScalingConfig ResolveScalingConfig(Control rootControl)
	{
		var scalingMode = UiScalingMode.Layout;
		if (rootControl.HasMeta(SmlUiBuilder.MetaScalingMode))
		{
			var scalingRaw = rootControl.GetMeta(SmlUiBuilder.MetaScalingMode).AsString();
			if (string.Equals(scalingRaw, "fixed", StringComparison.OrdinalIgnoreCase))
			{
				scalingMode = UiScalingMode.Fixed;
			}
			else if (!string.IsNullOrWhiteSpace(scalingRaw) && !string.Equals(scalingRaw, "layout", StringComparison.OrdinalIgnoreCase))
			{
				RunnerLogger.Warn("UI", $"Unknown scaling mode '{scalingRaw}'. Falling back to 'layout'.");
			}
		}

		var designSize = Vector2I.Zero;
		if (rootControl.HasMeta(SmlUiBuilder.MetaDesignSizeX) && rootControl.HasMeta(SmlUiBuilder.MetaDesignSizeY))
		{
			designSize = new Vector2I(
				rootControl.GetMeta(SmlUiBuilder.MetaDesignSizeX).AsInt32(),
				rootControl.GetMeta(SmlUiBuilder.MetaDesignSizeY).AsInt32()
			);
		}

		if (scalingMode == UiScalingMode.Fixed && (designSize.X <= 0 || designSize.Y <= 0))
		{
			throw new InvalidOperationException("Scaling mode 'fixed' requires SML property designSize with positive width and height.");
		}

		return new UiScalingConfig(scalingMode, designSize);
	}

	private void OnFixedRootResized()
	{
		if (_runtimeUiRoot is null || _uiScalingConfig.Mode != UiScalingMode.Fixed)
		{
			return;
		}

		var current = _runtimeUiRoot.Size;
		if (Mathf.IsEqualApprox(current.X, _fixedExpectedRootSize.X) && Mathf.IsEqualApprox(current.Y, _fixedExpectedRootSize.Y))
		{
			return;
		}

		_fixedRootResizeWarnings++;
		RunnerLogger.Warn(
			"UI",
			$"[fixed] Unexpected root resize detected: current={current.X:0}x{current.Y:0}, expected={_fixedExpectedRootSize.X:0}x{_fixedExpectedRootSize.Y:0}."
		);
	}

	private static int FindFirstEffectiveFontSize(Control root)
	{
		foreach (var control in EnumerateControls(root))
		{
			try
			{
				var size = control.GetThemeFontSize("font_size");
				if (size > 0)
				{
					return size;
				}
			}
			catch
			{
				// ignore and continue probing
			}
		}

		return -1;
	}

	private static IEnumerable<Control> EnumerateControls(Control root)
	{
		var stack = new Stack<Node>();
		stack.Push(root);

		while (stack.Count > 0)
		{
			var current = stack.Pop();
			if (current is Control control)
			{
				yield return control;
			}

			for (var i = current.GetChildCount() - 1; i >= 0; i--)
			{
				stack.Push(current.GetChild(i));
			}
		}
	}
}
