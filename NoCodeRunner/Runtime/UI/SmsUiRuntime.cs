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
    private const string MetaSmsSaveShortcutHooked = "sms_saveShortcutHooked";

    private readonly RunnerUriResolver _uriResolver;
    private readonly string _uiSmlUri;
    private readonly ScriptEngine _engine = new();
    private UiActionDispatcher? _dispatcher;
    private readonly Dictionary<int, TreeItem> _treeHandles = [];
    private readonly Dictionary<string, string> _codeEditSaveCallbacks = new(StringComparer.OrdinalIgnoreCase);
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
        ExecuteBootstrapGlobals();

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

        dispatcher.RegisterActionHandler("menuItemSelected", ctx =>
        {
            var menuId = ctx.SourceId;
            var itemId = ctx.Clicked;
            ExecuteCall($"menuItemSelected({Quote(menuId)}, {Quote(itemId)})");
        });

        // SMS should own save behavior so codeEdit.onSave(...) callbacks always fire,
        // even if a generic fallback handler was registered earlier.
        dispatcher.RegisterActionHandler("save", ctx =>
        {
            var editorId = string.IsNullOrWhiteSpace(ctx.SourceId) ? "codeEdit" : ctx.SourceId;
            if (_codeEditSaveCallbacks.TryGetValue(editorId, out var callbackName)
                && !string.IsNullOrWhiteSpace(callbackName))
            {
                ExecuteCall($"{callbackName}({Quote(editorId)})");
            }
            else
            {
                RunnerLogger.Warn("SMS", $"Save ignored for '{editorId}': no onSave callback registered.");
            }
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
        _engine.RegisterFunction("__sms_fs", _ => CreateFsObject());
        _engine.RegisterFunction("__sms_log", _ => CreateLogObject());

        _engine.RegisterFunction("UiExists", args =>
        {
            var id = ArgString(args, 0);
            return UiRuntimeApi.GetObjectById(id) is not null;
        });

        _engine.RegisterFunction("getObject", args =>
        {
            var id = ValueArgString(args, 0);
            if (string.IsNullOrWhiteSpace(id))
            {
                return NullValue.Instance;
            }

            var node = UiRuntimeApi.GetObjectById(id);
            if (node is TreeView tree)
            {
                return CreateTreeObject(id, tree);
            }

            if (node is CodeEdit editor)
            {
                return CreateCodeEditObject(id, editor);
            }

            return NullValue.Instance;
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

    }

    private void ExecuteBootstrapGlobals()
    {
        try
        {
            _engine.Execute("var fs = __sms_fs()\nvar log = __sms_log()");
        }
        catch (Exception ex)
        {
            RunnerLogger.Error("SMS", "Failed to bootstrap global SMS objects (fs/log).", ex);
        }
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

    private ObjectValue CreateFsObject()
    {
        return new ObjectValue("ProjectFs", new Dictionary<string, Value>(StringComparer.Ordinal)
        {
            ["readText"] = new NativeFunctionValue(methodArgs =>
            {
                var path = ValueArgString(methodArgs, 0);
                return new StringValue(ResolveProjectFs().ReadText(path));
            }),
            ["writeText"] = new NativeFunctionValue(methodArgs =>
            {
                var path = ValueArgString(methodArgs, 0);
                var content = ValueArgString(methodArgs, 1);
                ResolveProjectFs().WriteText(path, content);
                return NullValue.Instance;
            }),
            ["exists"] = new NativeFunctionValue(methodArgs =>
            {
                var path = ValueArgString(methodArgs, 0);
                return new BooleanValue(ResolveProjectFs().Exists(path));
            }),
            ["list"] = new NativeFunctionValue(methodArgs =>
            {
                var dir = methodArgs.Count > 0 ? ValueArgString(methodArgs, 0) : ".";
                var entries = ResolveProjectFs().List(dir);
                var items = entries.Select(e => (Value)new ObjectValue("ProjectFsEntry", new Dictionary<string, Value>
                {
                    ["Path"] = new StringValue(e.Path),
                    ["Name"] = new StringValue(e.Name),
                    ["IsDirectory"] = new BooleanValue(e.IsDirectory)
                })).ToList();
                return new ArrayValue(items);
            })
        });
    }

    private static ObjectValue CreateLogObject()
    {
        return new ObjectValue("Log", new Dictionary<string, Value>(StringComparer.Ordinal)
        {
            ["info"] = new NativeFunctionValue(methodArgs =>
            {
                RunnerLogger.Info("SMS", ValueArgString(methodArgs, 0));
                return NullValue.Instance;
            }),
            ["warn"] = new NativeFunctionValue(methodArgs =>
            {
                RunnerLogger.Warn("SMS", ValueArgString(methodArgs, 0));
                return NullValue.Instance;
            }),
            ["error"] = new NativeFunctionValue(methodArgs =>
            {
                RunnerLogger.Error("SMS", ValueArgString(methodArgs, 0));
                return NullValue.Instance;
            }),
            ["success"] = new NativeFunctionValue(methodArgs =>
            {
                RunnerLogger.Success("SMS", ValueArgString(methodArgs, 0));
                return NullValue.Instance;
            })
        });
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

    private ObjectValue CreateTreeObject(string id, TreeView tree)
    {
        return new ObjectValue("TreeView", new Dictionary<string, Value>(StringComparer.Ordinal)
        {
            ["Clear"] = new NativeFunctionValue(_ =>
            {
                tree.Clear();
                tree.Columns = 1;
                _treeHandles.Clear();
                return NullValue.Instance;
            }),
            ["CreateRoot"] = new NativeFunctionValue(methodArgs =>
            {
                var text = ValueArgString(methodArgs, 0);
                var path = ValueArgString(methodArgs, 1);
                var item = tree.CreateItem();
                item.SetText(0, text);
                item.Collapsed = false;
                item.SetMetadata(0, path);
                return new NumberValue(RegisterTreeHandle(item));
            }),
            ["CreateChild"] = new NativeFunctionValue(methodArgs =>
            {
                var parentHandle = ValueArgInt(methodArgs, 0);
                if (!_treeHandles.TryGetValue(parentHandle, out var parent))
                {
                    return new NumberValue(0);
                }

                var text = ValueArgString(methodArgs, 1);
                var path = ValueArgString(methodArgs, 2);
                var isDirectory = ValueArgBool(methodArgs, 3);

                var item = tree.CreateItem(parent);
                item.SetText(0, isDirectory ? text + "/" : text);
                item.SetMetadata(0, path);
                item.Collapsed = true;
                return new NumberValue(RegisterTreeHandle(item));
            }),
            ["BindEvents"] = new NativeFunctionValue(_ =>
            {
                var dispatcher = _dispatcher ?? ResolveDispatcherFromMain();
                if (dispatcher is not null)
                {
                    UiRuntimeApi.BindTreeViewEvents(tree, dispatcher);
                    RunnerLogger.Info("SMS", $"TreeView events bound for '{id}'.");
                }
                else
                {
                    RunnerLogger.Warn("SMS", $"BindEvents skipped for '{id}' (dispatcher not found).");
                }

                return NullValue.Instance;
            })
        });
    }

    private ObjectValue CreateCodeEditObject(string id, CodeEdit editor)
    {
        EnsureCodeEditSaveShortcut(id, editor);

        return new ObjectValue("CodeEdit", new Dictionary<string, Value>(StringComparer.Ordinal)
        {
            ["SetPath"] = new NativeFunctionValue(methodArgs =>
            {
                var path = ValueArgString(methodArgs, 0);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    editor.SetMeta(MetaSmsPath, path);
                    CodeEditSyntaxRuntime.Load(editor, path);
                }

                return NullValue.Instance;
            }),
            ["SetText"] = new NativeFunctionValue(methodArgs =>
            {
                var text = ValueArgString(methodArgs, 0);
                editor.Text = text;

                if (editor.HasMeta(MetaSmsPath))
                {
                    var path = editor.GetMeta(MetaSmsPath).AsString();
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        CodeEditSyntaxRuntime.Load(editor, path);
                    }
                }

                editor.QueueRedraw();
                return NullValue.Instance;
            }),
            ["GetText"] = new NativeFunctionValue(_ => new StringValue(editor.Text ?? string.Empty)),
            ["GetPath"] = new NativeFunctionValue(_ =>
            {
                var path = editor.HasMeta(MetaSmsPath) ? editor.GetMeta(MetaSmsPath).AsString() : string.Empty;
                return new StringValue(path ?? string.Empty);
            }),
            ["onSave"] = new NativeFunctionValue(methodArgs =>
            {
                var callbackName = ValueArgString(methodArgs, 0);
                if (!string.IsNullOrWhiteSpace(callbackName))
                {
                    _codeEditSaveCallbacks[id] = callbackName;
                    RunnerLogger.Info("SMS", $"Registered onSave callback '{callbackName}' for '{id}'.");
                }

                return NullValue.Instance;
            })
        });
    }

    private void EnsureCodeEditSaveShortcut(string id, CodeEdit editor)
    {
        if (editor.HasMeta(MetaSmsSaveShortcutHooked) && editor.GetMeta(MetaSmsSaveShortcutHooked).AsBool())
        {
            return;
        }

        editor.GuiInput += @event =>
        {
            if (@event is not InputEventKey keyEvent)
            {
                return;
            }

            if (!keyEvent.Pressed || keyEvent.Echo)
            {
                return;
            }

            if (keyEvent.Keycode != Key.S)
            {
                return;
            }

            var isSaveShortcut = keyEvent.CtrlPressed || keyEvent.MetaPressed;
            if (!isSaveShortcut)
            {
                return;
            }

            var sourceId = string.IsNullOrWhiteSpace(id)
                ? (editor.HasMeta(NodePropertyMapper.MetaId) ? editor.GetMeta(NodePropertyMapper.MetaId).AsString() : "codeEdit")
                : id;
            var sourceIdValue = editor.HasMeta(NodePropertyMapper.MetaIdValue)
                ? new Id(editor.GetMeta(NodePropertyMapper.MetaIdValue).AsInt32())
                : new Id(0);

            _dispatcher?.Dispatch(new UiActionContext(
                Source: editor,
                SourceId: sourceId,
                SourceIdValue: sourceIdValue,
                Action: "save",
                Clicked: string.Empty,
                ClickedIdValue: new Id(0)
            ));

            editor.AcceptEvent();
        };

        editor.SetMeta(MetaSmsSaveShortcutHooked, Variant.From(true));
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

    private static string ValueArgString(IReadOnlyList<Value> args, int index)
    {
        if (index >= args.Count)
        {
            return string.Empty;
        }

        return args[index] switch
        {
            StringValue s => s.Value,
            NumberValue n => n.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            BooleanValue b => b.Value ? "true" : "false",
            NullValue => string.Empty,
            _ => args[index].ToString()
        };
    }

    private static int ValueArgInt(IReadOnlyList<Value> args, int index)
    {
        if (index >= args.Count)
        {
            return 0;
        }

        return args[index] switch
        {
            NumberValue n => n.ToInt(),
            _ => int.TryParse(ValueArgString(args, index), out var parsed) ? parsed : 0
        };
    }

    private static bool ValueArgBool(IReadOnlyList<Value> args, int index)
    {
        if (index >= args.Count)
        {
            return false;
        }

        return args[index] switch
        {
            BooleanValue b => b.Value,
            _ => string.Equals(ValueArgString(args, index), "true", StringComparison.OrdinalIgnoreCase)
        };
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
