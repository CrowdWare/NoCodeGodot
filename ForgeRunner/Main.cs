/*
 * Copyright (C) 2026 CrowdWare
 *
 * This file is part of ForgeRunner.
 *
 *  ForgeRunner is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  ForgeRunner is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with ForgeRunner.  If not, see <http://www.gnu.org/licenses/>.
 */

using Godot;
using Runtime.Logging;
using Runtime.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Runtime.Assets;
using Runtime.Sml;
using System.Linq;

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
	private const string DefaultStartUrl = "https://crowdware.github.io/Forge/Default/manifest.sml";
	private const string StartupSettingsFileName = "startup_settings.sml";
	private CanvasLayer? _startupProgressLayer;
	private ProgressBar? _startupProgressBar;
	private Label? _startupProgressLabel;
	private bool _viewportSizeChangedConnected;
	private CanvasLayer? _runtimeUiLayer;
	private Window? _mainAppWindow; // separate OS window created after splash; null until first SwapToUiAsync

	/// <summary>Window that hosts the main app UI. Null during splash → root Godot window.</summary>
	private Window ActiveAppWindow => _mainAppWindow ?? GetWindow();

	/// <summary>Viewport of the active app window (root viewport during splash, main-window viewport after swap).</summary>
	private Viewport ActiveAppViewport => _mainAppWindow is not null ? (Viewport)_mainAppWindow : GetViewport();
	private Runtime.Manifest.ManifestDocument? _startupManifest;
	private readonly HashSet<string> _loadedRuntimePluginIds = new(StringComparer.OrdinalIgnoreCase);

	public override void _EnterTree()
	{
		// Ensure the viewport renders with a transparent background from the very
		// first frame. Combined with the project.godot clear-color setting and
		// window/size/transparent=true, the initial window is see-through rather than
		// black while the SML document is loading.
		RenderingServer.SetDefaultClearColor(Colors.Transparent);
		GetViewport().TransparentBg = true;
		base._EnterTree();
	}

	public override async void _Ready()
	{
		var totalSw = Stopwatch.StartNew();
		var sw = Stopwatch.StartNew();

		DiscoverUiActionModules();
		ConfigureWindowContentScale(UiScalingMode.Layout);

		var options = ParseStartupOptions();
		var startupSettings = await LoadStartupSettingsAsync();
		if (options.DebugOverride.HasValue)
		{
			startupSettings.ShowDebugLogs = options.DebugOverride.Value;
		}
		RunnerLogger.Configure(startupSettings.IncludeStackTraces, startupSettings.ShowParserWarnings, startupSettings.ShowDebugLogs, options.VerboseRequested);
		SmlParseRuntime.Configure(options.SmlNativeProbeEnabled);
		RunnerLogger.Debug("Perf", $"[Ready] settings={sw.ElapsedMilliseconds}ms"); sw.Restart();

		var theme = GD.Load<Theme>("res://theme.tres");
		if (theme is null)
		{
			RunnerLogger.Warn("Startup", "Theme 'res://theme.tres' konnte nicht geladen werden.");
		}
		else
		{
			GetWindow().Theme = theme;
			RunnerLogger.Debug("Startup", $"Theme loaded {theme}");
			RunnerLogger.Debug("Startup", $"Window.Theme now = {GetWindow().Theme}");
		}
		RunnerLogger.Debug("Perf", $"[Ready] theme={sw.ElapsedMilliseconds}ms"); sw.Restart();

		if (!LoadUiOnStartup)
		{
			return;
		}

		_resolvedStartupUiUrl = await ResolveEntryFileUrlAsync();
		RunnerLogger.Debug("Perf", $"[Ready] resolveEntryUrl={sw.ElapsedMilliseconds}ms url='{_resolvedStartupUiUrl}'"); sw.Restart();

		await RunUiStartup();
		RunnerLogger.Debug("Perf", $"[Ready] runUiStartup={sw.ElapsedMilliseconds}ms"); sw.Restart();

		// Phase 2: Restliche Assets laden (parallel zum SplashScreen-Timer)
		if (_runtimeUiRoot is not null && IsSplashScreenRoot(_runtimeUiRoot))
		{
			await RunSplashFlowAsync(_runtimeUiRoot);
			RunnerLogger.Debug("Perf", $"[Ready] splashFlow={sw.ElapsedMilliseconds}ms");
		}
		else if (_startupManifest is not null)
		{
			// Kein SplashScreen → restliche Assets mit bestehendem Overlay nachladen
			await SyncRemainingAssetsAsync(progressBar: null);
			RunnerLogger.Debug("Perf", $"[Ready] syncRemaining={sw.ElapsedMilliseconds}ms");
		}

		RunnerLogger.Debug("Perf", $"[Ready] TOTAL={totalSw.ElapsedMilliseconds}ms");
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMAbout)
		{
			DispatchMacAboutAsMenuItemSelected();
		}

		if (what == NotificationWMCloseRequest)
		{
			TrySaveSessionState();
		}

		base._Notification(what);
	}

	public override void _ExitTree()
	{
		TrySaveSessionState();
		base._ExitTree();
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

		RunnerLogger.Info("UI", "Received macOS About notification -> dispatch appMenu/about menu click event.");

		_uiDispatcher.Dispatch(new UiActionContext(
			Source: source,
			SourceId: "appMenu",
			SourceIdValue: IdRuntimeScope.GetOrCreate("appMenu"),
			Action: "menuItemSelected",
			Clicked: "about",
			ClickedIdValue: IdRuntimeScope.GetOrCreate("about")
		));
	}

	private void DetachUi()
	{
		if (_viewportSizeChangedConnected)
		{
			ActiveAppViewport.SizeChanged -= OnViewportSizeChanged;
			_viewportSizeChangedConnected = false;
		}

		if (_runtimeUiLayer is not null)
		{
			// RemoveChild is synchronous — node leaves the scene tree immediately and is no longer rendered.
			// QueueFree handles memory cleanup asynchronously at end of frame.
			RemoveChild(_runtimeUiLayer);
			_runtimeUiLayer.QueueFree();
			_runtimeUiLayer = null;
		}
		_runtimeUiHost   = null;
		_runtimeUiRoot   = null;
		_fixedUiViewport = null;
		_fixedUiTexture  = null;
		_uiScalingConfig = new UiScalingConfig(UiScalingMode.Layout, Vector2I.Zero);
	}

	private static bool IsSplashScreenRoot(Control control)
	{
		return control.HasMeta(NodePropertyMapper.MetaNodeName)
			&& string.Equals(
				control.GetMeta(NodePropertyMapper.MetaNodeName).AsString(),
				"SplashScreen",
				StringComparison.OrdinalIgnoreCase);
	}

	private async Task<string> ResolveEntryFileUrlAsync()
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
			?? (!string.IsNullOrWhiteSpace(UiSmlUrl) ? UiSmlUrl : null);

		if (!string.IsNullOrWhiteSpace(configuredStartUrl))
		{
			var normalized = _uriResolver.Normalize(configuredStartUrl);
			var kind = SmlUriResolver.ClassifyScheme(normalized);
			if (SmlUriResolver.IsLocalScheme(kind))
			{
				RunnerLogger.Debug("Startup", $"Local entry URL resolved directly: '{normalized}'");
				return normalized;
			}
		}

		if (!EnableStartupSync)
		{
			return ResolveLegacyStartupUiUrl(configuredStartUrl ?? string.Empty);
		}

		var manifestUrl = ToManifestUrl(configuredStartUrl ?? DefaultStartUrl);
		if (manifestUrl is null)
		{
			var direct = await TryResolveDirectUiAsync(configuredStartUrl ?? DefaultStartUrl);
			return direct ?? BuildFallbackAppFileUrl();
		}

		try
		{
			_startupManifest = await _manifestLoader.LoadAsync(manifestUrl);
			var entryPaths = AssetCacheManager.BuildEntryPathSet(_startupManifest);
			var entryResult = await _assetCacheManager.SyncAsync(
				_startupManifest, entryPathsOnly: entryPaths);
			if (!string.IsNullOrWhiteSpace(entryResult.EntryFileUrl))
			{
			RunnerLogger.Debug("Startup", $"Entry file synced: '{entryResult.EntryFileUrl}'");
				return entryResult.EntryFileUrl;
			}
		}
		catch (Exception ex)
		{
			var offline = IsOfflineException(ex);
			RunnerLogger.Warn("Startup", $"Entry-file sync failed for '{manifestUrl}'", ex);
			RunnerLogger.Info("Startup", $"Offline: {offline}");
			var cached = _assetCacheManager.TryGetCachedEntryUrl(manifestUrl);
			if (!string.IsNullOrWhiteSpace(cached))
			{
				return cached;
			}
		}

		return BuildFallbackAppFileUrl();
	}

	private async Task SyncRemainingAssetsAsync(ProgressBar? progressBar)
	{
		if (_startupManifest is null)
		{
			return;
		}

		var syncPlan = await _assetCacheManager.BuildSyncPlanAsync(_startupManifest);
		if (syncPlan.DownloadCount == 0)
		{
			return;
		}

		IProgress<AssetSyncProgress>? reporter;

		if (progressBar is not null)
		{
			reporter = new Progress<AssetSyncProgress>(p =>
			{
				if (!IsInstanceValid(progressBar)) return;
				progressBar.Visible  = p.TotalCount > 0;
				progressBar.MaxValue = Math.Max(1, p.TotalCount);
				progressBar.Value    = Math.Min(p.CompletedCount, p.TotalCount);
			});

			try
			{
				await _assetCacheManager.SyncAsync(_startupManifest, progress: reporter);
			}
			finally
			{
				if (IsInstanceValid(progressBar))
				{
					progressBar.Visible = false;
				}
			}
		}
		else
		{
			ShowStartupProgressOverlay(syncPlan);
			reporter = new Progress<AssetSyncProgress>(ReportStartupProgress);
			try
			{
				await _assetCacheManager.SyncAsync(_startupManifest, progress: reporter);
			}
			finally
			{
				HideStartupProgressOverlay();
			}
		}
	}

	private async Task RunSplashFlowAsync(Control splashRoot)
	{
		var sw = Stopwatch.StartNew();

		var durationMs = splashRoot.HasMeta(NodePropertyMapper.MetaSplashDuration)
			? splashRoot.GetMeta(NodePropertyMapper.MetaSplashDuration).AsInt32()
			: 0;
		var loadOnReady = splashRoot.HasMeta(NodePropertyMapper.MetaSplashLoadOnReady)
			? splashRoot.GetMeta(NodePropertyMapper.MetaSplashLoadOnReady).AsString()
			: string.Empty;

		// Resolve relative loadOnReady URLs against the current document's URL
		if (!string.IsNullOrEmpty(loadOnReady) && !string.IsNullOrEmpty(_resolvedStartupUiUrl))
		{
			loadOnReady = _uriResolver.ResolveReference(loadOnReady, _resolvedStartupUiUrl);
		}

		var progressBar = EnumerateDescendants<ProgressBar>(splashRoot).FirstOrDefault();

		var timerTask = durationMs > 0 ? Task.Delay(durationMs) : Task.CompletedTask;
		var syncTask  = SyncRemainingAssetsAsync(progressBar);

		await Task.WhenAll(timerTask, syncTask);
		RunnerLogger.Debug("Perf", $"[SplashFlow] timer+sync={sw.ElapsedMilliseconds}ms (duration={durationMs}ms)"); sw.Restart();

		if (!string.IsNullOrEmpty(loadOnReady))
		{
			// Load the next document while the splash is still visible, then swap
			// atomically — this avoids any black-screen gap between the two UIs.
			await SwapToUiAsync(loadOnReady);
			RunnerLogger.Debug("Perf", $"[SplashFlow] swapToUi='{System.IO.Path.GetFileName(loadOnReady)}' {sw.ElapsedMilliseconds}ms");
		}
	}

	/// <summary>
	/// Pre-loads a new SML document, then atomically swaps out the current UI
	/// (e.g. SplashScreen) for the new one without an intermediate blank frame.
	/// </summary>
	private async Task SwapToUiAsync(string url)
	{
		var sw = Stopwatch.StartNew();

		var newRuntime = new SmsUiRuntime(_uriResolver, url);
		await newRuntime.InitializeAsync();
		RunnerLogger.Debug("Perf", $"[SwapToUi] runtimeInit={sw.ElapsedMilliseconds}ms"); sw.Restart();

		var loader = new SmlUiLoader(
			_nodeFactoryRegistry,
			_nodePropertyMapper,
			animationApi: null,
			configureActions: ConfigureProjectActions,
			uriResolver: _uriResolver);

		Control? rootControl;
		try
		{
			rootControl = await loader.LoadFromUriAsync(url);
		}
		catch (Exception ex)
		{
			RunnerLogger.Error("UI", $"Failed to load UI from '{url}'", ex);
			return;
		}
		RunnerLogger.Debug("Perf", $"[SwapToUi] loadFromUri={sw.ElapsedMilliseconds}ms"); sw.Restart();

		_resolvedStartupUiUrl = url;
		_smsUiRuntime = newRuntime;
		// ConfigureProjectActions was called during LoadFromUriAsync while _smsUiRuntime
		// still pointed to the old runtime — rebind the dispatcher to the new runtime now.
		if (_uiDispatcher != null)
			_smsUiRuntime?.BindDispatcher(_uiDispatcher);

		// Detect whether we're leaving a splash screen (first-ever swap).
		var isFromSplash = _runtimeUiRoot is not null && IsSplashScreenRoot(_runtimeUiRoot);

		// Before detaching, strip the splash window flags so it no longer sits on top
		// of other windows while we build the main UI.
		if (isFromSplash)
		{
			GetWindow().SetFlag(Window.Flags.AlwaysOnTop, false);
		}

		// DetachUi BEFORE creating _mainAppWindow: ActiveAppViewport still points to the
		// splash (root-window) viewport here, so SizeChanged is disconnected correctly.
		DetachUi();

		// On first splash→main transition, create a dedicated native OS window.
		// project.godot: window/subwindows/embed_subwindows=false makes this a real OS window.
		if (isFromSplash && _mainAppWindow is null)
		{
			var mainWin = new Window { Visible = false, Name = "MainAppWindow" };
			mainWin.CloseRequested += () => GetTree().Quit();
			AddChild(mainWin); // add to scene tree before AttachUi uses it
			_mainAppWindow = mainWin;
		}

		// After the first swap, always target _mainAppWindow (null → root window for apps without splash).
		AttachUi(rootControl, _mainAppWindow);
		RunnerLogger.Debug("Perf", $"[SwapToUi] attachUi={sw.ElapsedMilliseconds}ms"); sw.Restart();

		await EnsureRuntimeUiReadyStateAsync();
		RunnerLogger.Debug("UI", $"UI loaded from '{url}'.");
		await InvokeUiReadyHandlersAsync();
		await LoadRuntimePluginsAsync(url);
		TryRestoreDockingStateOnly();
		_smsUiRuntime?.InvokeReady();
		ApplyDefaultHiddenPanels();

		// Reveal the main window only after it is fully ready, then banish the splash
		// (root) window. Godot forbids Visible=false on the root OS window, so we shrink
		// it to 1×1 and move it far off-screen — effectively invisible.
		if (isFromSplash && _mainAppWindow is not null)
		{
			_mainAppWindow.Visible = true;
			var splashWin = GetWindow();
			splashWin.Size      = new Vector2I(1, 1);
			splashWin.Position  = new Vector2I(-32000, -32000);
			RunnerLogger.Debug("UI", "Main app window revealed; splash window banished.");
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
			Runtime.Logging.RunnerLogger.Debug("UI", $"UI loaded from '{uiUrl}'.");
			await InvokeUiReadyHandlersAsync();
			await LoadRuntimePluginsAsync(uiUrl);
			TryRestoreDockingStateOnly();
			_smsUiRuntime?.InvokeReady();
			ApplyDefaultHiddenPanels();
		}
		catch (Exception ex)
		{
			Runtime.Logging.RunnerLogger.Error("UI", $"Failed to load UI from '{uiUrl}'", ex);
		}
	}

	private sealed class RuntimePluginDescriptor
	{
		public string Id { get; init; } = string.Empty;
		public bool Enabled { get; init; } = true;
		public string Title { get; init; } = string.Empty;
		public string Dock { get; init; } = "dockRightBottom";
		public string PanelSmlPath { get; init; } = string.Empty;
		public string SmsPath { get; init; } = string.Empty;
		public string AssemblyPath { get; init; } = string.Empty;
	}

	private async Task LoadRuntimePluginsAsync(string uiUrl)
	{
		if (_runtimeUiRoot is null)
		{
			return;
		}

		// Plugins rely on the runtime editor UI (DockingHost + known control ids).
		// Skip loading during splash/minimal UIs (e.g. app.sml) and load on the next
		// UI swap once the full docking layout is active.
		var dockingContainers = CollectDockingContainers(_runtimeUiRoot).ToList();
		if (dockingContainers.Count == 0)
		{
			RunnerLogger.Debug("Plugin", $"Skipping plugin load for '{uiUrl}' (no docking containers in active UI root).");
			return;
		}

		_loadedRuntimePluginIds.Clear();
		var descriptors = DiscoverRuntimePlugins(uiUrl);
		foreach (var descriptor in descriptors)
		{
			if (!descriptor.Enabled || string.IsNullOrWhiteSpace(descriptor.Id))
			{
				continue;
			}

			if (!_loadedRuntimePluginIds.Add(descriptor.Id))
			{
				RunnerLogger.Warn("Plugin", $"Skipping duplicate plugin id '{descriptor.Id}'.");
				continue;
			}

			if (!string.IsNullOrWhiteSpace(descriptor.PanelSmlPath))
			{
				Control? panel;
				try
				{
					panel = BuildPluginPanelControl(descriptor.PanelSmlPath);
				}
				catch (Exception ex)
				{
					RunnerLogger.Warn("Plugin", $"Failed to build plugin panel for '{descriptor.Id}'.", ex);
					panel = null;
				}

				if (panel is not null && !TryAttachPluginPanel(descriptor, panel))
				{
					RunnerLogger.Warn("Plugin", $"Failed to attach plugin panel for '{descriptor.Id}'.");
				}
			}

			if (_smsUiRuntime is not null && !string.IsNullOrWhiteSpace(descriptor.SmsPath))
			{
				try
				{
					await _smsUiRuntime.LoadAdditionalScriptFromUriAsync(new Uri(descriptor.SmsPath).AbsoluteUri);
				}
				catch (Exception ex)
				{
					RunnerLogger.Warn("Plugin", $"Failed to load plugin SMS for '{descriptor.Id}'.", ex);
				}
			}

			if (!string.IsNullOrWhiteSpace(descriptor.AssemblyPath))
			{
				try
				{
					if (File.Exists(descriptor.AssemblyPath))
					{
						Assembly.LoadFrom(descriptor.AssemblyPath);
						RunnerLogger.Info("Plugin", $"Loaded plugin assembly for '{descriptor.Id}': {descriptor.AssemblyPath}");
					}
					else
					{
						RunnerLogger.Warn("Plugin", $"Assembly for plugin '{descriptor.Id}' not found: {descriptor.AssemblyPath}");
					}
				}
				catch (Exception ex)
				{
					RunnerLogger.Warn("Plugin", $"Failed loading assembly for plugin '{descriptor.Id}'.", ex);
				}
			}
		}
	}

	private List<RuntimePluginDescriptor> DiscoverRuntimePlugins(string uiUrl)
	{
		var result = new List<RuntimePluginDescriptor>();
		var uiDirectory = ResolveUiDirectory(uiUrl);
		if (string.IsNullOrWhiteSpace(uiDirectory))
		{
			return result;
		}

		var pluginsRoot = Path.Combine(uiDirectory, "plugins");
		if (!Directory.Exists(pluginsRoot))
		{
			return result;
		}

		foreach (var pluginDescriptorPath in Directory.EnumerateFiles(pluginsRoot, "plugin.sml", SearchOption.AllDirectories))
		{
			try
			{
				var descriptor = ParseRuntimePluginDescriptor(pluginDescriptorPath);
				if (descriptor is not null)
				{
					result.Add(descriptor);
				}
			}
			catch (Exception ex)
			{
				RunnerLogger.Warn("Plugin", $"Failed to parse plugin descriptor '{pluginDescriptorPath}'.", ex);
			}
		}

		return result;
	}

	private static string ResolveUiDirectory(string uiUrl)
	{
		if (string.IsNullOrWhiteSpace(uiUrl))
		{
			return string.Empty;
		}

		if (Uri.TryCreate(uiUrl, UriKind.Absolute, out var uri) && uri.IsFile)
		{
			return Path.GetDirectoryName(uri.LocalPath) ?? string.Empty;
		}

		try
		{
			if (File.Exists(uiUrl))
			{
				return Path.GetDirectoryName(Path.GetFullPath(uiUrl)) ?? string.Empty;
			}
		}
		catch
		{
			// Ignore and fall back to empty.
		}

		return string.Empty;
	}

	private static RuntimePluginDescriptor? ParseRuntimePluginDescriptor(string descriptorPath)
	{
		var text = File.ReadAllText(descriptorPath, Encoding.UTF8);
		var doc = SmlParseRuntime.ParseDocument(text, context: "PluginDescriptor");
		SmlNode? pluginNode = null;
		foreach (var node in doc.Roots)
		{
			if (string.Equals(node.Name, "Plugin", StringComparison.OrdinalIgnoreCase))
			{
				pluginNode = node;
				break;
			}
		}

		if (pluginNode is null)
		{
			RunnerLogger.Warn("Plugin", $"Descriptor '{descriptorPath}' has no Plugin root.");
			return null;
		}

		var pluginDir = Path.GetDirectoryName(descriptorPath) ?? string.Empty;
		var id = GetSmlString(pluginNode, "id", string.Empty);
		if (string.IsNullOrWhiteSpace(id))
		{
			RunnerLogger.Warn("Plugin", $"Descriptor '{descriptorPath}' missing required 'id'.");
			return null;
		}

		var panelSmlRelative = GetSmlString(pluginNode, "panelSml", string.Empty);
		var smsRelative = GetSmlString(pluginNode, "sms", string.Empty);
		var assemblyRelative = GetSmlString(pluginNode, "assemblyPath", string.Empty);
		var title = GetSmlString(pluginNode, "title", id);
		var dock = GetSmlString(pluginNode, "dock", "dockRightBottom");
		var enabled = GetSmlBool(pluginNode, "enabled", true);

		return new RuntimePluginDescriptor
		{
			Id = id,
			Enabled = enabled,
			Title = title,
			Dock = dock,
			PanelSmlPath = string.IsNullOrWhiteSpace(panelSmlRelative)
				? string.Empty
				: Path.GetFullPath(Path.Combine(pluginDir, panelSmlRelative)),
			SmsPath = string.IsNullOrWhiteSpace(smsRelative)
				? string.Empty
				: Path.GetFullPath(Path.Combine(pluginDir, smsRelative)),
			AssemblyPath = string.IsNullOrWhiteSpace(assemblyRelative)
				? string.Empty
				: Path.GetFullPath(Path.Combine(pluginDir, assemblyRelative))
		};
	}

	private Control BuildPluginPanelControl(string panelSmlPath)
	{
		var content = File.ReadAllText(panelSmlPath, Encoding.UTF8);
		var schema = SmlSchemaFactory.CreateDefault();
		var document = SmlParseRuntime.ParseDocument(content, schema, context: "PluginPanel");
		foreach (var warning in document.Warnings)
		{
			RunnerLogger.ParserWarning(warning);
		}

		var panelSmlUri = new Uri(panelSmlPath).AbsoluteUri;
		Func<string, string> resolveAssetPath = source =>
		{
			try
			{
				return _uriResolver.ResolveForResourceLoadAsync(source, panelSmlUri).GetAwaiter().GetResult();
			}
			catch
			{
				return source;
			}
		};

		var builder = new SmlUiBuilder(
			_nodeFactoryRegistry,
			_nodePropertyMapper,
			animationApi: new Runtime.ThreeD.AnimationControlApi(),
			localization: LocalizationStore.Empty,
			resolveAssetPath: resolveAssetPath);

		BridgePluginActionsToMainDispatcher(builder.Actions);
		var built = builder.Build(document);
		return built;
	}

	private void BridgePluginActionsToMainDispatcher(UiActionDispatcher pluginDispatcher)
	{
		if (_uiDispatcher is null)
		{
			return;
		}

		void Forward(UiActionContext ctx) => _uiDispatcher.Dispatch(ctx);
		pluginDispatcher.RegisterActionHandler("buttonClicked", Forward);
		pluginDispatcher.RegisterActionHandler("lineEditTextChanged", Forward);
		pluginDispatcher.RegisterActionHandler("lineEditTextSubmitted", Forward);
		pluginDispatcher.RegisterActionHandler("treeItemSelected", Forward);
		pluginDispatcher.RegisterActionHandler("treeItemToggled", Forward);
		pluginDispatcher.RegisterActionHandler("menuItemSelected", Forward);
		pluginDispatcher.RegisterActionHandler("listItemSelected", Forward);
		pluginDispatcher.RegisterActionHandler("save", Forward);
	}

	private bool TryAttachPluginPanel(RuntimePluginDescriptor descriptor, Control panel)
	{
		if (_runtimeUiRoot is null)
		{
			return false;
		}

		var dockSide = MapPluginDockSide(descriptor.Dock);
		var target = CollectDockingContainers(_runtimeUiRoot)
			.FirstOrDefault(c => c.GetDockSideKind() == dockSide);
		if (target is null)
		{
			target = CollectDockingContainers(_runtimeUiRoot)
				.FirstOrDefault(c => c.GetDockSideKind() == DockingContainerControl.DockSideKind.RightBottom);
		}

		if (target is null)
		{
			RunnerLogger.Warn("Plugin", $"No target docking container found for plugin '{descriptor.Id}'.");
			return false;
		}

		if (!panel.HasMeta(NodePropertyMapper.MetaId))
		{
			panel.SetMeta(NodePropertyMapper.MetaId, Variant.From($"{descriptor.Id}Panel"));
		}
		panel.Name = string.IsNullOrWhiteSpace(panel.Name) ? $"{descriptor.Id}Panel" : panel.Name;
		target.AddDockTab(panel, descriptor.Title);
		target.Visible = true;
		RunnerLogger.Debug("Plugin", $"Loaded plugin panel '{descriptor.Id}' into '{dockSide}'.");
		return true;
	}

	private static DockingContainerControl.DockSideKind MapPluginDockSide(string dock)
	{
		return dock.Trim().ToLowerInvariant() switch
		{
			"farleft" => DockingContainerControl.DockSideKind.FarLeft,
			"farleftbottom" => DockingContainerControl.DockSideKind.FarLeftBottom,
			"dockleft" => DockingContainerControl.DockSideKind.Left,
			"dockleftbottom" => DockingContainerControl.DockSideKind.LeftBottom,
			"dockfarleft" => DockingContainerControl.DockSideKind.FarLeft,
			"dockfarleftbottom" => DockingContainerControl.DockSideKind.FarLeftBottom,
			"dockright" => DockingContainerControl.DockSideKind.Right,
			"dockrightbottom" => DockingContainerControl.DockSideKind.RightBottom,
			"farright" => DockingContainerControl.DockSideKind.FarRight,
			"farrightbottom" => DockingContainerControl.DockSideKind.FarRightBottom,
			"dockfarright" => DockingContainerControl.DockSideKind.FarRight,
			"dockfarrightbottom" => DockingContainerControl.DockSideKind.FarRightBottom,
			"center" => DockingContainerControl.DockSideKind.Center,
			_ => DockingContainerControl.DockSideKind.RightBottom
		};
	}

	private void ApplyDefaultHiddenPanels()
	{
		if (_runtimeUiRoot is null)
		{
			return;
		}

		var stack = new Stack<Node>();
		stack.Push(_runtimeUiRoot);
		while (stack.Count > 0)
		{
			var current = stack.Pop();
			if (current is DockingHostControl host)
			{
				host.HidePanelById("sourcePanel");
			}

			for (var i = current.GetChildCount() - 1; i >= 0; i--)
			{
				stack.Push(current.GetChild(i));
			}
		}
	}

	private static string GetSmlString(SmlNode node, string key, string fallback)
	{
		if (!node.TryGetProperty(key, out var value))
		{
			return fallback;
		}

		return value.Kind is SmlValueKind.String or SmlValueKind.Identifier
			? (string)value.Value
			: fallback;
	}

	private static bool GetSmlBool(SmlNode node, string key, bool fallback)
	{
		if (!node.TryGetProperty(key, out var value))
		{
			return fallback;
		}

		return value.Kind == SmlValueKind.Bool
			? (bool)value.Value
			: fallback;
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
			if (configuredStartUrl.Contains("/ForgeRunner/SampleProject/UI.sml", StringComparison.OrdinalIgnoreCase)
				|| configuredStartUrl.Contains("\\ForgeRunner\\SampleProject\\UI.sml", StringComparison.OrdinalIgnoreCase))
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
		bool? debugOverride = null;
		var verboseRequested = false;
		var smlNativeProbeEnabled = false;

		for (var i = 0; i < args.Count; i++)
		{
			var arg = args[i];
			if (arg.StartsWith("--debug=", StringComparison.Ordinal))
			{
				var raw = arg.Substring("--debug=".Length);
				if (TryParseBoolArg(raw, out var parsed))
				{
					debugOverride = parsed;
				}
				continue;
			}
			if (arg == "--verbose")
			{
				verboseRequested = true;
				continue;
			}
			if (arg.StartsWith("--sml-native=", StringComparison.Ordinal))
			{
				var raw = arg.Substring("--sml-native=".Length);
				if (TryParseBoolArg(raw, out var parsed))
				{
					smlNativeProbeEnabled = parsed;
				}
				continue;
			}

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

		return new StartupOptions(urlOverride, clearCache, resetStartUrl, debugOverride, verboseRequested, smlNativeProbeEnabled);
	}

	private static bool TryParseBoolArg(string raw, out bool value)
	{
		var normalized = (raw ?? string.Empty).Trim().ToLowerInvariant();
		switch (normalized)
		{
			case "1":
			case "true":
			case "yes":
			case "on":
				value = true;
				return true;
			case "0":
			case "false":
			case "no":
			case "off":
				value = false;
				return true;
			default:
				value = false;
				return false;
		}
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

	private sealed record StartupOptions(string? UrlOverride, bool ClearCache, bool ResetStartUrl, bool? DebugOverride, bool VerboseRequested, bool SmlNativeProbeEnabled);

	private sealed class StartupSettings
	{
		public string? StartUrl { get; set; }
		public int ProgressThresholdMb { get; set; } = 10;
		public bool IncludeStackTraces { get; set; }
		public bool ShowParserWarnings { get; set; } = true;
		public bool ShowDebugLogs { get; set; }
	}

	private static StartupSettings ParseStartupSettingsSml(string content)
	{
		var schema = new SmlParserSchema();
		schema.RegisterKnownNode("StartupSettings");
		schema.WarnOnUnknownNodes = true;

		var document = SmlParseRuntime.ParseDocument(content, schema, context: "StartupSettings");

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

		if (root.TryGetProperty("showDebugLogs", out var showDebugLogsValue))
		{
			settings.ShowDebugLogs = showDebugLogsValue.AsBoolOrThrow("showDebugLogs");
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
		builder.AppendLine($"    showDebugLogs: {settings.ShowDebugLogs.ToString().ToLowerInvariant()}");
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

	private void AttachUi(Control rootControl, Window? targetWindow = null)
	{
		// targetWindow = separate OS window (main app after splash swap)
		// null           = root Godot window (used for splash or non-splash first load)
		var appWindow = targetWindow ?? GetWindow();
		ApplyWindowProperties(appWindow, rootControl);
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

		var inheritedTheme = GetWindow()?.Theme;
		if (inheritedTheme is not null)
		{
			if (targetWindow is not null)
				targetWindow.Theme = inheritedTheme;
			host.Theme = inheritedTheme;
			rootControl.Theme = inheritedTheme;
			RunnerLogger.Debug("UI", "Applied window theme directly to RuntimeUiHost and SmlRoot.");
		}
		else
		{
			RunnerLogger.Warn("UI", "Window theme is null while attaching runtime UI.");
		}

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
		// Add CanvasLayer to the target window node (or to this Main node for root-window rendering)
		if (targetWindow is not null)
			targetWindow.AddChild(layer);
		else
			AddChild(layer);

		_runtimeUiLayer = layer;
		_runtimeUiHost = host;
		_runtimeUiRoot = rootControl;

		var viewportToWatch = targetWindow is not null ? (Viewport)targetWindow : GetViewport();
		if (viewportToWatch is not null && !_viewportSizeChangedConnected)
		{
			viewportToWatch.SizeChanged += OnViewportSizeChanged;
			_viewportSizeChangedConnected = true;
		}

		OnViewportSizeChanged();
		TryRestoreSessionState();
		LayoutRuntime.Apply(rootControl);
		LogUiScalingState(ActiveAppViewport?.GetVisibleRect().Size ?? Vector2.Zero);
	}

	private void TryRestoreSessionState()
	{
		var state = LoadSessionState();
		if (state is null)
		{
			return;
		}

		if (state.Window is not null)
		{
			var currentWindowId = _runtimeUiRoot is not null ? ResolveControlKey(_runtimeUiRoot) : string.Empty;
			var savedWindowId = state.Window.WindowId;
			var bothHaveNoId = string.IsNullOrWhiteSpace(savedWindowId) && string.IsNullOrWhiteSpace(currentWindowId);
			if (bothHaveNoId || savedWindowId == currentWindowId)
			{
				ApplyWindowState(state.Window);
			}
		}

		if (state.Docking is not null && _runtimeUiRoot is not null && ContainsDockingHost(_runtimeUiRoot))
		{
			ApplyDockingState(_runtimeUiRoot, state.Docking);
		}
	}

	private void TryRestoreDockingStateOnly()
	{
		if (_runtimeUiRoot is null || !ContainsDockingHost(_runtimeUiRoot))
		{
			return;
		}

		var state = LoadSessionState();
		if (state?.Docking is null)
		{
			return;
		}

		ApplyDockingState(_runtimeUiRoot, state.Docking);
	}

	private void TrySaveSessionState()
	{
		if (_runtimeUiRoot is null)
		{
			return;
		}

		var state = new UiSessionState
		{
			Window = CaptureWindowState()
		};

		if (ContainsDockingHost(_runtimeUiRoot))
		{
			state.Docking = CaptureDockingState(_runtimeUiRoot);
		}

		SaveSessionState(state);
	}

	private UiWindowState? CaptureWindowState()
	{
		var window = ActiveAppWindow;
		if (window is null)
		{
			return null;
		}

		return new UiWindowState
		{
			WindowId = _runtimeUiRoot is not null ? ResolveControlKey(_runtimeUiRoot) : string.Empty,
			Screen = window.CurrentScreen,
			PositionX = window.Position.X,
			PositionY = window.Position.Y,
			SizeX = window.Size.X,
			SizeY = window.Size.Y,
			Mode = window.Mode.ToString()
		};
	}

	private void ApplyWindowState(UiWindowState windowState)
	{
		var window = ActiveAppWindow;
		if (window is null)
		{
			return;
		}

		var parsedMode = Window.ModeEnum.Windowed;
		var hasMode = !string.IsNullOrWhiteSpace(windowState.Mode)
			&& Enum.TryParse(windowState.Mode, true, out parsedMode);

		if (hasMode && parsedMode != Window.ModeEnum.Windowed && window.Mode != parsedMode)
		{
			window.Mode = parsedMode;
		}
		else if (window.Mode != Window.ModeEnum.Windowed)
		{
			window.Mode = Window.ModeEnum.Windowed;
		}

		if (windowState.SizeX > 0 && windowState.SizeY > 0)
		{
			window.Size = new Vector2I(windowState.SizeX, windowState.SizeY);
		}

		var screenCount = DisplayServer.GetScreenCount();
		if (windowState.Screen >= 0 && windowState.Screen < screenCount && window.CurrentScreen != windowState.Screen)
		{
			window.CurrentScreen = windowState.Screen;
		}

		window.Position = new Vector2I(windowState.PositionX, windowState.PositionY);
	}

	private UiDockingState CaptureDockingState(Control root)
	{
		var containers = CollectDockingContainers(root);
		var state = new UiDockingState();

		foreach (var container in containers)
		{
			var tabTitles = new List<string>();
			for (var i = 0; i < container.GetTabCount(); i++)
			{
				tabTitles.Add(container.GetTabTitle(i));
			}

			var currentTitle = string.Empty;
			var current = container.GetCurrentTab();
			if (current >= 0 && current < tabTitles.Count)
			{
				currentTitle = tabTitles[current];
			}

			state.Containers.Add(new UiDockingContainerState
			{
				Key = ResolveControlKey(container),
				Visible = container.Visible,
				DockSide = container.GetDockSide(),
				FixedWidth = (int)MathF.Round(container.GetFixedWidth()),
				FixedHeight = container.HasFixedHeight() ? (int?)MathF.Round(container.GetFixedHeight()) : null,
				HeightPercent = container.HasHeightPercent() ? container.GetHeightPercent() : null,
				TabTitles = tabTitles,
				CurrentTitle = currentTitle
			});
		}

		return state;
	}

	private void ApplyDockingState(Control root, UiDockingState dockingState)
	{
		var containers = CollectDockingContainers(root);
		var byKey = new Dictionary<string, DockingContainerControl>(StringComparer.Ordinal);
		foreach (var container in containers)
		{
			byKey[ResolveControlKey(container)] = container;
		}

		foreach (var saved in dockingState.Containers)
		{
			if (!byKey.TryGetValue(saved.Key, out var target))
			{
				continue;
			}

			target.Visible = saved.Visible;
			target.SetFixedWidth(saved.FixedWidth);
			if (saved.FixedHeight.HasValue)
			{
				target.SetFixedHeight(saved.FixedHeight.Value);
			}
			else if (saved.HeightPercent.HasValue)
			{
				target.SetHeightPercent(saved.HeightPercent.Value);
			}
			else
			{
				if (target.HasMeta(NodePropertyMapper.MetaDockFixedHeight))
				{
					target.RemoveMeta(NodePropertyMapper.MetaDockFixedHeight);
				}
				if (target.HasMeta(NodePropertyMapper.MetaDockHeightPercent))
				{
					target.RemoveMeta(NodePropertyMapper.MetaDockHeightPercent);
				}
			}
		}

		var titleMap = BuildTabTitleMap(containers);
		foreach (var saved in dockingState.Containers)
		{
			if (!byKey.TryGetValue(saved.Key, out var target))
			{
				continue;
			}

			foreach (var title in saved.TabTitles)
			{
				if (!titleMap.TryGetValue(title, out var sourceEntry))
				{
					continue;
				}

				if (ReferenceEquals(sourceEntry.Container, target))
				{
					continue;
				}

				sourceEntry.Container.RemoveDockTab(sourceEntry.Control);
				target.AddDockTab(sourceEntry.Control, title);
				titleMap[title] = (target, sourceEntry.Control);
			}

			if (!string.IsNullOrWhiteSpace(saved.CurrentTitle))
			{
				for (var i = 0; i < target.GetTabCount(); i++)
				{
					if (!string.Equals(target.GetTabTitle(i), saved.CurrentTitle, StringComparison.Ordinal))
					{
						continue;
					}

					target.SetCurrentTab(i);
					break;
				}
			}
		}
	}

	private static Dictionary<string, (DockingContainerControl Container, Control Control)> BuildTabTitleMap(List<DockingContainerControl> containers)
	{
		var result = new Dictionary<string, (DockingContainerControl Container, Control Control)>(StringComparer.Ordinal);
		foreach (var container in containers)
		{
			for (var i = 0; i < container.GetTabCount(); i++)
			{
				var title = container.GetTabTitle(i);
				if (string.IsNullOrWhiteSpace(title) || result.ContainsKey(title))
				{
					continue;
				}

				var control = container.GetTabControl(i);
				if (control is null)
				{
					continue;
				}

				result[title] = (container, control);
			}
		}

		return result;
	}

	private static List<DockingContainerControl> CollectDockingContainers(Control root)
	{
		var result = new List<DockingContainerControl>();
		var stack = new Stack<Node>();
		stack.Push(root);

		while (stack.Count > 0)
		{
			var current = stack.Pop();
			if (current is DockingContainerControl container)
			{
				result.Add(container);
			}

			for (var i = current.GetChildCount() - 1; i >= 0; i--)
			{
				stack.Push(current.GetChild(i));
			}
		}

		return result;
	}

	private static bool ContainsDockingHost(Control root)
	{
		var stack = new Stack<Node>();
		stack.Push(root);

		while (stack.Count > 0)
		{
			var current = stack.Pop();
			if (current is DockingHostControl)
			{
				return true;
			}

			for (var i = current.GetChildCount() - 1; i >= 0; i--)
			{
				stack.Push(current.GetChild(i));
			}
		}

		return false;
	}

	private static string ResolveControlKey(Control control)
	{
		if (control.HasMeta(NodePropertyMapper.MetaId))
		{
			var id = control.GetMeta(NodePropertyMapper.MetaId).AsString();
			if (!string.IsNullOrWhiteSpace(id))
			{
				return id;
			}
		}

		return control.Name;
	}

	private UiSessionState? LoadSessionState()
	{
		var path = GetUiSessionStatePath();
		if (!File.Exists(path))
		{
			return null;
		}

		try
		{
			var sml = File.ReadAllText(path);
			return DeserializeUiSessionStateFromSml(sml);
		}
		catch (Exception ex)
		{
			RunnerLogger.Warn("UI", "Failed to load ui session state. Ignoring persisted layout.", ex);
			return null;
		}
	}

	private void SaveSessionState(UiSessionState state)
	{
		var path = GetUiSessionStatePath();
		var temp = path + ".tmp";

		try
		{
			var sml = SerializeUiSessionStateToSml(state);
			var dir = Path.GetDirectoryName(path);
			if (!string.IsNullOrWhiteSpace(dir))
			{
				Directory.CreateDirectory(dir);
			}
			File.WriteAllText(temp, sml);
			if (File.Exists(path))
			{
				File.Delete(path);
			}

			File.Move(temp, path);
		}
		catch (Exception ex)
		{
			RunnerLogger.Warn("UI", "Failed to save ui session state.", ex);
		}
	}

	private string GetUiSessionStatePath()
	{
		var keySource = _resolvedStartupUiUrl;
		if (string.IsNullOrWhiteSpace(keySource))
		{
			keySource = !string.IsNullOrWhiteSpace(UiSmlUrl)
				? UiSmlUrl
				: (!string.IsNullOrWhiteSpace(ManifestUrl) ? ManifestUrl : DefaultStartUrl);
		}

		keySource = _uriResolver.Normalize(keySource);
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(keySource));
		var key = Convert.ToHexString(hash).ToLowerInvariant()[..16];
		return ProjectSettings.GlobalizePath($"user://layouts/ui_session_state_{key}.sml");
	}

	private static string SerializeUiSessionStateToSml(UiSessionState state)
	{
		var sb = new StringBuilder();
		sb.AppendLine("UiSessionState {");

		if (state.Window is not null)
		{
			sb.AppendLine("    Window {");
			sb.AppendLine($"        windowId: \"{EscSml(state.Window.WindowId)}\"");
			sb.AppendLine($"        screen: {state.Window.Screen}");
			sb.AppendLine($"        positionX: {state.Window.PositionX}");
			sb.AppendLine($"        positionY: {state.Window.PositionY}");
			sb.AppendLine($"        sizeX: {state.Window.SizeX}");
			sb.AppendLine($"        sizeY: {state.Window.SizeY}");
			sb.AppendLine($"        mode: \"{EscSml(state.Window.Mode)}\"");
			sb.AppendLine("    }");
		}

		if (state.Docking is not null)
		{
			sb.AppendLine("    Docking {");
			foreach (var container in state.Docking.Containers)
			{
				sb.AppendLine("        Container {");
				sb.AppendLine($"            key: \"{EscSml(container.Key)}\"");
				sb.AppendLine($"            visible: {(container.Visible ? "true" : "false")}");
				sb.AppendLine($"            dockSide: \"{EscSml(container.DockSide)}\"");
				sb.AppendLine($"            fixedWidth: {container.FixedWidth}");
				if (container.FixedHeight.HasValue)
				{
					sb.AppendLine($"            fixedHeight: {container.FixedHeight.Value}");
				}
				if (container.HeightPercent.HasValue)
				{
					sb.AppendLine(global::System.FormattableString.Invariant($"            heightPercent: {container.HeightPercent.Value:0.###}"));
				}
				sb.AppendLine($"            currentTitle: \"{EscSml(container.CurrentTitle)}\"");
				foreach (var title in container.TabTitles)
				{
					sb.AppendLine($"            Tab {{ title: \"{EscSml(title)}\" }}");
				}
				sb.AppendLine("        }");
			}
			sb.AppendLine("    }");
		}

		sb.AppendLine("}");
		return sb.ToString();
	}

	private static UiSessionState? DeserializeUiSessionStateFromSml(string content)
	{
		SmlDocument doc;
		try
		{
			doc = SmlParseRuntime.ParseDocument(content, context: "UiSessionState");
		}
		catch
		{
			return null;
		}

		SmlNode? root = null;
		foreach (var node in doc.Roots)
		{
			if (string.Equals(node.Name, "UiSessionState", StringComparison.OrdinalIgnoreCase))
			{
				root = node;
				break;
			}
		}

		if (root is null)
		{
			return null;
		}

		var state = new UiSessionState();
		foreach (var child in root.Children)
		{
			if (string.Equals(child.Name, "Window", StringComparison.OrdinalIgnoreCase))
			{
				state.Window = new UiWindowState
				{
					WindowId = GetSessionSmlString(child, "windowId", string.Empty),
					Screen = GetSessionSmlInt(child, "screen", -1),
					PositionX = GetSessionSmlInt(child, "positionX", 0),
					PositionY = GetSessionSmlInt(child, "positionY", 0),
					SizeX = GetSessionSmlInt(child, "sizeX", 0),
					SizeY = GetSessionSmlInt(child, "sizeY", 0),
					Mode = GetSessionSmlString(child, "mode", string.Empty)
				};
				continue;
			}

			if (string.Equals(child.Name, "Docking", StringComparison.OrdinalIgnoreCase))
			{
				var docking = new UiDockingState();
				foreach (var containerNode in child.Children)
				{
					if (!string.Equals(containerNode.Name, "Container", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}

					var container = new UiDockingContainerState
					{
						Key = GetSessionSmlString(containerNode, "key", string.Empty),
						Visible = GetSessionSmlBool(containerNode, "visible", false),
						DockSide = GetSessionSmlString(containerNode, "dockSide", string.Empty),
						FixedWidth = GetSessionSmlInt(containerNode, "fixedWidth", 240),
						CurrentTitle = GetSessionSmlString(containerNode, "currentTitle", string.Empty)
					};
					if (containerNode.TryGetProperty("fixedHeight", out var fixedHeightValue))
					{
						container.FixedHeight = SessionSmlIntValue(fixedHeightValue, 0);
					}
					if (containerNode.TryGetProperty("heightPercent", out var heightPercentValue))
					{
						container.HeightPercent = SessionSmlFloatValue(heightPercentValue, 50f);
					}

					foreach (var tabNode in containerNode.Children)
					{
						if (!string.Equals(tabNode.Name, "Tab", StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}

						var tabTitle = GetSessionSmlString(tabNode, "title", string.Empty);
						if (!string.IsNullOrWhiteSpace(tabTitle))
						{
							container.TabTitles.Add(tabTitle);
						}
					}

					if (string.IsNullOrWhiteSpace(container.Key))
					{
						continue;
					}

					docking.Containers.Add(container);
				}
				state.Docking = docking;
			}
		}

		return state;
	}

	private static string GetSessionSmlString(SmlNode node, string key, string fallback)
	{
		if (!node.TryGetProperty(key, out var value))
		{
			return fallback;
		}

		return value.Kind switch
		{
			SmlValueKind.String => (string)value.Value,
			SmlValueKind.Identifier => (string)value.Value,
			_ => fallback
		};
	}

	private static int GetSessionSmlInt(SmlNode node, string key, int fallback)
	{
		if (!node.TryGetProperty(key, out var value))
		{
			return fallback;
		}

		return value.Kind switch
		{
			SmlValueKind.Int => (int)value.Value,
			SmlValueKind.Float => (int)(double)value.Value,
			_ => fallback
		};
	}

	private static int SessionSmlIntValue(SmlValue value, int fallback)
	{
		return value.Kind switch
		{
			SmlValueKind.Int => (int)value.Value,
			SmlValueKind.Float => (int)(double)value.Value,
			_ => fallback
		};
	}

	private static float SessionSmlFloatValue(SmlValue value, float fallback)
	{
		return value.Kind switch
		{
			SmlValueKind.Float => (float)(double)value.Value,
			SmlValueKind.Int => (int)value.Value,
			_ => fallback
		};
	}

	private static bool GetSessionSmlBool(SmlNode node, string key, bool fallback)
	{
		if (!node.TryGetProperty(key, out var value))
		{
			return fallback;
		}

		return value.Kind switch
		{
			SmlValueKind.Bool => (bool)value.Value,
			SmlValueKind.Identifier => ParseSessionBoolIdentifier(value.Value as string, fallback),
			_ => fallback
		};
	}

	private static bool ParseSessionBoolIdentifier(string? raw, bool fallback)
	{
		if (string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		return fallback;
	}

	private static string EscSml(string value)
	{
		return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
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

		LogUiScalingState(ActiveAppViewport?.GetVisibleRect().Size ?? Vector2.Zero);
	}

	private sealed class UiSessionState
	{
		public UiWindowState? Window { get; set; }
		public UiDockingState? Docking { get; set; }
	}

	private sealed class UiWindowState
	{
		public string WindowId { get; set; } = string.Empty;
		public int Screen { get; set; } = -1;
		public int PositionX { get; set; }
		public int PositionY { get; set; }
		public int SizeX { get; set; }
		public int SizeY { get; set; }
		public string Mode { get; set; } = string.Empty;
	}

	private sealed class UiDockingState
	{
		public List<UiDockingContainerState> Containers { get; set; } = [];
	}

	private sealed class UiDockingContainerState
	{
		public string Key { get; set; } = string.Empty;
		public bool Visible { get; set; }
		public string DockSide { get; set; } = string.Empty;
		public int FixedWidth { get; set; }
		public int? FixedHeight { get; set; }
		public float? HeightPercent { get; set; }
		public List<string> TabTitles { get; set; } = [];
		public string CurrentTitle { get; set; } = string.Empty;
	}

	private void ApplyWindowProperties(Window window, Control rootControl)
	{

		// When transitioning to a Window node, reset the project-default flags that were
		// configured for the SplashScreen (non-resizable, borderless, always-on-top).
		var nodeName = rootControl.HasMeta(NodePropertyMapper.MetaNodeName)
			? rootControl.GetMeta(NodePropertyMapper.MetaNodeName).AsString()
			: string.Empty;

		if (string.Equals(nodeName, "Window", StringComparison.OrdinalIgnoreCase))
		{
			window.SetFlag(Window.Flags.ResizeDisabled, false);
			window.SetFlag(Window.Flags.Borderless, false);
			window.SetFlag(Window.Flags.AlwaysOnTop, false);
			window.MinimizeDisabled = false;
			window.MaximizeDisabled = false;
			RunnerLogger.Debug("UI", "Window flags reset to normal (resizable, all buttons, not borderless, not always-on-top)");
		}

		if (rootControl.HasMeta(NodePropertyMapper.MetaWindowTitle))
		{
			window.Title = rootControl.GetMeta(NodePropertyMapper.MetaWindowTitle).AsString();
			RunnerLogger.Debug("UI", $"Window title applied: '{window.Title}'");
		}

		if (rootControl.HasMeta(NodePropertyMapper.MetaWindowSizeX)
			&& rootControl.HasMeta(NodePropertyMapper.MetaWindowSizeY))
		{
			var width = Math.Max(1, rootControl.GetMeta(NodePropertyMapper.MetaWindowSizeX).AsInt32());
			var height = Math.Max(1, rootControl.GetMeta(NodePropertyMapper.MetaWindowSizeY).AsInt32());
			window.Size = new Vector2I(width, height);
			RunnerLogger.Debug("UI", $"Window size applied: {window.Size.X}x{window.Size.Y}");
		}

		if (rootControl.HasMeta(NodePropertyMapper.MetaWindowExtendToTitle))
		{
			var enabled = rootControl.GetMeta(NodePropertyMapper.MetaWindowExtendToTitle).AsBool();
			window.SetFlag(Window.Flags.ExtendToTitle, enabled);
			RunnerLogger.Debug("UI", $"Window extendToTitle applied: {enabled}");
		}

		if (rootControl.HasMeta(NodePropertyMapper.MetaWindowPosX)
			&& rootControl.HasMeta(NodePropertyMapper.MetaWindowPosY))
		{
			var x = rootControl.GetMeta(NodePropertyMapper.MetaWindowPosX).AsInt32();
			var y = rootControl.GetMeta(NodePropertyMapper.MetaWindowPosY).AsInt32();
			window.Position = new Vector2I(x, y);
			RunnerLogger.Debug("UI", $"Window position applied: {window.Position.X},{window.Position.Y}");
		}

		if (!rootControl.HasMeta(NodePropertyMapper.MetaWindowMinSizeX)
			|| !rootControl.HasMeta(NodePropertyMapper.MetaWindowMinSizeY))
		{
			return;
		}

		var minWidth = rootControl.GetMeta(NodePropertyMapper.MetaWindowMinSizeX).AsInt32();
		var minHeight = rootControl.GetMeta(NodePropertyMapper.MetaWindowMinSizeY).AsInt32();
		window.MinSize = new Vector2I(Math.Max(0, minWidth), Math.Max(0, minHeight));
		RunnerLogger.Debug("UI", $"Window minSize applied: {window.MinSize.X}x{window.MinSize.Y}");
	}

	private void ResizeUiRootToViewport()
	{
		if (_runtimeUiHost is null || _runtimeUiRoot is null)
		{
			return;
		}

		var viewport = ActiveAppViewport;
		if (viewport is null)
		{
			return;
		}

		var size = viewport.GetVisibleRect().Size;
		_runtimeUiHost.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_runtimeUiHost.SetOffsetsPreset(Control.LayoutPreset.FullRect);
		_runtimeUiHost.Position = Vector2.Zero;
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
		var window = ActiveAppWindow;
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

		// Layout mode runs continuously; avoid noisy per-resize diagnostics in normal operation.
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
