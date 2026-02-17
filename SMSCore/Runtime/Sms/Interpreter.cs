namespace Runtime.Sms;

internal sealed class BreakException : Exception;
internal sealed class ContinueException : Exception;
internal sealed class ReturnException(Value value) : Exception
{
    public Value Value { get; } = value;
}

internal sealed record VariableBinding(Value InitialValue, PropertyAccessor? Getter = null, PropertyAccessor? Setter = null)
{
    public Value Value { get; set; } = InitialValue;
}

internal sealed class Scope(Scope? parent = null)
{
    private readonly Dictionary<string, VariableBinding> _variables = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FunctionDeclaration> _functions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DataClassDeclaration> _dataClasses = new(StringComparer.Ordinal);

    public void DefineVariable(string name, Value value, PropertyAccessor? getter = null, PropertyAccessor? setter = null)
        => _variables[name] = new VariableBinding(value, getter, setter);

    public VariableBinding? GetVariableBinding(string name)
        => _variables.TryGetValue(name, out var binding) ? binding : parent?.GetVariableBinding(name);

    public void DefineFunction(string name, FunctionDeclaration function) => _functions[name] = function;
    public FunctionDeclaration? GetFunction(string name) => _functions.TryGetValue(name, out var function) ? function : parent?.GetFunction(name);
    public void DefineDataClass(string name, DataClassDeclaration declaration) => _dataClasses[name] = declaration;
    public DataClassDeclaration? GetDataClass(string name) => _dataClasses.TryGetValue(name, out var d) ? d : parent?.GetDataClass(name);
}

public sealed class Interpreter
{
    private readonly NativeFunctionRegistry _nativeFunctions;
    private Scope _currentScope = new();
    private readonly Dictionary<string, EventHandlerDeclaration> _eventHandlers = new(StringComparer.Ordinal);
    private Action<string, string, IReadOnlyList<Value>>? _superDispatcher;
    private (string TargetId, string EventName)? _currentEventContext;

    public Interpreter(NativeFunctionRegistry? nativeFunctions = null)
    {
        _nativeFunctions = nativeFunctions ?? new NativeFunctionRegistry();
        _nativeFunctions.RegisterBuiltins();
    }

    public NativeFunctionRegistry NativeFunctions => _nativeFunctions;

    public void SetSuperDispatcher(Action<string, string, IReadOnlyList<Value>>? dispatcher)
    {
        _superDispatcher = dispatcher;
    }

    public bool InvokeEvent(string targetId, string eventName, IReadOnlyList<Value> args)
    {
        var key = BuildEventKey(targetId, eventName);
        if (!_eventHandlers.TryGetValue(key, out var handler))
        {
            return false;
        }

        if (args.Count != handler.Parameters.Count)
        {
            throw new RuntimeError($"Event {key} expects {handler.Parameters.Count} args, got {args.Count}");
        }

        var previousScope = _currentScope;
        var previousContext = _currentEventContext;
        _currentScope = new Scope(previousScope);
        _currentEventContext = (handler.TargetId, handler.EventName);
        try
        {
            for (var i = 0; i < handler.Parameters.Count; i++)
            {
                _currentScope.DefineVariable(handler.Parameters[i], args[i]);
            }

            _ = ExecuteBlock(handler.Body);
            return true;
        }
        finally
        {
            _currentScope = previousScope;
            _currentEventContext = previousContext;
        }
    }

    public Value Execute(ProgramNode program)
    {
        Value lastValue = NullValue.Instance;
        _eventHandlers.Clear();

        foreach (var statement in program.Statements)
        {
            switch (statement)
            {
                case FunctionDeclaration function:
                    _currentScope.DefineFunction(function.Name, function);
                    break;
                case DataClassDeclaration dataClass:
                    _currentScope.DefineDataClass(dataClass.Name, dataClass);
                    break;
                case EventHandlerDeclaration eventHandler:
                    _eventHandlers[BuildEventKey(eventHandler.TargetId, eventHandler.EventName)] = eventHandler;
                    break;
            }
        }

        foreach (var statement in program.Statements)
        {
            if (statement is FunctionDeclaration or DataClassDeclaration or EventHandlerDeclaration)
            {
                continue;
            }

            lastValue = ExecuteStatement(statement);
        }

        return lastValue;
    }

    private Value ExecuteStatement(Statement statement)
    {
        try
        {
            return statement switch
            {
                VarDeclaration varDecl => ExecuteVarDeclaration(varDecl),
                Assignment assignment => ExecuteAssignment(assignment),
                ExpressionStatement exprStatement => EvaluateExpression(exprStatement.Expression),
                IfStatement ifStatement => ExecuteIf(ifStatement),
                WhileStatement whileStatement => ExecuteWhile(whileStatement),
                ForStatement forStatement => ExecuteFor(forStatement),
                ForInStatement forInStatement => ExecuteForIn(forInStatement),
                BreakStatement => throw new BreakException(),
                ContinueStatement => throw new ContinueException(),
                ReturnStatement returnStatement => throw new ReturnException(returnStatement.Value is null ? NullValue.Instance : EvaluateExpression(returnStatement.Value)),
                _ => throw new RuntimeError("Unknown statement type in execution", statement.Position)
            };
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (BreakException)
        {
            throw;
        }
        catch (ContinueException)
        {
            throw;
        }
        catch (ReturnException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError($"Runtime error: {ex.Message}", statement.Position, ex);
        }
    }

    private Value ExecuteVarDeclaration(VarDeclaration statement)
    {
        var value = EvaluateExpression(statement.Value);
        _currentScope.DefineVariable(statement.Name, value, statement.Getter, statement.Setter);
        return NullValue.Instance;
    }

    private Value ExecuteAssignment(Assignment statement)
    {
        var value = EvaluateExpression(statement.Value);
        ApplyAssignmentTarget(statement.Target, value, statement.Position);
        return NullValue.Instance;
    }

    private Value ExecuteIf(IfStatement statement)
    {
        var condition = EvaluateExpression(statement.Condition);
        if (ValueUtils.IsTruthy(condition))
        {
            return ExecuteBlock(statement.ThenBranch);
        }

        return statement.ElseBranch is not null ? ExecuteBlock(statement.ElseBranch) : NullValue.Instance;
    }

    private Value ExecuteWhile(WhileStatement statement)
    {
        Value last = NullValue.Instance;
        try
        {
            while (ValueUtils.IsTruthy(EvaluateExpression(statement.Condition)))
            {
                try
                {
                    last = ExecuteBlock(statement.Body);
                }
                catch (ContinueException)
                {
                }
            }
        }
        catch (BreakException)
        {
        }

        return last;
    }

    private Value ExecuteFor(ForStatement statement)
    {
        Value last = NullValue.Instance;
        var prev = _currentScope;
        _currentScope = new Scope(prev);
        try
        {
            if (statement.Init is not null)
            {
                ExecuteStatement(statement.Init);
            }

            try
            {
                while (statement.Condition is null || ValueUtils.IsTruthy(EvaluateExpression(statement.Condition)))
                {
                    try
                    {
                        last = ExecuteBlock(statement.Body);
                    }
                    catch (ContinueException)
                    {
                    }

                    if (statement.Update is not null)
                    {
                        ExecuteStatement(statement.Update);
                    }
                }
            }
            catch (BreakException)
            {
            }
        }
        finally
        {
            _currentScope = prev;
        }

        return last;
    }

    private Value ExecuteForIn(ForInStatement statement)
    {
        Value last = NullValue.Instance;
        var prev = _currentScope;
        _currentScope = new Scope(prev);
        try
        {
            var iterable = EvaluateExpression(statement.Iterable);
            if (iterable is not ArrayValue array)
            {
                throw new RuntimeError("for-in requires an array", statement.Position);
            }

            try
            {
                foreach (var element in array.Elements)
                {
                    _currentScope.DefineVariable(statement.Variable, element);
                    try
                    {
                        last = ExecuteBlock(statement.Body);
                    }
                    catch (ContinueException)
                    {
                    }
                }
            }
            catch (BreakException)
            {
            }
        }
        finally
        {
            _currentScope = prev;
        }

        return last;
    }

    private Value ExecuteBlock(IReadOnlyList<Statement> statements)
    {
        var prev = _currentScope;
        _currentScope = new Scope(prev);
        try
        {
            Value last = NullValue.Instance;
            foreach (var statement in statements)
            {
                last = ExecuteStatement(statement);
            }

            return last;
        }
        finally
        {
            _currentScope = prev;
        }
    }

    private Value EvaluateExpression(Expression expression)
    {
        try
        {
            return expression switch
            {
                NumberLiteral number => new NumberValue(number.Value),
                StringLiteral str => new StringValue(str.Value),
                InterpolatedStringLiteral interpolated => EvaluateInterpolated(interpolated),
                BooleanLiteral boolean => new BooleanValue(boolean.Value),
                NullLiteral => NullValue.Instance,
                Identifier identifier => EvaluateIdentifier(identifier),
                BinaryExpression binary => EvaluateBinary(binary),
                UnaryExpression unary => EvaluateUnary(unary),
                PostfixExpression postfix => EvaluatePostfix(postfix),
                FunctionCall call => EvaluateFunctionCall(call),
                MethodCall call => EvaluateMethodCall(call),
                MemberAccess access => EvaluateMemberAccess(access),
                ArrayAccess access => EvaluateArrayAccess(access),
                ArrayLiteral literal => new ArrayValue(literal.Elements.Select(EvaluateExpression).ToList()),
                IfExpression ifExpression => ValueUtils.IsTruthy(EvaluateExpression(ifExpression.Condition))
                    ? EvaluateExpression(ifExpression.ThenBranch)
                    : EvaluateExpression(ifExpression.ElseBranch),
                AssignmentExpression assignment => ApplyAssignmentTarget(assignment.Target, EvaluateExpression(assignment.Value), assignment.Position),
                WhenExpression whenExpr => EvaluateWhen(whenExpr),
                _ => throw new RuntimeError("Unknown expression type", expression.Position)
            };
        }
        catch (ScriptError)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RuntimeError($"Expression evaluation error: {ex.Message}", expression.Position, ex);
        }
    }

    private Value EvaluateIdentifier(Identifier identifier)
    {
        var binding = _currentScope.GetVariableBinding(identifier.Name)
                      ?? throw new RuntimeError($"Undefined variable '{identifier.Name}'", identifier.Position);
        return binding.Getter is null ? binding.Value : EvaluateAccessor(binding.Getter, binding, null);
    }

    private Value EvaluateBinary(BinaryExpression expr)
    {
        var left = EvaluateExpression(expr.Left);
        var right = EvaluateExpression(expr.Right);

        return expr.Operator switch
        {
            "+" => EvaluatePlus(left, right, expr.Position),
            "-" => left is NumberValue ln && right is NumberValue rn
                ? new NumberValue(ln.Value - rn.Value)
                : throw new RuntimeError("Invalid operands for '-'", expr.Position),
            "*" => left is NumberValue lm && right is NumberValue rm
                ? new NumberValue(lm.Value * rm.Value)
                : throw new RuntimeError("Invalid operands for '*'", expr.Position),
            "/" => EvaluateDivide(left, right, expr.Position),
            "==" => new BooleanValue(ValueUtils.EqualsValue(left, right)),
            "!=" => new BooleanValue(!ValueUtils.EqualsValue(left, right)),
            "<" => left is NumberValue l1 && right is NumberValue r1 ? new BooleanValue(l1.Value < r1.Value) : throw new RuntimeError("Invalid operands for '<'", expr.Position),
            ">" => left is NumberValue l2 && right is NumberValue r2 ? new BooleanValue(l2.Value > r2.Value) : throw new RuntimeError("Invalid operands for '>'", expr.Position),
            "<=" => left is NumberValue l3 && right is NumberValue r3 ? new BooleanValue(l3.Value <= r3.Value) : throw new RuntimeError("Invalid operands for '<='", expr.Position),
            ">=" => left is NumberValue l4 && right is NumberValue r4 ? new BooleanValue(l4.Value >= r4.Value) : throw new RuntimeError("Invalid operands for '>='", expr.Position),
            "&&" => new BooleanValue(ValueUtils.IsTruthy(left) && ValueUtils.IsTruthy(right)),
            "||" => new BooleanValue(ValueUtils.IsTruthy(left) || ValueUtils.IsTruthy(right)),
            _ => throw new RuntimeError($"Unknown binary operator '{expr.Operator}'", expr.Position)
        };
    }

    private static Value EvaluatePlus(Value left, Value right, Position? position)
    {
        if (left is NumberValue l && right is NumberValue r)
        {
            return new NumberValue(l.Value + r.Value);
        }

        return new StringValue(ToStringValue(left) + ToStringValue(right));

        static string ToStringValue(Value value) => value switch
        {
            StringValue s => s.Value,
            NumberValue n => n.Value % 1 == 0 ? ((int)n.Value).ToString() : n.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            BooleanValue b => b.Value.ToString(),
            NullValue => "null",
            _ => value.ToString()
        };
    }

    private static Value EvaluateDivide(Value left, Value right, Position? position)
    {
        if (left is not NumberValue l || right is not NumberValue r)
        {
            throw new RuntimeError("Invalid operands for '/'", position);
        }

        if (r.Value == 0)
        {
            throw new RuntimeError("Division by zero", position);
        }

        return new NumberValue((int)l.Value / (int)r.Value);
    }

    private Value EvaluateUnary(UnaryExpression expr)
    {
        var operand = EvaluateExpression(expr.Operand);
        return expr.Operator switch
        {
            "-" => operand is NumberValue number ? new NumberValue(-number.Value) : throw new RuntimeError("Invalid operand for unary '-'", expr.Position),
            "+" => operand is NumberValue plus ? plus : throw new RuntimeError("Invalid operand for unary '+'", expr.Position),
            "!" => new BooleanValue(!ValueUtils.IsTruthy(operand)),
            _ => throw new RuntimeError($"Unknown unary operator '{expr.Operator}'", expr.Position)
        };
    }

    private Value EvaluatePostfix(PostfixExpression expr)
    {
        if (expr.Operand is not Identifier identifier)
        {
            throw new RuntimeError("Postfix operators only work on variables", expr.Position);
        }

        var binding = _currentScope.GetVariableBinding(identifier.Name)
                      ?? throw new RuntimeError($"Undefined variable '{identifier.Name}'", expr.Position);
        var current = binding.Getter is null ? binding.Value : EvaluateAccessor(binding.Getter, binding, null);
        if (current is not NumberValue number)
        {
            throw new RuntimeError("Postfix operators only work on numbers", expr.Position);
        }

        var newValue = expr.Operator switch
        {
            "++" => new NumberValue(number.Value + 1),
            "--" => new NumberValue(number.Value - 1),
            _ => throw new RuntimeError($"Unknown postfix operator '{expr.Operator}'", expr.Position)
        };

        AssignToBinding(binding, newValue);
        return current;
    }

    private Value EvaluateFunctionCall(FunctionCall call)
    {
        if (string.Equals(call.Name, "super", StringComparison.Ordinal))
        {
            return EvaluateSuperCall(call);
        }

        var args = call.Arguments.Select(EvaluateExpression).ToList();
        var native = _nativeFunctions.Get(call.Name);
        if (native is not null)
        {
            return native(args);
        }

        var dataClass = _currentScope.GetDataClass(call.Name);
        if (dataClass is not null)
        {
            if (args.Count != dataClass.Fields.Count)
            {
                throw new RuntimeError($"Expected {dataClass.Fields.Count} arguments for {dataClass.Name} constructor, got {args.Count}", call.Position);
            }

            var fields = new Dictionary<string, Value>(StringComparer.Ordinal);
            for (var i = 0; i < dataClass.Fields.Count; i++)
            {
                fields[dataClass.Fields[i]] = args[i];
            }

            return new ObjectValue(dataClass.Name, fields);
        }

        var function = _currentScope.GetFunction(call.Name)
                       ?? throw new RuntimeError($"Undefined function '{call.Name}'", call.Position);
        return CallFunction(function, args, call.Position);
    }

    private Value EvaluateSuperCall(FunctionCall call)
    {
        if (_currentEventContext is null)
        {
            throw new RuntimeError("super(...) can only be used inside an event handler", call.Position);
        }

        var args = call.Arguments.Select(EvaluateExpression).ToList();
        var (targetId, eventName) = _currentEventContext.Value;

        if (_superDispatcher is null)
        {
            throw new RuntimeError($"super dispatcher is not configured for event {targetId}.{eventName}", call.Position);
        }

        _superDispatcher(targetId, eventName, args);
        return NullValue.Instance;
    }

    private static string BuildEventKey(string targetId, string eventName)
    {
        return $"{targetId}.{eventName}";
    }

    private Value EvaluateMethodCall(MethodCall call)
    {
        var receiver = EvaluateExpression(call.Receiver);
        var args = call.Arguments.Select(EvaluateExpression).ToList();
        return receiver switch
        {
            ArrayValue array => CallArrayMethod(array, call.Method, args, call.Position),
            StringValue str => CallStringMethod(str, call.Method, args, call.Position),
            ObjectValue obj => CallObjectMethod(obj, call.Method, args, call.Position),
            NativeFunctionValue native => native.Function(args),
            _ => throw new RuntimeError($"Cannot call method '{call.Method}' on {receiver.GetType().Name}", call.Position)
        };
    }

    private static Value CallObjectMethod(ObjectValue obj, string method, IReadOnlyList<Value> args, Position? position)
    {
        var field = obj.GetField(method);
        if (field is NativeFunctionValue function)
        {
            return function.Function(args);
        }

        throw new RuntimeError($"Unknown object method '{method}' on '{obj.ClassName}'", position);
    }

    private static Value CallArrayMethod(ArrayValue array, string method, IReadOnlyList<Value> args, Position? position)
    {
        return method switch
        {
            "add" when args.Count == 1 => AddArray(array, args[0]),
            "remove" when args.Count == 1 => new BooleanValue(array.Elements.Remove(args[0])),
            "removeAt" when args.Count == 1 && args[0] is NumberValue index => RemoveAt(array, index.ToInt()),
            "contains" when args.Count == 1 => new BooleanValue(array.Elements.Contains(args[0])),
            _ => throw new RuntimeError($"Unknown array method '{method}'", position)
        };

        static Value AddArray(ArrayValue array, Value value)
        {
            array.Elements.Add(value);
            return NullValue.Instance;
        }

        static Value RemoveAt(ArrayValue array, int index)
        {
            if (index < 0 || index >= array.Elements.Count)
            {
                return NullValue.Instance;
            }

            var value = array.Elements[index];
            array.Elements.RemoveAt(index);
            return value;
        }
    }

    private static Value CallStringMethod(StringValue str, string method, IReadOnlyList<Value> _args, Position? position)
        => method switch
        {
            "length" => new NumberValue(str.Value.Length),
            "toUpperCase" => new StringValue(str.Value.ToUpperInvariant()),
            "toLowerCase" => new StringValue(str.Value.ToLowerInvariant()),
            "trim" => new StringValue(str.Value.Trim()),
            _ => throw new RuntimeError($"Unknown string method '{method}'", position)
        };

    private Value EvaluateMemberAccess(MemberAccess access)
    {
        var receiver = EvaluateExpression(access.Receiver);
        return receiver switch
        {
            ObjectValue obj => obj.GetField(access.Member),
            ArrayValue arr when access.Member == "size" => new NumberValue(arr.Size()),
            _ => throw new RuntimeError($"Cannot access member '{access.Member}' on {receiver.GetType().Name}", access.Position)
        };
    }

    private Value EvaluateArrayAccess(ArrayAccess access)
    {
        var receiver = EvaluateExpression(access.Receiver);
        var index = EvaluateExpression(access.Index);
        if (receiver is ArrayValue arr && index is NumberValue n)
        {
            return arr.Get(n.ToInt());
        }

        throw new RuntimeError("Invalid array access", access.Position);
    }

    private Value EvaluateWhen(WhenExpression expr)
    {
        var subject = expr.Subject is null ? null : EvaluateExpression(expr.Subject);
        foreach (var branch in expr.Branches)
        {
            if (branch.IsElse)
            {
                return EvaluateExpression(branch.Result);
            }

            var condition = branch.Condition is null
                ? throw new RuntimeError("When branch missing condition", branch.Position)
                : EvaluateExpression(branch.Condition);

            var matches = subject is null
                ? ValueUtils.IsTruthy(condition)
                : ValueUtils.EqualsValue(subject, condition);
            if (matches)
            {
                return EvaluateExpression(branch.Result);
            }
        }

        return NullValue.Instance;
    }

    private Value EvaluateInterpolated(InterpolatedStringLiteral expr)
    {
        var parts = new List<string>();
        foreach (var part in expr.Parts)
        {
            switch (part)
            {
                case TextPart text:
                    parts.Add(text.Value);
                    break;
                case ExpressionPart expressionPart:
                    parts.Add(EvaluateExpression(expressionPart.Expr).ToString());
                    break;
            }
        }

        return new StringValue(string.Concat(parts));
    }

    private Value CallFunction(FunctionDeclaration function, IReadOnlyList<Value> args, Position? position)
    {
        if (args.Count != function.Parameters.Count)
        {
            throw new RuntimeError($"Expected {function.Parameters.Count} arguments, got {args.Count}", position);
        }

        var prev = _currentScope;
        _currentScope = new Scope(prev);
        try
        {
            for (var i = 0; i < function.Parameters.Count; i++)
            {
                _currentScope.DefineVariable(function.Parameters[i], args[i]);
            }

            return ExecuteBlock(function.Body);
        }
        catch (ReturnException ret)
        {
            return ret.Value;
        }
        finally
        {
            _currentScope = prev;
        }
    }

    private Value ApplyAssignmentTarget(Expression target, Value value, Position? position)
    {
        switch (target)
        {
            case Identifier identifier:
            {
                var binding = _currentScope.GetVariableBinding(identifier.Name)
                              ?? throw new RuntimeError($"Undefined variable '{identifier.Name}'", position);
                AssignToBinding(binding, value);
                break;
            }
            case MemberAccess memberAccess:
            {
                var obj = EvaluateExpression(memberAccess.Receiver);
                if (obj is not ObjectValue objectValue)
                {
                    throw new RuntimeError("Cannot set field on non-object", position);
                }

                objectValue.SetField(memberAccess.Member, value);
                break;
            }
            case ArrayAccess arrayAccess:
            {
                var array = EvaluateExpression(arrayAccess.Receiver);
                var index = EvaluateExpression(arrayAccess.Index);
                if (array is not ArrayValue arr || index is not NumberValue n)
                {
                    throw new RuntimeError("Invalid array assignment", position);
                }

                arr.Set(n.ToInt(), value);
                break;
            }
            default:
                throw new RuntimeError("Invalid assignment target", position);
        }

        return value;
    }

    private Value EvaluateAccessor(PropertyAccessor accessor, VariableBinding binding, Value? newValue)
    {
        var prev = _currentScope;
        _currentScope = new Scope(prev);
        try
        {
            _currentScope.DefineVariable("field", binding.Value);
            if (!string.IsNullOrWhiteSpace(accessor.ParameterName))
            {
                _currentScope.DefineVariable(accessor.ParameterName!, newValue ?? NullValue.Instance);
            }

            var result = EvaluateExpression(accessor.Body);
            var updatedField = _currentScope.GetVariableBinding("field");
            if (updatedField is not null)
            {
                binding.Value = updatedField.Value;
            }

            return result;
        }
        finally
        {
            _currentScope = prev;
        }
    }

    private void AssignToBinding(VariableBinding binding, Value value)
    {
        if (binding.Setter is null)
        {
            binding.Value = value;
            return;
        }

        EvaluateAccessor(binding.Setter, binding, value);
    }
}
