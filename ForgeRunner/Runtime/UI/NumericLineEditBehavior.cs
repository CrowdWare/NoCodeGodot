using Godot;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Runtime.UI;

internal sealed class NumericLineEditBehavior
{
    private static readonly Regex NumberRegex = new(
        @"[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly LineEdit _lineEdit;
    private bool _editing;
    private bool _pressed;
    private bool _dragging;
    private Vector2 _dragStartPos;
    private double _dragStartValue;
    private double _value;
    private string _axis = "x";
    private string _unit = "m";
    private int _decimals = 3;
    private double _step = 0.01d;
    private double _dragSensitivity = 0.02d;
    private Color _axisColor = Colors.White;
    private bool _suppressTextSync;

    public NumericLineEditBehavior(LineEdit lineEdit)
    {
        _lineEdit = lineEdit;
        _lineEdit.TextChanged += OnTextChanged;
        _lineEdit.FocusEntered += OnFocusEntered;
        _lineEdit.FocusExited += OnFocusExited;
        _lineEdit.TextSubmitted += OnTextSubmitted;
        _lineEdit.GuiInput += OnGuiInput;
    }

    public void Configure(string axis, string unit, Color axisColor, double step, double dragSensitivity, int decimals)
    {
        _axis = string.IsNullOrWhiteSpace(axis) ? "x" : axis.Trim().ToLowerInvariant();
        _unit = unit?.Trim() ?? string.Empty;
        _axisColor = axisColor;
        _step = Math.Max(0.000001d, Math.Abs(step));
        _dragSensitivity = Math.Max(0.000001d, Math.Abs(dragSensitivity));
        _decimals = Math.Clamp(decimals, 0, 6);

        _lineEdit.SelectAllOnFocus = true;
        _lineEdit.Editable = true;
        _lineEdit.FocusMode = Control.FocusModeEnum.All;
        ExitEditMode(showPreview: true);
    }

    public void SetValue(double value)
    {
        _value = value;
        if (_editing)
            WriteText(FormatRaw(_value));
        else
            WriteText(FormatPreview(_value));
    }

    public double GetValue()
    {
        if (_editing && TryParseNumber(_lineEdit.Text, out var parsed))
        {
            _value = parsed;
        }
        return _value;
    }

    private void OnTextChanged(string newText)
    {
        if (_suppressTextSync || !_editing)
            return;

        if (TryParseNumber(newText, out var parsed))
        {
            _value = parsed;
        }
    }

    private void OnFocusEntered()
    {
        EnterEditMode();
    }

    private void OnFocusExited()
    {
        if (_dragging)
            return;
        CommitEditAndShowPreview();
    }

    private void OnTextSubmitted(string _)
    {
        CommitEditAndShowPreview();
    }

    private void OnGuiInput(InputEvent @event)
    {
        if (_editing)
            return;

        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.Pressed)
            {
                _pressed = true;
                _dragging = false;
                _dragStartPos = mb.Position;
                _dragStartValue = _value;
                _lineEdit.AcceptEvent();
                return;
            }

            if (_pressed)
            {
                var wasDragging = _dragging;
                _pressed = false;
                _dragging = false;
                _lineEdit.AcceptEvent();
                if (!wasDragging)
                {
                    EnterEditMode();
                }
                return;
            }
        }

        if (_pressed && @event is InputEventMouseMotion mm)
        {
            var deltaX = mm.Position.X - _dragStartPos.X;
            if (!_dragging && Math.Abs(deltaX) >= 2.0f)
            {
                _dragging = true;
            }
            if (_dragging)
            {
                var scaled = deltaX * _dragSensitivity;
                var next = _dragStartValue + (scaled * _step);
                SetValue(next);
                _lineEdit.AcceptEvent();
            }
        }
    }

    private void EnterEditMode()
    {
        _editing = true;
        _lineEdit.RemoveThemeColorOverride("font_color");
        _lineEdit.RemoveThemeColorOverride("caret_color");
        WriteText(FormatRaw(_value));
        _lineEdit.GrabFocus();
        _lineEdit.SelectAll();
    }

    private void CommitEditAndShowPreview()
    {
        if (TryParseNumber(_lineEdit.Text, out var parsed))
            _value = parsed;
        ExitEditMode(showPreview: true);
    }

    private void ExitEditMode(bool showPreview)
    {
        _editing = false;
        _lineEdit.Editable = true;
        _lineEdit.FocusMode = Control.FocusModeEnum.All;
        _lineEdit.AddThemeColorOverride("font_color", _axisColor);
        _lineEdit.AddThemeColorOverride("caret_color", _axisColor);
        if (showPreview)
            WriteText(FormatPreview(_value));
    }

    private void WriteText(string text)
    {
        _suppressTextSync = true;
        _lineEdit.Text = text;
        _suppressTextSync = false;
    }

    private string FormatRaw(double value)
        => value.ToString("G9", CultureInfo.InvariantCulture);

    private string FormatPreview(double value)
    {
        var number = value.ToString($"F{_decimals}", CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(_unit))
            return $"{_axis} {number}";
        return $"{_axis} {number} {_unit}";
    }

    private static bool TryParseNumber(string text, out double value)
    {
        value = 0d;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return true;

        var match = NumberRegex.Match(text);
        if (!match.Success)
            return false;

        return double.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
