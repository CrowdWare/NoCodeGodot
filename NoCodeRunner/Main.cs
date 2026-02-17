using Godot;
using Runtime.Logging;
using Runtime.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
	private readonly Runtime.Assets.RunnerUriResolver _uriResolver = new();
	private readonly NodeFactoryRegistry _nodeFactoryRegistry = new();
	private readonly NodePropertyMapper _nodePropertyMapper = new();
	private readonly List<IUiActionModule> _uiActionModules = [];
	private UiActionDispatcher? _uiDispatcher;
	private SmsUiRuntime? _smsUiRuntime;
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
	private CanvasLayer? _startupProgressLayer;
	private ProgressBar? _startupProgressBar;
	private Label? _startupProgressLabel;
	private bool _viewportSizeChangedConnected;

	public override async void _Ready()
	{
		DiscoverUiActionModules();
		ConfigureWindowContentScale(UiScalingMode.Layout);
		var startupSettings = await LoadStartupSettingsAsync();
		RunnerLogger.Configure(startupSettings.IncludeStackTraces, startupSettings.ShowParserWarnings);

		_resolvedStartupUiUrl = await ResolveStartupUiUrlAsync();

		if (LoadUiOnStartup)
		{
			await RunUiStartup();
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMAbout)
		{
			DispatchMacAboutAsMenuItemSelected();
		}

		base._Notification(what);
	}

	private void DispatchMacAboutAsMenuItemSelected()
	{
		if (!string.Equals(OS.GetName(), "macOS", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}

		if (_uiDispatcher is null)
		{
			RunnerLogger.Warn("UI", "Received macOS About notification, but UI dispatcher is not initialized yet.");
			return;
		}

		var source = _runtimeUiRoot ?? _runtimeUiHost;
		if (source is null)
		{
			RunnerLogger.Warn("UI", "Received macOS About notification, but no runtime UI source control is available.");
			return;
		}

		RunnerLogger.Info("UI", "Received macOS About notification -> dispatch menuItemSelected(appMenu, about).");

		_uiDispatcher.Dispatch(new UiActionContext(
			Source: source,
			SourceId: "appMenu",
			SourceIdValue: IdRuntimeScope.GetOrCreate("appMenu"),
			Action: "menuItemSelected",
			Clicked: "about",
			ClickedIdValue: IdRuntimeScope.GetOrCreate("about")
		));
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

		configuredStartUrl = _uriResolver.Normalize(configuredStartUrl);

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
				var clearManifestUrl = ToManifestUrl(_uriResolver.Normalize(options.UrlOverride));
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
			settings.StartUrl = _uriResolver.Normalize(options.UrlOverride);
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
			var directUiFile = await TryResolveDirectUiAsync(configuredStartUrl);
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
			RunnerLogger.Warn("Startup", $"Manifest startup failed for '{manifestUrl}'", ex);
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
		_smsUiRuntime = new SmsUiRuntime(_uriResolver, uiUrl);
		await _smsUiRuntime.InitializeAsync();

		try
		{
			var loader = new SmlUiLoader(
				_nodeFactoryRegistry,
				_nodePropertyMapper,
				animationApi: null,
				configureActions: ConfigureProjectActions,
				uriResolver: _uriResolver);
			var rootControl = await loader.LoadFromUriAsync(uiUrl);
			AttachUi(rootControl);
			await EnsureRuntimeUiReadyStateAsync();
			Runtime.Logging.RunnerLogger.Info("UI", $"UI loaded from '{uiUrl}'.");
			await InvokeUiReadyHandlersAsync();
			_smsUiRuntime?.InvokeReady();
		}
		catch (Exception ex)
		{
			Runtime.Logging.RunnerLogger.Error("UI", $"Failed to load UI from '{uiUrl}'", ex);
		}
	}

	private async Task InvokeUiReadyHandlersAsync()
	{
		var dispatcher = _uiDispatcher;
		if (dispatcher is null)
		{
			return;
		}

		foreach (var module in _uiActionModules)
		{
			var moduleType = module.GetType();
			try
			{
				var asyncWithDispatcher = moduleType.GetMethod(
					"OnReadyAsync",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase,
					binder: null,
					types: [typeof(UiActionDispatcher)],
					modifiers: null);
				if (asyncWithDispatcher is not null)
				{
					if (asyncWithDispatcher.Invoke(module, [dispatcher]) is Task readyTask)
					{
						await readyTask;
					}
					continue;
				}

				var syncWithDispatcher = moduleType.GetMethod(
					"OnReady",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase,
					binder: null,
					types: [typeof(UiActionDispatcher)],
					modifiers: null);
				if (syncWithDispatcher is not null)
				{
					syncWithDispatcher.Invoke(module, [dispatcher]);
					continue;
				}

				var asyncNoArgs = moduleType.GetMethod(
					"OnReadyAsync",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase,
					binder: null,
					types: Type.EmptyTypes,
					modifiers: null);
				if (asyncNoArgs is not null)
				{
					if (asyncNoArgs.Invoke(module, null) is Task readyTask)
					{
						await readyTask;
					}
					continue;
				}

				var syncNoArgs = moduleType.GetMethod(
					"OnReady",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase,
					binder: null,
					types: Type.EmptyTypes,
					modifiers: null);
				syncNoArgs?.Invoke(module, null);
			}
			catch (Exception ex)
			{
				RunnerLogger.Warn("UI", $"Action module '{moduleType.FullName}' failed during OnReady invocation.", ex);
			}
		}
	}

	private async Task EnsureRuntimeUiReadyStateAsync()
	{
		if (_runtimeUiRoot is null)
		{
			return;
		}

		// Let deferred UI work (splitter/layout adjustments) settle before SMS ready().
		if (GetTree() is { } tree)
		{
			await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
		}

		LayoutRuntime.Apply(_runtimeUiRoot);
	}

	private static IEnumerable<T> EnumerateDescendants<T>(Node root) where T : Node
	{
		var stack = new Stack<Node>();
		stack.Push(root);

		while (stack.Count > 0)
		{
			var current = stack.Pop();
			if (current is T typed)
			{
				yield return typed;
			}

			for (var i = current.GetChildCount() - 1; i >= 0; i--)
			{
				stack.Push(current.GetChild(i));
			}
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

	private string? ToManifestUrl(string urlOrBase)
	{
		var normalized = _uriResolver.Normalize(urlOrBase);
		var kind = SmlUriResolver.ClassifyScheme(normalized);

		if (kind is SmlUriSchemeKind.Http or SmlUriSchemeKind.Https)
		{
			if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
			{
				return null;
			}

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

		if (kind == SmlUriSchemeKind.Ipfs)
		{
			var manifestIpfs = normalized.EndsWith("/manifest.sml", StringComparison.OrdinalIgnoreCase)
				? normalized
				: normalized.EndsWith(".sml", StringComparison.OrdinalIgnoreCase)
					? string.Empty
					: normalized.TrimEnd('/') + "/manifest.sml";

			if (string.IsNullOrWhiteSpace(manifestIpfs))
			{
				return null;
			}

			return SmlUriResolver.MapIpfsToHttp(manifestIpfs, _uriResolver.IpfsGateway);
		}

		return null;
	}

	private async Task<string?> TryResolveDirectUiAsync(string uiUrl)
	{
		if (string.IsNullOrWhiteSpace(uiUrl))
		{
			return null;
		}

		try
		{
			var normalized = _uriResolver.ResolveReference(uiUrl);
			var resolved = await _uriResolver.ResolveForResourceLoadAsync(normalized);
			var resolvedKind = SmlUriResolver.ClassifyScheme(resolved);

			if (SmlUriResolver.IsLocalScheme(resolvedKind)
				&& TryResolveLocalPath(resolved, out var localPath)
				&& !File.Exists(localPath))
			{
				RunnerLogger.Warn("Startup", $"Resolved direct UI does not exist: {localPath}");
				return null;
			}

			return resolved;
		}
		catch (Exception ex)
		{
			RunnerLogger.Warn("Startup", $"Direct UI resolve failed for '{uiUrl}'", ex);
			return null;
		}
	}

	private static bool TryResolveLocalPath(string uriOrPath, out string localPath)
	{
		localPath = string.Empty;
		var kind = SmlUriResolver.ClassifyScheme(uriOrPath);
		switch (kind)
		{
			case SmlUriSchemeKind.Res:
			case SmlUriSchemeKind.User:
				localPath = ProjectSettings.GlobalizePath(uriOrPath);
				return true;

			case SmlUriSchemeKind.File:
				if (Uri.TryCreate(uriOrPath, UriKind.Absolute, out var fileUri))
				{
					localPath = fileUri.LocalPath;
					return true;
				}

				if (Path.IsPathRooted(uriOrPath))
				{
					localPath = uriOrPath;
					return true;
				}

				return false;

			default:
				return false;
		}
	}

	private static string BuildFallbackAppFileUrl()
	{
		var fallbackPath = ProjectSettings.GlobalizePath("res://fallback/app.sml");
		return new Uri(fallbackPath).AbsoluteUri;
	}

	private StartupOptions ParseStartupOptions()
	{
		var args = CollectStartupArgs();
		string? urlOverride = null;
		var clearCache = false;
		var resetStartUrl = false;

		for (var i = 0; i < args.Count; i++)
		{
			var arg = args[i];
			if (arg == "--url" && i + 1 < args.Count)
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
				continue;
			}

			if (!arg.StartsWith("--", StringComparison.Ordinal)
				&& string.IsNullOrWhiteSpace(urlOverride)
				&& LooksLikeUrlOrPath(arg))
			{
				urlOverride = arg;
			}
		}

		return new StartupOptions(urlOverride, clearCache, resetStartUrl);
	}

	private static List<string> CollectStartupArgs()
	{
		var args = new List<string>();

		try
		{
			var userArgs = OS.GetCmdlineUserArgs();
			if (userArgs is not null)
			{
				args.AddRange(userArgs);
			}
		}
		catch
		{
			// Fallback to GetCmdlineArgs only on platforms/runtimes where GetCmdlineUserArgs is unavailable.
		}

		var rawArgs = OS.GetCmdlineArgs();
		if (rawArgs is not null)
		{
			args.AddRange(rawArgs);
		}

		return args;
	}

	private static bool LooksLikeUrlOrPath(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}

		var kind = SmlUriResolver.ClassifyScheme(value);
		return kind is not (SmlUriSchemeKind.Relative or SmlUriSchemeKind.Unknown);
	}

	private async Task<StartupSettings> LoadStartupSettingsAsync()
	{
		var path = ProjectSettings.GlobalizePath($"user://{StartupSettingsFileName}");
		if (!File.Exists(path))
		{
			var defaults = new StartupSettings();
			await SaveStartupSettingsAsync(defaults);
			RunnerLogger.Info("Startup", $"Created default startup settings at '{path}'.");
			return defaults;
		}

		try
		{
			var content = await File.ReadAllTextAsync(path);
			return ParseStartupSettingsSml(content);
		}
		catch (Exception ex)
		{
			RunnerLogger.Warn("Startup", "Failed to load startup settings from SML. Using defaults", ex);
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
		public bool IncludeStackTraces { get; set; }
		public bool ShowParserWarnings { get; set; } = true;
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

		if (root.TryGetProperty("includeStackTraces", out var includeStackTracesValue))
		{
			settings.IncludeStackTraces = includeStackTracesValue.AsBoolOrThrow("includeStackTraces");
		}

		if (root.TryGetProperty("showParserWarnings", out var showParserWarningsValue))
		{
			settings.ShowParserWarnings = showParserWarningsValue.AsBoolOrThrow("showParserWarnings");
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
		builder.AppendLine($"    includeStackTraces: {settings.IncludeStackTraces.ToString().ToLowerInvariant()}");
		builder.AppendLine($"    showParserWarnings: {settings.ShowParserWarnings.ToString().ToLowerInvariant()}");
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
		panel.SetAnchorsPreset(Control.LayoutPreset.Center);
		panel.OffsetLeft = -320;
		panel.OffsetTop = -55;
		panel.OffsetRight = 320;
		panel.OffsetBottom = 55;

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
		_uiDispatcher = dispatcher;
		_smsUiRuntime?.BindDispatcher(dispatcher);

		foreach (var module in _uiActionModules)
		{
			try
			{
				module.Configure(dispatcher);
			}
			catch (Exception ex)
			{
				RunnerLogger.Error("UI", $"Action module '{module.GetType().FullName}' failed during Configure", ex);
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

		dispatcher.RegisterActionHandlerIfMissing("treeItemSelected", ctx =>
		{
			InvokeTreeConventionHandlers("treeItemSelected", ctx);
		});

		dispatcher.RegisterActionHandlerIfMissing("treeItemToggle", ctx =>
		{
			InvokeTreeConventionHandlers("treeItemToggled", ctx);
		});

		dispatcher.RegisterActionHandlerIfMissing("treeItemToggled", ctx =>
		{
			InvokeTreeConventionHandlers("treeItemToggled", ctx);
		});
	}

	private void InvokeTreeConventionHandlers(string methodName, UiActionContext ctx)
	{
		foreach (var module in _uiActionModules)
		{
			var moduleType = module.GetType();

			if (string.Equals(methodName, "treeItemSelected", StringComparison.OrdinalIgnoreCase))
			{
				var method = moduleType.GetMethod(
					methodName,
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase,
					binder: null,
					types: [typeof(Id), typeof(TreeViewItem)],
					modifiers: null);

				if (method is null)
				{
					continue;
				}

				try
				{
					method.Invoke(module, [ctx.SourceIdValue, ctx.TreeItem ?? new TreeViewItem { Text = string.Empty }]);
				}
				catch (Exception ex)
				{
					RunnerLogger.Warn("UI", $"Action module '{moduleType.FullName}' failed in '{methodName}'.", ex);
				}
			}
			else if (string.Equals(methodName, "treeItemToggled", StringComparison.OrdinalIgnoreCase))
			{
				var method = moduleType.GetMethod(
					methodName,
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase,
					binder: null,
					types: [typeof(Id), typeof(TreeViewItem), typeof(bool)],
					modifiers: null);

				if (method is null)
				{
					continue;
				}

				try
				{
					method.Invoke(module, [ctx.SourceIdValue, ctx.TreeItem ?? new TreeViewItem { Text = string.Empty }, ctx.BoolValue ?? false]);
				}
				catch (Exception ex)
				{
					RunnerLogger.Warn("UI", $"Action module '{moduleType.FullName}' failed in '{methodName}'.", ex);
				}
			}
		}
	}

	public bool TryGetDispatcher(out UiActionDispatcher dispatcher)
	{
		dispatcher = _uiDispatcher!;
		return dispatcher is not null;
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
		ApplyWindowProperties(rootControl);
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

		if (GetViewport() is { } viewport && !_viewportSizeChangedConnected)
		{
			viewport.SizeChanged += OnViewportSizeChanged;
			_viewportSizeChangedConnected = true;
		}

		OnViewportSizeChanged();
		LayoutRuntime.Apply(rootControl);
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

		if (_runtimeUiRoot is not null)
		{
			LayoutRuntime.Apply(_runtimeUiRoot);
		}

		LogUiScalingState(GetViewport()?.GetVisibleRect().Size ?? Vector2.Zero);
	}

	private void ApplyWindowProperties(Control rootControl)
	{
		if (GetWindow() is not { } window)
		{
			return;
		}

		if (rootControl.HasMeta(NodePropertyMapper.MetaWindowTitle))
		{
			window.Title = rootControl.GetMeta(NodePropertyMapper.MetaWindowTitle).AsString();
			RunnerLogger.Info("UI", $"Window title applied: '{window.Title}'");
		}

		if (rootControl.HasMeta(NodePropertyMapper.MetaWindowSizeX)
			&& rootControl.HasMeta(NodePropertyMapper.MetaWindowSizeY))
		{
			var width = Math.Max(1, rootControl.GetMeta(NodePropertyMapper.MetaWindowSizeX).AsInt32());
			var height = Math.Max(1, rootControl.GetMeta(NodePropertyMapper.MetaWindowSizeY).AsInt32());
			window.Size = new Vector2I(width, height);
			RunnerLogger.Info("UI", $"Window size applied: {window.Size.X}x{window.Size.Y}");
		}

		if (rootControl.HasMeta(NodePropertyMapper.MetaWindowPosX)
			&& rootControl.HasMeta(NodePropertyMapper.MetaWindowPosY))
		{
			var x = rootControl.GetMeta(NodePropertyMapper.MetaWindowPosX).AsInt32();
			var y = rootControl.GetMeta(NodePropertyMapper.MetaWindowPosY).AsInt32();
			window.Position = new Vector2I(x, y);
			RunnerLogger.Info("UI", $"Window position applied: {window.Position.X},{window.Position.Y}");
		}

		if (!rootControl.HasMeta(NodePropertyMapper.MetaWindowMinSizeX)
			|| !rootControl.HasMeta(NodePropertyMapper.MetaWindowMinSizeY))
		{
			return;
		}

		var minWidth = rootControl.GetMeta(NodePropertyMapper.MetaWindowMinSizeX).AsInt32();
		var minHeight = rootControl.GetMeta(NodePropertyMapper.MetaWindowMinSizeY).AsInt32();
		window.MinSize = new Vector2I(Math.Max(0, minWidth), Math.Max(0, minHeight));
		RunnerLogger.Info("UI", $"Window minSize applied: {window.MinSize.X}x{window.MinSize.Y}");
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

		_runtimeUiRoot.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_runtimeUiRoot.SetOffsetsPreset(Control.LayoutPreset.FullRect);
		_runtimeUiRoot.Position = Vector2.Zero;
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

		if (_fixedUiTexture is not null)
		{
			_fixedUiTexture.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			_fixedUiTexture.SetOffsetsPreset(Control.LayoutPreset.FullRect);
			_fixedUiTexture.Position = Vector2.Zero;
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
