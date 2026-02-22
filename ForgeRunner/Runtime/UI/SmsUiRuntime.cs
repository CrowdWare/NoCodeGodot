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
using Runtime.Assets;
using Runtime.Logging;
using Runtime.Sms;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
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
    private readonly Dictionary<ulong, Dictionary<long, string>> _dynamicMenuItemsByPopup = new();
    private readonly Dictionary<ulong, Control> _dynamicMenuSourcesByPopup = new();
    private readonly HashSet<ulong> _dynamicMenuPopupHooked = [];
    private readonly Dictionary<ulong, string> _windowCloseCallbacks = new();
    private readonly HashSet<ulong> _windowCloseHooked = [];
    private readonly Dictionary<Type, Dictionary<string, List<MethodInfo>>> _methodCache = [];
    private readonly Dictionary<long, object> _nativeObjects = [];
    private long _nextNativeObjectId = 1;
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
            var treeId = ResolveSourceId(ctx);
            if (string.IsNullOrWhiteSpace(treeId))
            {
                RunnerLogger.Warn("SMS", "treeItemSelected ignored: source tree has no id.");
                return;
            }

            TryInvokeEvent(treeId, "itemSelected");
        });

        dispatcher.RegisterActionHandler("treeItemToggled", ctx =>
        {
            var treeId = ResolveSourceId(ctx);
            if (string.IsNullOrWhiteSpace(treeId))
            {
                RunnerLogger.Warn("SMS", "treeItemToggled ignored: source tree has no id.");
                return;
            }

            TryInvokeEvent(treeId, "treeItemToggled", ctx.BoolValue == true);
        });

        dispatcher.RegisterActionHandler("menuItemSelected", ctx =>
        {
            var itemId = ctx.Clicked;

            if (string.IsNullOrWhiteSpace(itemId))
            {
                RunnerLogger.Warn("SMS", "menuItemSelected ignored: clicked item id is empty.");
                return;
            }

            // Event-first only: on <menuItemId>.clicked()
            TryInvokeEvent(itemId, "clicked", warnIfMissing: !ShouldSuppressMissingMenuHandler(itemId));
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
        AutoBindTreeEvents();
    }

    private void AutoBindTreeEvents()
    {
        var dispatcher = _dispatcher ?? ResolveDispatcherFromMain();
        if (dispatcher is null)
        {
            RunnerLogger.Warn("SMS", "Auto tree binding skipped: dispatcher not found.");
            return;
        }

        var boundCount = 0;
        foreach (var tree in EnumerateTrees())
        {
            UiRuntimeApi.BindTreeEvents(tree, dispatcher);
            boundCount++;
        }

        if (boundCount > 0)
        {
            RunnerLogger.Info("SMS", $"Auto-bound tree events for {boundCount} tree control(s) after ready().");
        }
    }

    private static IEnumerable<Tree> EnumerateTrees()
    {
        if (Engine.GetMainLoop() is not SceneTree sceneTree || sceneTree.Root is null)
        {
            yield break;
        }

        var stack = new Stack<Node>();
        stack.Push(sceneTree.Root);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (node is Tree tree)
            {
                yield return tree;
            }

            for (var i = node.GetChildCount() - 1; i >= 0; i--)
            {
                stack.Push(node.GetChild(i));
            }
        }
    }

    private void RegisterNativeFunctions()
    {
        _engine.RegisterFunction("__sms_fs", _ => CreateFsObject());
        _engine.RegisterFunction("__sms_log", _ => CreateLogObject());
        _engine.RegisterFunction("__sms_ui", _ => CreateUiObject());
        _engine.RegisterFunction("__sms_get_menu_item", args =>
        {
            var itemId = ArgString(args, 0);
            var item = TryCreateMenuItemObject(itemId);
            return item is null ? NullValue.Instance : (Value)item;
        });

        _engine.RegisterFunction("UiExists", args =>
        {
            var id = ArgString(args, 0);
            return UiRuntimeApi.GetObjectById(id) is not null;
        });

        _engine.RegisterFunction("BindTreeEvents", args =>
        {
            var id = ArgString(args, 0);
            var tree = UiRuntimeApi.GetObjectById(id) as Tree;
            var dispatcher = _dispatcher ?? ResolveDispatcherFromMain();
            if (tree is not null && dispatcher is not null)
            {
                UiRuntimeApi.BindTreeEvents(tree, dispatcher);
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
            _engine.Execute("var fs = __sms_fs()\nvar log = __sms_log()\nvar ui = __sms_ui()");
            BootstrapWindowFlagSymbols();
            BootstrapUiIdSymbols();
        }
        catch (Exception ex)
        {
            RunnerLogger.Error("SMS", "Failed to bootstrap global SMS objects (fs/log/ui).", ex);
        }
    }

    private void BootstrapWindowFlagSymbols()
    {
        // Stable, documented aliases for SMS scripts.
        // Keep explicit values so users can rely on constants across platforms.
        var symbols = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["borderless"] = 0,
            ["alwaysOnTop"] = 1,
            ["transparent"] = 2,
            ["noFocus"] = 3,
            ["popup"] = 5,
            ["extendToTitle"] = 6,
            ["mousePassthrough"] = 7,
            ["sharpCorners"] = 8,
            ["excludeFromCapture"] = 9,
            ["popupWmHint"] = 10,
            ["minSize"] = 11,
            ["maxSize"] = 12,
            ["resizeDisabled"] = 13,
            ["transient"] = 14,
            ["modal"] = 15,
            ["popupExclusive"] = 16
        };

        var declared = 0;
        foreach (var entry in symbols)
        {
            var symbol = entry.Key;
            var value = entry.Value;
            if (!IsValidSmsIdentifier(symbol)
                || IsSmsKeyword(symbol)
                || string.Equals(symbol, "fs", StringComparison.Ordinal)
                || string.Equals(symbol, "log", StringComparison.Ordinal)
                || string.Equals(symbol, "ui", StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                _engine.Execute($"var {symbol} = {value}");
                declared++;
            }
            catch (Exception ex)
            {
                RunnerLogger.Warn("SMS", $"Could not expose window flag symbol '{symbol}' to SMS globals.", ex);
            }
        }

        if (declared > 0)
        {
            RunnerLogger.Info("SMS", $"Exposed {declared} window flag symbols as SMS globals.");
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

    private ObjectValue CreateUiObject()
    {
        return new ObjectValue("Ui", new Dictionary<string, Value>(StringComparer.Ordinal)
        {
            ["getObject"] = new NativeFunctionValue(methodArgs =>
            {
                var id = ValueArgString(methodArgs, 0).Trim();
                if (string.IsNullOrWhiteSpace(id))
                {
                    return NullValue.Instance;
                }

                var node = UiRuntimeApi.GetObjectById(id);
                if (node is not null)
                {
                    if (node is Control control
                        && control.HasMeta(NodePropertyMapper.MetaNodeName)
                        && string.Equals(control.GetMeta(NodePropertyMapper.MetaNodeName).AsString(), "Window", StringComparison.OrdinalIgnoreCase))
                    {
                        var hostWindow = control.GetWindow();
                        if (hostWindow is not null)
                        {
                            return CreateRuntimeObject(id, hostWindow);
                        }
                    }

                    return CreateRuntimeObject(id, node);
                }

                var menuItem = TryCreateMenuItemObject(id);
                if (menuItem is not null)
                {
                    return menuItem;
                }

                return NullValue.Instance;
            }),
            ["CreateWindow"] = new NativeFunctionValue(methodArgs =>
            {
                var sml = ValueArgString(methodArgs, 0);
                var window = UiRuntimeApi.CreateWindowFromSml(sml);
                if (window is null)
                {
                    return NullValue.Instance;
                }

                return CreateRuntimeObject(
                    window.HasMeta(NodePropertyMapper.MetaId)
                        ? window.GetMeta(NodePropertyMapper.MetaId).AsString()
                        : string.Empty,
                    window);
            })
        });
    }

    private ObjectValue CreateRuntimeObject(string id, object runtimeObject)
    {
        var type = runtimeObject.GetType();
        var nativeObjectId = _nextNativeObjectId++;
        _nativeObjects[nativeObjectId] = runtimeObject;

        var fields = new Dictionary<string, Value>(StringComparer.Ordinal)
        {
            ["id"] = new StringValue(id),
            ["type"] = new StringValue(type.Name),
            ["__nativeObjectId"] = new NumberValue(nativeObjectId)
        };

        var methodMap = GetMethodMap(type);
        foreach (var entry in methodMap)
        {
            var methodName = entry.Key;
            var methods = entry.Value;

            fields[methodName] = new NativeFunctionValue(methodArgs =>
            {
                if (runtimeObject is GodotObject godot && !GodotObject.IsInstanceValid(godot))
                {
                    return NullValue.Instance;
                }

                if (!TrySelectMethod(methods, methodArgs, out var selected, out var convertedArgs))
                {
                    RunnerLogger.Warn("SMS", $"No suitable method overload found for '{type.Name}.{methodName}' with {methodArgs.Count} argument(s). Returning null.");
                    return NullValue.Instance;
                }

                try
                {
                    var result = selected.Invoke(runtimeObject, convertedArgs);
                    return ToSmsValue(result);
                }
                catch (Exception ex)
                {
                    RunnerLogger.Warn("SMS", $"Invocation failed for '{type.Name}.{selected.Name}'.", ex);
                    return NullValue.Instance;
                }
            });
        }

        AttachSmsExtensions(id, runtimeObject, fields);

        return new ObjectValue(type.Name, fields);
    }

    private void AttachSmsExtensions(string id, object runtimeObject, Dictionary<string, Value> fields)
    {
        if (runtimeObject is CodeEdit editor)
        {
            EnsureCodeEditSaveShortcut(id, editor);
            fields["SetPath"] = new NativeFunctionValue(methodArgs =>
            {
                var path = ValueArgString(methodArgs, 0);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    editor.SetMeta(MetaSmsPath, path);
                    CodeEditSyntaxRuntime.Load(editor, path);
                }

                return NullValue.Instance;
            });
            fields["GetPath"] = new NativeFunctionValue(_ =>
            {
                var path = editor.HasMeta(MetaSmsPath) ? editor.GetMeta(MetaSmsPath).AsString() : string.Empty;
                return new StringValue(path ?? string.Empty);
            });
            fields["onSave"] = new NativeFunctionValue(methodArgs =>
            {
                var callbackName = ValueArgString(methodArgs, 0);
                if (!string.IsNullOrWhiteSpace(callbackName))
                {
                    _codeEditSaveCallbacks[id] = callbackName;
                    RunnerLogger.Info("SMS", $"Registered onSave callback '{callbackName}' for '{id}'.");
                }

                return NullValue.Instance;
            });
        }

        if (runtimeObject is Tree tree)
        {
            fields["CreateRoot"] = new NativeFunctionValue(methodArgs =>
            {
                var text = ValueArgString(methodArgs, 0);
                var path = ValueArgString(methodArgs, 1);
                var item = tree.CreateItem();
                item.SetText(0, text);
                item.Collapsed = false;
                item.SetMetadata(0, path);
                return new NumberValue(RegisterTreeHandle(item));
            });
            fields["CreateChild"] = new NativeFunctionValue(methodArgs =>
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
            });
            fields["BindEvents"] = new NativeFunctionValue(_ =>
            {
                var dispatcher = _dispatcher ?? ResolveDispatcherFromMain();
                if (dispatcher is not null)
                {
                    UiRuntimeApi.BindTreeEvents(tree, dispatcher);
                    RunnerLogger.Info("SMS", $"Tree events bound for '{id}'.");
                }
                else
                {
                    RunnerLogger.Warn("SMS", $"BindEvents skipped for '{id}' (dispatcher not found).");
                }

                return NullValue.Instance;
            });
            fields["GetSelectedPath"] = new NativeFunctionValue(_ =>
            {
                var selected = tree.GetSelected();
                return new StringValue(selected?.GetMetadata(0).AsString() ?? string.Empty);
            });
        }

        if (runtimeObject is PopupMenu popup)
        {
            var source = popup.GetParent() as Control;
            fields["AddMenuItem"] = new NativeFunctionValue(methodArgs =>
            {
                var itemKey = ValueArgString(methodArgs, 0);
                if (string.IsNullOrWhiteSpace(itemKey))
                {
                    return NullValue.Instance;
                }

                var requestedPopupId = ValueArgInt(methodArgs, 1);
                var popupId = requestedPopupId > 0 ? requestedPopupId : ResolveNextPopupItemId(popup);

                popup.AddItem(itemKey, popupId);
                var createdIndex = popup.ItemCount - 1;
                popup.SetItemMetadata(createdIndex, Variant.From(itemKey));
                RegisterDynamicMenuItem(id, popup, popupId, itemKey, source);
                return new NumberValue(popupId);
            });
        }

        if (runtimeObject is MenuButton menuButton)
        {
            var buttonPopup = menuButton.GetPopup();
            fields["AddMenuItem"] = new NativeFunctionValue(methodArgs =>
            {
                var itemKey = ValueArgString(methodArgs, 0);
                if (string.IsNullOrWhiteSpace(itemKey))
                {
                    return NullValue.Instance;
                }

                var requestedPopupId = ValueArgInt(methodArgs, 1);
                var popupId = requestedPopupId > 0 ? requestedPopupId : ResolveNextPopupItemId(buttonPopup);

                buttonPopup.AddItem(itemKey, popupId);
                var createdIndex = buttonPopup.ItemCount - 1;
                buttonPopup.SetItemMetadata(createdIndex, Variant.From(itemKey));
                RegisterDynamicMenuItem(id, buttonPopup, popupId, itemKey, menuButton);
                return new NumberValue(popupId);
            });
        }

        if (runtimeObject is Window window)
        {
            var windowInstanceId = window.GetInstanceId();
            EnsureWindowCloseBinding(window);

            fields["onClose"] = new NativeFunctionValue(methodArgs =>
            {
                var callbackName = ValueArgString(methodArgs, 0);
                if (string.IsNullOrWhiteSpace(callbackName))
                {
                    _windowCloseCallbacks.Remove(windowInstanceId);
                    return NullValue.Instance;
                }

                _windowCloseCallbacks[windowInstanceId] = callbackName;
                RunnerLogger.Info("SMS", $"Registered onClose callback '{callbackName}' for window #{windowInstanceId}.");
                return NullValue.Instance;
            });
            fields["close"] = new NativeFunctionValue(_ =>
            {
                if (!GodotObject.IsInstanceValid(window))
                {
                    return NullValue.Instance;
                }

                InvokeWindowCloseCallback(windowInstanceId);
                window.QueueFree();
                return NullValue.Instance;
            });
        }
    }

    private Dictionary<string, List<MethodInfo>> GetMethodMap(Type type)
    {
        if (_methodCache.TryGetValue(type, out var cached))
        {
            return cached;
        }

        var map = new Dictionary<string, List<MethodInfo>>(StringComparer.Ordinal);
        var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        foreach (var method in methods)
        {
            if (method.IsSpecialName)
            {
                continue;
            }

            if (method.DeclaringType == typeof(object))
            {
                continue;
            }

            if (method.GetParameters().Any(p => p.ParameterType.IsByRef || p.IsOut))
            {
                continue;
            }

            AddMethodAlias(map, method.Name, method);
            AddMethodAlias(map, ToLowerCamel(method.Name), method);
        }

        _methodCache[type] = map;
        return map;
    }

    private static void AddMethodAlias(Dictionary<string, List<MethodInfo>> map, string name, MethodInfo method)
    {
        if (!map.TryGetValue(name, out var list))
        {
            list = [];
            map[name] = list;
        }

        if (!list.Contains(method))
        {
            list.Add(method);
        }
    }

    private static string ToLowerCamel(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || char.IsLower(value[0]))
        {
            return value;
        }

        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    private bool TrySelectMethod(IReadOnlyList<MethodInfo> methods, IReadOnlyList<Value> smsArgs, out MethodInfo selected, out object?[] convertedArgs)
    {
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var requiredCount = 0;
            foreach (var parameter in parameters)
            {
                if (!parameter.HasDefaultValue)
                {
                    requiredCount++;
                }
            }

            if (smsArgs.Count < requiredCount || smsArgs.Count > parameters.Length)
            {
                continue;
            }

            var buffer = new object?[parameters.Length];
            var ok = true;
            for (var i = 0; i < smsArgs.Count; i++)
            {
                if (!TryConvertSmsValue(smsArgs[i], parameters[i].ParameterType, out var converted))
                {
                    ok = false;
                    break;
                }

                buffer[i] = converted;
            }

            if (!ok)
            {
                continue;
            }

            for (var i = smsArgs.Count; i < parameters.Length; i++)
            {
                if (parameters[i].HasDefaultValue)
                {
                    buffer[i] = parameters[i].DefaultValue;
                    continue;
                }

                ok = false;
                break;
            }

            if (!ok)
            {
                continue;
            }

            selected = method;
            convertedArgs = buffer;
            return true;
        }

        selected = null!;
        convertedArgs = [];
        return false;
    }

    private bool TryConvertSmsValue(Value value, Type targetType, out object? converted)
    {
        var nullableUnderlying = Nullable.GetUnderlyingType(targetType);
        if (nullableUnderlying is not null)
        {
            if (value is NullValue)
            {
                converted = null;
                return true;
            }

            targetType = nullableUnderlying;
        }

        if (targetType == typeof(Value))
        {
            converted = value;
            return true;
        }

        if (targetType == typeof(string))
        {
            converted = ValueAsString(value);
            return true;
        }

        if (targetType == typeof(bool))
        {
            converted = value switch
            {
                BooleanValue b => b.Value,
                NumberValue n => Math.Abs(n.Value) > double.Epsilon,
                StringValue s => string.Equals(s.Value, "true", StringComparison.OrdinalIgnoreCase),
                _ => ValueUtils.IsTruthy(value)
            };
            return true;
        }

        if (targetType == typeof(int))
        {
            converted = value switch
            {
                NumberValue n => n.ToInt(),
                StringValue s when int.TryParse(s.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) => i,
                _ => 0
            };
            return true;
        }

        if (targetType == typeof(long))
        {
            converted = value switch
            {
                NumberValue n => (long)n.Value,
                StringValue s when long.TryParse(s.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) => l,
                _ => 0L
            };
            return true;
        }

        if (targetType == typeof(float))
        {
            converted = value switch
            {
                NumberValue n => (float)n.Value,
                StringValue s when float.TryParse(s.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) => f,
                _ => 0f
            };
            return true;
        }

        if (targetType == typeof(double))
        {
            converted = value switch
            {
                NumberValue n => n.Value,
                StringValue s when double.TryParse(s.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) => d,
                _ => 0d
            };
            return true;
        }

        if (targetType.IsEnum)
        {
            if (value is NumberValue enumNumber)
            {
                converted = Enum.ToObject(targetType, enumNumber.ToInt());
                return true;
            }

            if (value is StringValue enumString)
            {
                try
                {
                    converted = Enum.Parse(targetType, enumString.Value, ignoreCase: true);
                    return true;
                }
                catch
                {
                    converted = null;
                    return false;
                }
            }
        }

        if (targetType == typeof(Variant))
        {
            converted = Variant.From(ValueUtils.ToDotNet(value));
            return true;
        }

        if (value is ObjectValue objectValue
            && objectValue.GetField("__nativeObjectId") is NumberValue nativeIdValue)
        {
            var nativeId = (long)nativeIdValue.Value;
            if (_nativeObjects.TryGetValue(nativeId, out var native)
                && targetType.IsInstanceOfType(native))
            {
                converted = native;
                return true;
            }
        }

        converted = null;
        return false;
    }

    private Value ToSmsValue(object? value)
    {
        return value switch
        {
            null => NullValue.Instance,
            Value smsValue => smsValue,
            bool b => new BooleanValue(b),
            string s => new StringValue(s),
            int i => new NumberValue(i),
            long l => new NumberValue(l),
            float f => new NumberValue(f),
            double d => new NumberValue(d),
            Enum e => new NumberValue(Convert.ToInt32(e, CultureInfo.InvariantCulture)),
            Node node => CreateRuntimeObject(
                node.HasMeta(NodePropertyMapper.MetaId)
                    ? node.GetMeta(NodePropertyMapper.MetaId).AsString()
                    : string.Empty,
                node),
            GodotObject godotObject => CreateRuntimeObject(string.Empty, godotObject),
            _ => ValueUtils.FromDotNet(value)
        };
    }

    private void EnsureWindowCloseBinding(Window window)
    {
        var windowInstanceId = window.GetInstanceId();
        if (!_windowCloseHooked.Add(windowInstanceId))
        {
            return;
        }

        window.CloseRequested += () =>
        {
            InvokeWindowCloseCallback(windowInstanceId);
        };

        window.TreeExited += () =>
        {
            _windowCloseCallbacks.Remove(windowInstanceId);
            _windowCloseHooked.Remove(windowInstanceId);
        };
    }

    private void InvokeWindowCloseCallback(ulong windowInstanceId)
    {
        if (!_windowCloseCallbacks.Remove(windowInstanceId, out var callbackName)
            || string.IsNullOrWhiteSpace(callbackName))
        {
            return;
        }

        ExecuteCall($"{callbackName}()");
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

    private void TryInvokeEvent(string targetId, string eventName, params object?[] args)
        => TryInvokeEvent(targetId, eventName, warnIfMissing: true, args);

    private void TryInvokeEvent(string targetId, string eventName, bool warnIfMissing, params object?[] args)
    {
        try
        {
            var handled = _engine.InvokeEvent(targetId, eventName, args);
            if (!handled && warnIfMissing)
            {
                RunnerLogger.Warn("SMS", $"No SMS event handler found for '{targetId}.{eventName}'.");
            }
        }
        catch (Exception ex)
        {
            RunnerLogger.Warn("SMS", $"SMS event dispatch failed for '{targetId}.{eventName}'.", ex);
        }
    }

    private static bool ShouldSuppressMissingMenuHandler(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        return DockingHostControl.IsMenuItemMappedToPanel(itemId);
    }

    private static string ResolveSourceId(UiActionContext ctx)
    {
        if (!string.IsNullOrWhiteSpace(ctx.SourceId))
        {
            return ctx.SourceId;
        }

        if (ctx.Source is not null && ctx.Source.HasMeta(NodePropertyMapper.MetaId))
        {
            return ctx.Source.GetMeta(NodePropertyMapper.MetaId).AsString();
        }

        return string.Empty;
    }

    private ObjectValue? TryCreateMenuItemObject(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        if (!TryResolveMenuItemById(itemId, out var popup, out var itemIndex, out var menuId))
        {
            return null;
        }

        var text = popup.GetItemText(itemIndex);
        var isChecked = popup.IsItemChecked(itemIndex);

        var values = new Dictionary<string, Value>(StringComparer.Ordinal)
        {
            ["id"] = new StringValue(itemId),
            ["text"] = new StringValue(text),
            ["isChecked"] = new BooleanValue(isChecked),
            ["menuId"] = new StringValue(menuId)
        };

        var menuItem = new ObjectValue("MenuItem", values);
        values["SetChecked"] = new NativeFunctionValue(methodArgs =>
        {
            var checkedState = ValueArgBool(methodArgs, 0);
            if (TryResolveMenuItemById(itemId, out var p, out var idx, out _))
            {
                p.SetItemAsCheckable(idx, true);
                p.SetItemChecked(idx, checkedState);
                values["isChecked"] = new BooleanValue(checkedState);
            }

            return NullValue.Instance;
        });
        values["SetText"] = new NativeFunctionValue(methodArgs =>
        {
            var nextText = ValueArgString(methodArgs, 0);
            if (TryResolveMenuItemById(itemId, out var p, out var idx, out _))
            {
                p.SetItemText(idx, nextText);
                values["text"] = new StringValue(nextText);
            }

            return NullValue.Instance;
        });
        values["GetText"] = new NativeFunctionValue(__ =>
        {
            if (TryResolveMenuItemById(itemId, out var p, out var idx, out var _menuId))
            {
                var current = p.GetItemText(idx);
                values["text"] = new StringValue(current);
                return new StringValue(current);
            }

            return new StringValue(string.Empty);
        });
        values["IsChecked"] = new NativeFunctionValue(__ =>
        {
            if (TryResolveMenuItemById(itemId, out var p, out var idx, out var _menuId))
            {
                var current = p.IsItemChecked(idx);
                values["isChecked"] = new BooleanValue(current);
                return new BooleanValue(current);
            }

            return new BooleanValue(false);
        });

        return menuItem;
    }

    private bool TryResolveMenuItemById(string itemId, out PopupMenu popup, out int itemIndex, out string menuId)
    {
        foreach (var currentPopup in EnumeratePopupMenus())
        {
            var count = currentPopup.ItemCount;
            for (var i = 0; i < count; i++)
            {
                var mappedId = currentPopup.GetItemMetadata(i).AsString();
                if (string.IsNullOrWhiteSpace(mappedId))
                {
                    continue;
                }

                if (!string.Equals(mappedId, itemId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                popup = currentPopup;
                itemIndex = i;
                menuId = ResolveMenuIdForPopup(currentPopup);
                return true;
            }
        }

        popup = null!;
        itemIndex = -1;
        menuId = string.Empty;
        return false;
    }

    private IEnumerable<PopupMenu> EnumeratePopupMenus()
    {
        if (Engine.GetMainLoop() is not SceneTree sceneTree || sceneTree.Root is null)
        {
            yield break;
        }

        var stack = new Stack<Node>();
        stack.Push(sceneTree.Root);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (node is PopupMenu popup)
            {
                yield return popup;
            }

            for (var i = node.GetChildCount() - 1; i >= 0; i--)
            {
                stack.Push(node.GetChild(i));
            }
        }
    }

    private static string ResolveMenuIdForPopup(PopupMenu popup)
    {
        if (popup.HasMeta(NodePropertyMapper.MetaId))
        {
            var own = popup.GetMeta(NodePropertyMapper.MetaId).AsString();
            if (!string.IsNullOrWhiteSpace(own))
            {
                return own;
            }
        }

        if (popup.GetParent() is Control controlParent && controlParent.HasMeta(NodePropertyMapper.MetaId))
        {
            var parent = controlParent.GetMeta(NodePropertyMapper.MetaId).AsString();
            if (!string.IsNullOrWhiteSpace(parent))
            {
                return parent;
            }
        }

        return "menu";
    }

    private void EnsureDynamicMenuBinding(string menuId, PopupMenu popup, Control? sourceControl)
    {
        var popupInstanceId = popup.GetInstanceId();
        if (!_dynamicMenuItemsByPopup.ContainsKey(popupInstanceId))
        {
            _dynamicMenuItemsByPopup[popupInstanceId] = new Dictionary<long, string>();
        }
        if (sourceControl is not null)
        {
            _dynamicMenuSourcesByPopup[popupInstanceId] = sourceControl;
        }

        if (!_dynamicMenuPopupHooked.Add(popupInstanceId))
        {
            return;
        }

        popup.IdPressed += id =>
        {
            if (_dynamicMenuItemsByPopup.TryGetValue(popupInstanceId, out var mapped)
                && mapped.TryGetValue(id, out var clicked))
            {
                DispatchMenuSelection(popupInstanceId, menuId, clicked);
            }
        };
    }

    private void RegisterDynamicMenuItem(string menuId, PopupMenu popup, long popupId, string clicked, Control? sourceControl)
    {
        var popupInstanceId = popup.GetInstanceId();
        if (!_dynamicMenuItemsByPopup.TryGetValue(popupInstanceId, out var mapped))
        {
            mapped = new Dictionary<long, string>();
            _dynamicMenuItemsByPopup[popupInstanceId] = mapped;
        }
        if (sourceControl is not null)
        {
            _dynamicMenuSourcesByPopup[popupInstanceId] = sourceControl;
        }

        mapped[popupId] = clicked;
        EnsureDynamicMenuBinding(menuId, popup, sourceControl);
    }

    private void DispatchMenuSelection(ulong popupInstanceId, string menuId, string clicked)
    {
        if (_dispatcher is not null
            && _dynamicMenuSourcesByPopup.TryGetValue(popupInstanceId, out var sourceControl)
            && GodotObject.IsInstanceValid(sourceControl))
        {
            _dispatcher.Dispatch(new UiActionContext(
                Source: sourceControl,
                SourceId: menuId,
                SourceIdValue: IdRuntimeScope.GetOrCreate(menuId),
                Action: "menuItemSelected",
                Clicked: clicked,
                ClickedIdValue: IdRuntimeScope.GetOrCreate(clicked)
            ));
            return;
        }

        if (string.IsNullOrWhiteSpace(clicked))
        {
            RunnerLogger.Warn("SMS", $"Dynamic menu selection ignored for '{menuId}': clicked item id is empty.");
            return;
        }

        // Event-first only fallback when no dispatcher is available yet.
        TryInvokeEvent(clicked, "clicked");
    }

    private static int ResolveNextPopupItemId(PopupMenu popup)
    {
        var candidate = 1;
        var count = popup.ItemCount;
        for (var i = 0; i < count; i++)
        {
            var existing = popup.GetItemId(i);
            if (existing >= candidate)
            {
                candidate = existing + 1;
            }
        }

        return candidate;
    }

    private void BootstrapUiIdSymbols()
    {
        var ids = EnumerateUiIds().Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (ids.Length == 0)
        {
            return;
        }

        var declared = 0;
        foreach (var id in ids)
        {
            if (!IsValidSmsIdentifier(id)
                || IsSmsKeyword(id)
                || string.Equals(id, "fs", StringComparison.Ordinal)
                || string.Equals(id, "log", StringComparison.Ordinal)
                || string.Equals(id, "ui", StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                _engine.Execute($"var {id} = {Quote(id)}");
                declared++;
            }
            catch (Exception ex)
            {
                RunnerLogger.Warn("SMS", $"Could not expose UI id symbol '{id}' to SMS globals.", ex);
            }
        }

        if (declared > 0)
        {
            RunnerLogger.Info("SMS", $"Exposed {declared} UI id symbols as SMS globals.");
        }
    }

    private static IEnumerable<string> EnumerateUiIds()
    {
        if (Engine.GetMainLoop() is not SceneTree sceneTree || sceneTree.Root is null)
        {
            return Array.Empty<string>();
        }

        var ids = new List<string>();
        var stack = new Stack<Node>();
        stack.Push(sceneTree.Root);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (node.HasMeta(NodePropertyMapper.MetaId))
            {
                var id = node.GetMeta(NodePropertyMapper.MetaId).AsString();
                if (!string.IsNullOrWhiteSpace(id))
                {
                    ids.Add(id);
                }
            }

            for (var i = node.GetChildCount() - 1; i >= 0; i--)
            {
                stack.Push(node.GetChild(i));
            }
        }

        return ids;
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

    private static bool IsValidSmsIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!(char.IsLetter(value[0]) || value[0] == '_'))
        {
            return false;
        }

        for (var i = 1; i < value.Length; i++)
        {
            var c = value[i];
            if (!(char.IsLetterOrDigit(c) || c == '_'))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsSmsKeyword(string value)
    {
        return value is "var" or "fun" or "if" or "else" or "for" or "while" or "return" or "true" or "false" or "null" or "data" or "break" or "continue";
    }

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
