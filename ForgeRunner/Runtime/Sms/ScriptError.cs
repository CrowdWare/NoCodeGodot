/*
 * Copyright (C) 2026 CrowdWare
 *
 * This file is part of SMSCore.
 *
 *  SMSCore is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  SMSCore is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with SMSCore.  If not, see <http://www.gnu.org/licenses/>.
 */

namespace Runtime.Sms;

public class ScriptError : Exception
{
    public ScriptError(string message, Position? position = null, Exception? inner = null)
        : base(position is null ? message : $"Error at {position}: {message}", inner)
    {
        Position = position;
    }

    public Position? Position { get; }
}

public sealed class LexError(string message, Position position, Exception? inner = null)
    : ScriptError(message, position, inner);

public sealed class ParseError(string message, Position? position = null, Exception? inner = null)
    : ScriptError(message, position, inner);

public sealed class RuntimeError(string message, Position? position = null, Exception? inner = null)
    : ScriptError(message, position, inner);
