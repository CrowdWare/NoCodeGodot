using Godot;
using Runtime.Assets;
using Runtime.Logging;
using Runtime.Sms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Runtime.UI;

public sealed class SmsUiRuntime
{
    private const string MetaSmsPath = "sms_path";

    private readonly RunnerUriResolver _uriResolver;
    private readonly string _uiSmlUri;
    private readonly ScriptEngine _engine = new();
    private UiActionDispatcher? _dispatcher;
    private readonly Dictionary<int, TreeItem> _treeHandles = [];
    private int _nextTreeHandle = 1;
    private string? _projectRoot;

    public SmsUiRuntime(RunnerUriResolver uriResolver, string uiSmlUri)
    {
        _uriResolver = uriResolver;
        _uiSmlUri = uiSmlUri;
    }

    public bool IsLoaded { get; private set; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var smsUri = TryBuildCompanionSmsUri(_uiSmlUri);
        if (string.IsNullOrWhiteSpace(smsUri))
        {
            return;
        }

        string source;
        try
        {
            source = await _uriResolver.LoadTextAsync(smsUri, cancellationToken: cancellationToken);
        }
        catch (FileNotFoundException)
        {
            RunnerLogger.Info("SMS", $"No companion SMS script found for '{_uiSmlUri}' (expected '{smsUri}').");
            return;
        }
        catch (Exception ex)
        {
            RunnerLogger.Warn("SMS", $"Failed loading companion SMS script '{smsUri}'.", ex);
            return;
        }

        _projectRoot = ResolveProjectRoot(_uiSmlUri);
        RegisterNativeFunctions();

        try
        {
            _engine.Execute(source);
            IsLoaded = true;
            RunnerLogger.Info("SMS", $"Loaded companion SMS script: '{smsUri}'.");
        }
        catch (Exception ex)
        {
            RunnerLogger.Error("SMS", $"Failed executing companion SMS script '{smsUri}'.", ex);
        }
    }

    public void BindDispatcher(UiActionDispatcher dispatcher)
    {
        if (!IsLoaded)
        {
            return;
        }

        _dispatcher = dispatcher;

        dispatcher.RegisterActionHandler("treeItemSelected", ctx =>
        {
            var treeId = ctx.SourceId;
            var treeText = ctx.TreeItem?.Text ?? string.Empty;
            var selectedPath = TryGetSelectedTreePath(ctx.Source as TreeView) ?? string.Empty;
            ExecuteCall($"treeItemSelected({Quote(treeId)}, {Quote(treeText)}, {Quote(selectedPath)})");
        });

        dispatcher.RegisterActionHandler("treeItemToggled", ctx =>
        {
            var treeId = ctx.SourceId;
            var treeText = ctx.TreeItem?.Text ?? string.Empty;
            var selectedPath = TryGetSelectedTreePath(ctx.Source as TreeView) ?? string.Empty;
            var isOn = ctx.BoolValue == true ? "true" : "false";
            ExecuteCall($"treeItemToggled({Quote(treeId)}, {Quote(treeText)}, {Quote(selectedPath)}, {isOn})");
        });

        dispatcher.RegisterActionHandlerIfMissing("save", _ =>
        {
            ExecuteCall("CodeEditOnSave(\"codeEdit\")");
        });
    }

    public void InvokeReady()
    {
        if (!IsLoaded)
        {
            return;
        }

        ExecuteCall("ready()");
    }

    private void RegisterNativeFunctions()
    {
        _engine.RegisterFunction("Info", args =>
        {
            var subsystem = ArgString(args, 0);
            if (string.IsNullOrWhiteSpace(subsystem))
            {
                subsystem = "SMS";
            }

            var message = ArgString(args, 1);
            RunnerLogger.Info(subsystem, message);
            return null;
        });

        _engine.RegisterFunction("UiExists", args =>
        {
            var id = ArgString(args, 0);
            return UiRuntimeApi.GetObjectById(id) is not null;
        });

        _engine.RegisterFunction("BindTreeEvents", args =>
        {
            var id = ArgString(args, 0);
            var tree = UiRuntimeApi.GetObjectById(id) as TreeView;
            var dispatcher = _dispatcher ?? ResolveDispatcherFromMain();
            if (tree is not null && dispatcher is not null)
            {
                UiRuntimeApi.BindTreeViewEvents(tree, dispatcher);
                RunnerLogger.Info("SMS", $"BindTreeEvents wired for '{id}'.");
            }
            else
            {
                RunnerLogger.Warn("SMS", $"BindTreeEvents skipped for '{id}' (treeFound={tree is not null}, dispatcherFound={dispatcher is not null}).");
            }

            return null;
        });

        _engine.RegisterFunction("TreeClear", args =>
        {
            var tree = ResolveTree(args);
            tree?.Clear();
            if (tree is not null)
            {
                tree.Columns = 1;
            }

            return null;
        });

        _engine.RegisterFunction("TreeCreateRoot", args =>
        {
            var tree = ResolveTree(args);
            if (tree is null)
            {
                return 0;
            }

            var text = ArgString(args, 1);
            var path = ArgString(args, 2);
            var item = tree.CreateItem();
            item.SetText(0, text);
            item.Collapsed = false;
            item.SetMetadata(0, path);
            return RegisterTreeHandle(item);
        });

        _engine.RegisterFunction("TreeCreateChild", args =>
        {
            var tree = ResolveTree(args);
            if (tree is null)
            {
                return 0;
            }

            var parentHandle = ArgInt(args, 1);
            if (!_treeHandles.TryGetValue(parentHandle, out var parent))
            {
                return 0;
            }

            var text = ArgString(args, 2);
            var path = ArgString(args, 3);
            var isDirectory = ArgBool(args, 4);

            var item = tree.CreateItem(parent);
            item.SetText(0, isDirectory ? text + "/" : text);
            item.SetMetadata(0, path);
            item.Collapsed = true;
            return RegisterTreeHandle(item);
        });

        _engine.RegisterFunction("CodeEditSetPath", args =>
        {
            var editor = ResolveCodeEdit(args);
            var path = ArgString(args, 1);
            if (editor is not null && !string.IsNullOrWhiteSpace(path))
            {
                editor.SetMeta(MetaSmsPath, path);
                CodeEditSyntaxRuntime.Load(editor, path);
            }

            return null;
        });

        _engine.RegisterFunction("CodeEditSetText", args =>
        {
            var editor = ResolveCodeEdit(args);
            var text = ArgString(args, 1);
            if (editor is not null)
            {
                editor.Text = text;

                // Re-apply syntax after content update to avoid first-render glitches
                // (e.g. partially unstyled/black text until manual edit).
                if (editor.HasMeta(MetaSmsPath))
                {
                    var path = editor.GetMeta(MetaSmsPath).AsString();
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        CodeEditSyntaxRuntime.Load(editor, path);
                    }
                }

                editor.QueueRedraw();
            }

            return null;
        });

        _engine.RegisterFunction("CodeEditGetText", args =>
        {
            var editor = ResolveCodeEdit(args);
            return editor?.Text ?? string.Empty;
        });

        _engine.RegisterFunction("CodeEditGetPath", args =>
        {
            var editor = ResolveCodeEdit(args);
            if (editor is null)
            {
                return string.Empty;
            }

            return editor.HasMeta(MetaSmsPath) ? editor.GetMeta(MetaSmsPath).AsString() : string.Empty;
        });

        _engine.RegisterFunction("ProjectFS_List", NativeList);

        _engine.RegisterFunction("ProjectFS_Exists", args =>
        {
            var path = ArgString(args, 0);
            return ResolveProjectFs().Exists(path);
        });

        _engine.RegisterFunction("ProjectFS_ReadText", args =>
        {
            var path = ArgString(args, 0);
            return ResolveProjectFs().ReadText(path);
        });

        _engine.RegisterFunction("ProjectFS_WriteText", args =>
        {
            var path = ArgString(args, 0);
            var text = ArgString(args, 1);
            ResolveProjectFs().WriteText(path, text);
            return null;
        });
    }

    private void ExecuteCall(string source)
    {
        try
        {
            _engine.Execute(source);
        }
        catch (Exception ex)
        {
            RunnerLogger.Warn("SMS", $"SMS call failed: {source}", ex);
        }
    }

    private ProjectFs ResolveProjectFs()
    {
        var root = _projectRoot;
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Directory.GetCurrentDirectory();
        }

        return new ProjectFs(root);
    }

    private UiActionDispatcher? ResolveDispatcherFromMain()
    {
        if (Engine.GetMainLoop() is not SceneTree sceneTree || sceneTree.Root is null)
        {
            return null;
        }

        var stack = new Stack<Node>();
        stack.Push(sceneTree.Root);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (node is Main main && main.TryGetDispatcher(out var dispatcher))
            {
                return dispatcher;
            }

            for (var i = node.GetChildCount() - 1; i >= 0; i--)
            {
                stack.Push(node.GetChild(i));
            }
        }

        return null;
    }

    private Value NativeList(IReadOnlyList<Value> args)
    {
        var dir = args.Count > 0 ? ValueAsString(args[0]) : ".";
        var entries = ResolveProjectFs().List(dir);
        var items = entries.Select(e => (Value)new ObjectValue("ProjectFsEntry", new Dictionary<string, Value>
        {
            ["Path"] = new StringValue(e.Path),
            ["Name"] = new StringValue(e.Name),
            ["IsDirectory"] = new BooleanValue(e.IsDirectory)
        })).ToList();
        return new ArrayValue(items);
    }

    private static string TryGetSelectedTreePath(TreeView? tree)
    {
        if (tree is null)
        {
            return string.Empty;
        }

        var selected = tree.GetSelected();
        if (selected is null)
        {
            return string.Empty;
        }

        return selected.GetMetadata(0).AsString();
    }

    private static string ResolveProjectRoot(string uiSmlUri)
    {
        if (Uri.TryCreate(uiSmlUri, UriKind.Absolute, out var uri) && uri.IsFile)
        {
            var fileDir = Path.GetDirectoryName(uri.LocalPath);
            if (!string.IsNullOrWhiteSpace(fileDir))
            {
                var parent = Directory.GetParent(fileDir)?.FullName;
                return string.IsNullOrWhiteSpace(parent) ? fileDir : parent;
            }
        }

        return Directory.GetCurrentDirectory();
    }

    private static string? TryBuildCompanionSmsUri(string uiSmlUri)
    {
        if (string.IsNullOrWhiteSpace(uiSmlUri))
        {
            return null;
        }

        if (Uri.TryCreate(uiSmlUri, UriKind.Absolute, out var absoluteUri))
        {
            if (!absoluteUri.AbsolutePath.EndsWith(".sml", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var builder = new UriBuilder(absoluteUri)
            {
                Path = absoluteUri.AbsolutePath[..^4] + ".sms"
            };

            return builder.Uri.ToString();
        }

        return uiSmlUri.EndsWith(".sml", StringComparison.OrdinalIgnoreCase)
            ? uiSmlUri[..^4] + ".sms"
            : null;
    }

    private TreeView? ResolveTree(IReadOnlyList<object?> args)
    {
        var id = ArgString(args, 0);
        return UiRuntimeApi.GetObjectById(id) as TreeView;
    }

    private CodeEdit? ResolveCodeEdit(IReadOnlyList<object?> args)
    {
        var id = ArgString(args, 0);
        return UiRuntimeApi.GetObjectById(id) as CodeEdit;
    }

    private int RegisterTreeHandle(TreeItem item)
    {
        var handle = _nextTreeHandle++;
        _treeHandles[handle] = item;
        return handle;
    }

    private static string ArgString(IReadOnlyList<object?> args, int index)
    {
        if (index >= args.Count)
        {
            return string.Empty;
        }

        var value = args[index];
        return value switch
        {
            null => string.Empty,
            string s => s,
            Runtime.Sms.StringValue s => s.Value,
            Runtime.Sms.NumberValue n => n.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Runtime.Sms.BooleanValue b => b.Value ? "true" : "false",
            Runtime.Sms.NullValue => string.Empty,
            _ => value.ToString() ?? string.Empty
        };
    }

    private static int ArgInt(IReadOnlyList<object?> args, int index)
    {
        var raw = ArgString(args, index);
        return int.TryParse(raw, out var value) ? value : 0;
    }

    private static bool ArgBool(IReadOnlyList<object?> args, int index)
    {
        var raw = ArgString(args, index);
        return string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string Quote(string value)
        => "\"" + (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    private static string ValueAsString(Value value)
    {
        return value switch
        {
            StringValue s => s.Value,
            NumberValue n => n.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            BooleanValue b => b.Value ? "true" : "false",
            NullValue => string.Empty,
            _ => value.ToString()
        };
    }
}
