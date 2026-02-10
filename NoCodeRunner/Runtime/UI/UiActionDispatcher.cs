using Godot;
using Runtime.Logging;
using System;
using System.Collections.Generic;

namespace Runtime.UI;

public readonly record struct UiActionContext(
    Control Source,
    string SourceId,
    string Action,
    string Clicked,
    double? NumericValue = null
);

public sealed class UiActionDispatcher
{
    private readonly Dictionary<string, Action<UiActionContext>> _actionHandlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Action<UiActionContext>> _idHandlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Action<UiActionContext>> _observers = [];
    private Action<string>? _pageHandler;

    public void RegisterObserver(Action<UiActionContext> observer)
    {
        _observers.Add(observer ?? throw new ArgumentNullException(nameof(observer)));
    }

    public void RegisterActionHandler(string actionName, Action<UiActionContext> handler)
    {
        if (string.IsNullOrWhiteSpace(actionName))
        {
            throw new ArgumentException("Action name must not be empty.", nameof(actionName));
        }

        _actionHandlers[actionName] = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public void RegisterActionHandlerIfMissing(string actionName, Action<UiActionContext> handler)
    {
        if (string.IsNullOrWhiteSpace(actionName))
        {
            throw new ArgumentException("Action name must not be empty.", nameof(actionName));
        }

        if (!_actionHandlers.ContainsKey(actionName))
        {
            _actionHandlers[actionName] = handler ?? throw new ArgumentNullException(nameof(handler));
        }
    }

    public void RegisterIdHandler(string id, Action<UiActionContext> handler)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("SML id must not be empty.", nameof(id));
        }

        _idHandlers[id] = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public void RegisterIdHandlerIfMissing(string id, Action<UiActionContext> handler)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("SML id must not be empty.", nameof(id));
        }

        if (!_idHandlers.ContainsKey(id))
        {
            _idHandlers[id] = handler ?? throw new ArgumentNullException(nameof(handler));
        }
    }

    public void SetPageHandler(Action<string> handler)
    {
        _pageHandler = handler;
    }

    public void SetPageHandlerIfMissing(Action<string> handler)
    {
        _pageHandler ??= handler;
    }

    public bool Dispatch(UiActionContext context)
    {
        foreach (var observer in _observers)
        {
            try
            {
                observer(context);
            }
            catch (Exception ex)
            {
                RunnerLogger.Warn("UI", $"Action observer threw exception: {ex.Message}");
            }
        }

        if (TryResolveTyped(context.Clicked, context, out var handledTypedClicked))
        {
            return handledTypedClicked;
        }

        if (TryResolveTyped(context.Action, context, out var handledTypedAction))
        {
            return handledTypedAction;
        }

        if (!string.IsNullOrWhiteSpace(context.Action) && _actionHandlers.TryGetValue(context.Action, out var actionHandler))
        {
            actionHandler(context);
            return true;
        }

        if (!string.IsNullOrWhiteSpace(context.SourceId) && _idHandlers.TryGetValue(context.SourceId, out var sourceIdHandler))
        {
            sourceIdHandler(context);
            return true;
        }

        if (!string.IsNullOrWhiteSpace(context.Clicked)
            && context.Clicked.IndexOf(':') < 0
            && _idHandlers.TryGetValue(context.Clicked, out var clickedIdHandler))
        {
            clickedIdHandler(context);
            return true;
        }

        RunnerLogger.Warn("UI", $"No action handler resolved (id='{context.SourceId}', action='{context.Action}', clicked='{context.Clicked}').");
        return false;
    }

    private bool TryResolveTyped(string expression, UiActionContext context, out bool handled)
    {
        handled = false;
        if (string.IsNullOrWhiteSpace(expression))
        {
            return false;
        }

        var separator = expression.IndexOf(':');
        if (separator <= 0)
        {
            return false;
        }

        var kind = expression[..separator].Trim().ToLowerInvariant();
        var payload = expression[(separator + 1)..].Trim();

        if (string.IsNullOrWhiteSpace(payload))
        {
            RunnerLogger.Warn("UI", $"Typed action '{expression}' has empty payload and was ignored.");
            handled = true;
            return true;
        }

        switch (kind)
        {
            case "action":
                if (_actionHandlers.TryGetValue(payload, out var namedActionHandler))
                {
                    namedActionHandler(context with { Action = payload });
                }
                else
                {
                    RunnerLogger.Warn("UI", $"Missing action handler for action '{payload}'.");
                }

                handled = true;
                return true;

            case "page":
                if (_pageHandler is null)
                {
                    RunnerLogger.Warn("UI", $"Page action requested ('{payload}') but no page handler is registered.");
                }
                else
                {
                    _pageHandler(payload);
                }

                handled = true;
                return true;

            case "web":
                HandleWeb(payload);
                handled = true;
                return true;

            default:
                RunnerLogger.Warn("UI", $"Unknown typed action kind '{kind}' in expression '{expression}'.");
                handled = true;
                return true;
        }
    }

    private static void HandleWeb(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed))
        {
            RunnerLogger.Warn("UI", $"web action ignored: invalid URL '{url}'.");
            return;
        }

        var scheme = parsed.Scheme.ToLowerInvariant();
        if (scheme is not ("http" or "https" or "file"))
        {
            RunnerLogger.Warn("UI", $"web action ignored: scheme '{parsed.Scheme}' is not allowed. Allowed: http, https, file.");
            return;
        }

        OS.ShellOpen(url);
        RunnerLogger.Info("UI", $"Opened URL via OS shell: {url}");
    }
}
