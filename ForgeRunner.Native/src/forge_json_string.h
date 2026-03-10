/*
#############################################################################
# Copyright (C) 2026 CrowdWare
#
# This file is part of Forge.
#
# SPDX-License-Identifier: GPL-3.0-or-later OR LicenseRef-CrowdWare-Commercial
#
# Forge is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# Forge is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with Forge. If not, see <https://www.gnu.org/licenses/>.
#
# Commercial licensing is available from CrowdWare for proprietary use.
#############################################################################
*/

#pragma once

#include <string>

namespace forge {

// Decodes a JSON string literal (for example "\"a\\nb\" -> "a\nb").
// Returns false if the input is not a valid JSON string literal.
bool decode_json_string_literal(const std::string& input, std::string& output);

// Returns decoded text for valid JSON string literals.
// Falls back to the input unchanged for non-string/invalid JSON.
std::string decode_json_string_or_fallback(const std::string& input);

} // namespace forge

