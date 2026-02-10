using Godot;
using Runtime.Logging;
using Runtime.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Runtime.Assets;
using Runtime.Sml;

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
	private string? _resolvedStartupUiUrl;
	private const string DefaultStartUrl = "https://crowdware.github.io/NoCodeGodot/Default/manifest.sml";
	private const string StartupSettingsFileName = "startup_settings.sml";
	private static readonly System.Net.Http.HttpClient StartupHttpClient = new();
	private CanvasLayer? _startupProgressLayer;
	private ProgressBar? _startupProgressBar;
	private Label? _startupProgressLabel;

	public override async void _Ready()
	{
		DiscoverUiActionModules();
		ConfigureWindowContentScale(UiScalingMode.Layout);

		_resolvedStartupUiUrl = await ResolveStartupUiUrlAsync();

		if (LoadUiOnStartup)
		{
			await RunUiStartup();
		}
	}

	private async Task<string> ResolveStartupUiUrlAsync()
	{
		var options = ParseStartupOptions();
		var settings = await LoadStartupSettingsAsync();

		if (options.ResetStartUrl)
		{
			settings.StartUrl = null;
			await SaveStartupSettingsAsync(settings);
		}

		var configuredStartUrl = options.UrlOverride
			?? settings.StartUrl
			?? (!string.IsNullOrWhiteSpace(UiSmlUrl) ? UiSmlUrl : null)
			?? (!string.IsNullOrWhiteSpace(ManifestUrl) ? ManifestUrl : null)
			?? DefaultStartUrl;

		var bootSource = options.UrlOverride is not null
			? "paramUrl"
			: settings.StartUrl is not null
				? "settingsStartUrl"
				: (!string.IsNullOrWhiteSpace(UiSmlUrl) || !string.IsNullOrWhiteSpace(ManifestUrl))
					? "legacyExport"
					: "defaultCrowdWare";

		if (options.ClearCache)
		{
			if (options.UrlOverride is not null)
			{
				var clearManifestUrl = ToManifestUrl(options.UrlOverride);
				if (clearManifestUrl is null)
				{
					_assetCacheManager.ClearAllCaches();
				}
				else
				{
					_assetCacheManager.ClearCacheForManifestUrl(clearManifestUrl);
				}
			}
			else
			{
				_assetCacheManager.ClearAllCaches();
			}
		}

		if (options.UrlOverride is not null)
		{
			settings.StartUrl = options.UrlOverride;
			await SaveStartupSettingsAsync(settings);
		}

		RunnerLogger.Info("Startup", $"BootSource: {bootSource}");

		if (!EnableStartupSync)
		{
			RunnerLogger.Info("Startup", "Manifest sync disabled (EnableStartupSync=false).");
			return ResolveLegacyStartupUiUrl(configuredStartUrl);
		}

		var manifestUrl = ToManifestUrl(configuredStartUrl);
		if (manifestUrl is null)
		{
			RunnerLogger.Info("Startup", "Configured start URL is direct UI URL (non-manifest mode).");
			var directUiFile = await TryDownloadDirectUiAsync(configuredStartUrl);
			if (!string.IsNullOrWhiteSpace(directUiFile))
			{
				RunnerLogger.Info("Startup", "Manifest: missing");
				RunnerLogger.Info("Startup", "Cache: hit");
				RunnerLogger.Info("Startup", "Downloads: 1 and 0");
				RunnerLogger.Info("Startup", "Offline: false");
				return directUiFile;
			}

			RunnerLogger.Warn("Startup", "Direct UI URL could not be resolved. Falling back to embedded fallback app.");
			return BuildFallbackAppFileUrl();
		}

		var offline = false;
		var progressOverlayVisible = false;
		try
		{
			var manifest = await _manifestLoader.LoadAsync(manifestUrl);
			var syncPlan = await _assetCacheManager.BuildSyncPlanAsync(manifest);
			var thresholdBytes = Math.Max(0, settings.ProgressThresholdMb) * 1024L * 1024L;
			var showProgress = syncPlan.DownloadCount > 0 && syncPlan.PlannedBytes >= thresholdBytes;

			RunnerLogger.Info(
				"Startup",
				$"PlannedDownloads: files={syncPlan.DownloadCount}, bytes={syncPlan.PlannedBytes}, unknownSizes={syncPlan.UnknownSizeCount}, progressThresholdBytes={thresholdBytes}, showProgress={showProgress}"
			);

			IProgress<AssetSyncProgress>? progressReporter = null;
			if (showProgress)
			{
				ShowStartupProgressOverlay(syncPlan);
				progressOverlayVisible = true;
				progressReporter = new Progress<AssetSyncProgress>(ReportStartupProgress);
			}

			var syncResult = await _assetCacheManager.SyncAsync(
				manifest,
				progress: progressReporter,
				planOverride: syncPlan);

			RunnerLogger.Info("Startup", $"Offline: {offline}");
			RunnerLogger.Info("Startup", $"Cache: {(syncResult.CacheHit ? "hit" : "miss")}");
			RunnerLogger.Info("Startup", $"Manifest: {syncResult.ManifestStatus}");
			RunnerLogger.Info("Startup", $"Downloads: {syncResult.DownloadedCount} and {syncResult.DownloadedBytes}");

			if (!string.IsNullOrWhiteSpace(syncResult.EntryFileUrl))
			{
				return syncResult.EntryFileUrl;
			}

			throw new InvalidOperationException("Manifest sync completed but no cached entry file is available.");
		}
		catch (Exception ex)
		{
			offline = IsOfflineException(ex);
			RunnerLogger.Warn("Startup", $"Manifest startup failed for '{manifestUrl}': {ex.Message}");
			RunnerLogger.Info("Startup", $"Offline: {offline}");

			var cachedEntryUrl = _assetCacheManager.TryGetCachedEntryUrl(manifestUrl);
			if (!string.IsNullOrWhiteSpace(cachedEntryUrl))
			{
				RunnerLogger.Info("Startup", "Cache: hit");
				RunnerLogger.Info("Startup", "Manifest: missing");
				RunnerLogger.Info("Startup", "Downloads: 0 and 0");
				return cachedEntryUrl;
			}

			RunnerLogger.Info("Startup", "Cache: miss");
			RunnerLogger.Info("Startup", "Manifest: missing");
			RunnerLogger.Info("Startup", "Downloads: 0 and 0");
			RunnerLogger.Warn("Startup", "No cached content available. Falling back to embedded fallback app.");
			return BuildFallbackAppFileUrl();
		}
		finally
		{
			if (progressOverlayVisible)
			{
				HideStartupProgressOverlay();
			}
		}
	}

	private async Task RunUiStartup()
	{
		var uiUrl = _resolvedStartupUiUrl ?? BuildFallbackAppFileUrl();

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

	private string ResolveLegacyStartupUiUrl(string configuredStartUrl)
	{
		if (!string.IsNullOrWhiteSpace(configuredStartUrl))
		{
			if (configuredStartUrl.Contains("/NoCodeRunner/SampleProject/UI.sml", StringComparison.OrdinalIgnoreCase)
				|| configuredStartUrl.Contains("\\NoCodeRunner\\SampleProject\\UI.sml", StringComparison.OrdinalIgnoreCase))
			{
				RunnerLogger.Warn("UI", $"Configured UiSmlUrl points to deprecated sample path ('{configuredStartUrl}'). Using docs sample path instead.");
				return BuildDefaultSampleUiFileUrl();
			}

			if (Uri.TryCreate(configuredStartUrl, UriKind.Absolute, out var configured)
				&& configured.Scheme.Equals(Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
			{
				if (File.Exists(configured.LocalPath))
				{
					return configuredStartUrl;
				}

				RunnerLogger.Warn("UI", $"Configured UiSmlUrl file does not exist: {configured.LocalPath}. Falling back to embedded fallback app.");
				return BuildFallbackAppFileUrl();
			}

			if (Uri.TryCreate(configuredStartUrl, UriKind.Absolute, out var httpConfigured)
				&& (httpConfigured.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
					|| httpConfigured.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
			{
				return configuredStartUrl;
			}

			return configuredStartUrl;
		}

		return BuildFallbackAppFileUrl();
	}

	private static bool IsOfflineException(Exception ex)
	{
		if (ex is System.Net.Http.HttpRequestException)
		{
			return true;
		}

		return ex.InnerException is not null && IsOfflineException(ex.InnerException);
	}

	private static string? ToManifestUrl(string urlOrBase)
	{
		if (!Uri.TryCreate(urlOrBase, UriKind.Absolute, out var uri))
		{
			return null;
		}

		if (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
			|| uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
		{
			if (uri.AbsolutePath.EndsWith("/manifest.sml", StringComparison.OrdinalIgnoreCase)
				|| uri.AbsolutePath.Equals("manifest.sml", StringComparison.OrdinalIgnoreCase))
			{
				return uri.ToString();
			}

			if (uri.AbsolutePath.EndsWith(".sml", StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}

			return new Uri(uri.ToString().TrimEnd('/') + "/manifest.sml").ToString();
		}

		return null;
	}

	private async Task<string?> TryDownloadDirectUiAsync(string uiUrl)
	{
		if (!Uri.TryCreate(uiUrl, UriKind.Absolute, out var uri)
			|| (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
				&& !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
		{
			if (Uri.TryCreate(uiUrl, UriKind.Absolute, out var fileUri)
				&& fileUri.Scheme.Equals(Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase)
				&& File.Exists(fileUri.LocalPath))
			{
				return uiUrl;
			}

			return null;
		}

		var cacheRoot = ProjectSettings.GlobalizePath("user://cache/direct-ui");
		var urlHash = ComputeSha256Hex(uiUrl);
		var appRoot = Path.Combine(cacheRoot, urlHash);
		Directory.CreateDirectory(appRoot);

		var targetPath = Path.Combine(appRoot, "app.sml");
		var tempPath = targetPath + ".tmp";

		try
		{
			using var response = await StartupHttpClient.GetAsync(uiUrl);
			response.EnsureSuccessStatusCode();
			var content = await response.Content.ReadAsStringAsync();
			await File.WriteAllTextAsync(tempPath, content);

			if (File.Exists(targetPath))
			{
				File.Delete(targetPath);
			}

			File.Move(tempPath, targetPath);
			RunnerLogger.Info("Startup", $"Downloaded direct UI: {uiUrl}");
			return new Uri(targetPath).AbsoluteUri;
		}
		catch (Exception ex)
		{
			if (File.Exists(targetPath))
			{
				RunnerLogger.Warn("Startup", $"Direct UI download failed ({ex.Message}). Using cached direct UI.");
				return new Uri(targetPath).AbsoluteUri;
			}

			RunnerLogger.Warn("Startup", $"Direct UI download failed and no cache is available: {ex.Message}");
			return null;
		}
		finally
		{
			if (File.Exists(tempPath))
			{
				File.Delete(tempPath);
			}
		}
	}

	private static string ComputeSha256Hex(string input)
	{
		var bytes = Encoding.UTF8.GetBytes(input);
		var hash = SHA256.HashData(bytes);
		return Convert.ToHexString(hash).ToLowerInvariant();
	}

	private static string BuildFallbackAppFileUrl()
	{
		var fallbackPath = ProjectSettings.GlobalizePath("res://fallback/app.sml");
		return new Uri(fallbackPath).AbsoluteUri;
	}

	private StartupOptions ParseStartupOptions()
	{
		var args = OS.GetCmdlineArgs();
		string? urlOverride = null;
		var clearCache = false;
		var resetStartUrl = false;

		for (var i = 0; i < args.Length; i++)
		{
			var arg = args[i];
			if (arg == "--url" && i + 1 < args.Length)
			{
				urlOverride = args[i + 1];
				i++;
				continue;
			}

			if (arg == "--clear-cache")
			{
				clearCache = true;
				continue;
			}

			if (arg == "--reset-start-url")
			{
				resetStartUrl = true;
			}
		}

		return new StartupOptions(urlOverride, clearCache, resetStartUrl);
	}

	private async Task<StartupSettings> LoadStartupSettingsAsync()
	{
		var path = ProjectSettings.GlobalizePath($"user://{StartupSettingsFileName}");
		if (!File.Exists(path))
		{
			return new StartupSettings();
		}

		try
		{
			var content = await File.ReadAllTextAsync(path);
			return ParseStartupSettingsSml(content);
		}
		catch (Exception ex)
		{
			RunnerLogger.Warn("Startup", $"Failed to load startup settings from SML. Using defaults. Reason: {ex.Message}");
			return new StartupSettings();
		}
	}

	private static async Task SaveStartupSettingsAsync(StartupSettings settings)
	{
		var path = ProjectSettings.GlobalizePath($"user://{StartupSettingsFileName}");
		var temp = path + ".tmp";
		var sml = BuildStartupSettingsSml(settings);
		await File.WriteAllTextAsync(temp, sml);

		if (File.Exists(path))
		{
			File.Delete(path);
		}

		File.Move(temp, path);
	}

	private sealed record StartupOptions(string? UrlOverride, bool ClearCache, bool ResetStartUrl);

	private sealed class StartupSettings
	{
		public string? StartUrl { get; set; }
		public int ProgressThresholdMb { get; set; } = 10;
	}

	private static StartupSettings ParseStartupSettingsSml(string content)
	{
		var schema = new SmlParserSchema();
		schema.RegisterKnownNode("StartupSettings");
		schema.WarnOnUnknownNodes = true;

		var parser = new SmlParser(content, schema);
		var document = parser.ParseDocument();

		if (document.Roots.Count == 0)
		{
			return new StartupSettings();
		}

		var root = document.Roots[0];
		if (!string.Equals(root.Name, "StartupSettings", StringComparison.OrdinalIgnoreCase))
		{
			return new StartupSettings();
		}

		var settings = new StartupSettings();
		if (root.TryGetProperty("startUrl", out var startUrlValue))
		{
			settings.StartUrl = startUrlValue.AsStringOrThrow("startUrl");
		}

		if (root.TryGetProperty("progressThresholdMb", out var thresholdValue))
		{
			settings.ProgressThresholdMb = Math.Max(0, thresholdValue.AsIntOrThrow("progressThresholdMb"));
		}

		return settings;
	}

	private static string BuildStartupSettingsSml(StartupSettings settings)
	{
		var builder = new StringBuilder();
		builder.AppendLine("StartupSettings {");
		if (!string.IsNullOrWhiteSpace(settings.StartUrl))
		{
			builder.AppendLine($"    startUrl: \"{EscapeSmlString(settings.StartUrl!)}\"");
		}

		builder.AppendLine($"    progressThresholdMb: {Math.Max(0, settings.ProgressThresholdMb)}");
		builder.AppendLine("}");
		return builder.ToString();
	}

	private static string EscapeSmlString(string value)
	{
		return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
	}

	private void ShowStartupProgressOverlay(AssetSyncPlan plan)
	{
		if (_startupProgressLayer is not null)
		{
			return;
		}

		var layer = new CanvasLayer { Name = "StartupProgressLayer", Layer = 1000 };
		var panel = new PanelContainer
		{
			Name = "StartupProgressPanel",
			CustomMinimumSize = new Vector2(640, 110)
		};
		panel.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
		panel.Position = new Vector2(320, 40);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 16);
		margin.AddThemeConstantOverride("margin_right", 16);
		margin.AddThemeConstantOverride("margin_top", 12);
		margin.AddThemeConstantOverride("margin_bottom", 12);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 6);
		var label = new Label
		{
			Text = $"Preparing download… files={plan.DownloadCount}, planned={FormatBytes(plan.PlannedBytes)}"
		};

		var progress = new ProgressBar
		{
			MinValue = 0,
			MaxValue = Math.Max(1, plan.DownloadCount),
			Value = 0,
			ShowPercentage = true,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};

		vbox.AddChild(label);
		vbox.AddChild(progress);
		margin.AddChild(vbox);
		panel.AddChild(margin);
		layer.AddChild(panel);
		AddChild(layer);

		_startupProgressLayer = layer;
		_startupProgressBar = progress;
		_startupProgressLabel = label;
	}

	private void ReportStartupProgress(AssetSyncProgress progress)
	{
		CallDeferred(nameof(ApplyStartupProgress), progress.CompletedCount, progress.TotalCount, progress.DownloadedBytes, progress.PlannedBytes, progress.CurrentPath ?? string.Empty);
	}

	private void ApplyStartupProgress(int completedCount, int totalCount, long downloadedBytes, long plannedBytes, string currentPath)
	{
		if (_startupProgressBar is null || _startupProgressLabel is null)
		{
			return;
		}

		_startupProgressBar.MaxValue = Math.Max(1, totalCount);
		_startupProgressBar.Value = Math.Min(completedCount, totalCount);
		var pathPart = string.IsNullOrWhiteSpace(currentPath) ? "" : $", current={currentPath}";
		_startupProgressLabel.Text = $"Downloading assets… {completedCount}/{totalCount}, bytes={FormatBytes(downloadedBytes)}/{FormatBytes(plannedBytes)}{pathPart}";
	}

	private void HideStartupProgressOverlay()
	{
		if (_startupProgressLayer is null)
		{
			return;
		}

		_startupProgressLayer.QueueFree();
		_startupProgressLayer = null;
		_startupProgressBar = null;
		_startupProgressLabel = null;
	}

	private static string FormatBytes(long bytes)
	{
		if (bytes <= 0)
		{
			return "0 B";
		}

		string[] units = ["B", "KB", "MB", "GB"];
		double value = bytes;
		var unit = 0;
		while (value >= 1024 && unit < units.Length - 1)
		{
			value /= 1024;
			unit++;
		}

		return $"{value:0.##} {units[unit]}";
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
		var projectDir = ProjectSettings.GlobalizePath("res://");
		var projectRoot = Directory.GetParent(projectDir)?.FullName ?? projectDir;
		var cwd = Directory.GetCurrentDirectory();
		var cwdParent = Directory.GetParent(cwd)?.FullName ?? cwd;

		var rootCandidates = new[]
		{
			projectRoot,
			projectDir,
			cwd,
			cwdParent
		};

		foreach (var root in rootCandidates)
		{
			var docsSamplePaths = new[]
			{
				Path.Combine(root, "docs", "SampleProject", "UI.sml"),
				Path.Combine(root, "docs", "SampleProjekt", "UI.sml")
			};

			foreach (var docsSamplePath in docsSamplePaths)
			{
				if (File.Exists(docsSamplePath))
				{
					RunnerLogger.Info("UI", $"Using docs sample UI: {docsSamplePath}");
					return new Uri(docsSamplePath).AbsoluteUri;
				}
			}
		}

		var legacySamplePath = ProjectSettings.GlobalizePath("res://SampleProject/UI.sml");
		RunnerLogger.Warn("UI", $"docs sample UI not found in known roots (projectDir='{projectDir}', cwd='{cwd}'). Falling back to legacy sample path: {legacySamplePath}");
		return new Uri(legacySamplePath).AbsoluteUri;
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
