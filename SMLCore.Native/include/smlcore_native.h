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

#include <cstdint>

#if defined(_WIN32)
#define SMLCORE_EXPORT __declspec(dllexport)
#else
#define SMLCORE_EXPORT __attribute__((visibility("default")))
#endif

extern "C" {
SMLCORE_EXPORT int smlcore_native_parse(
    const char* source,
    std::int64_t* out_node_count,
    char* error,
    int error_capacity);

SMLCORE_EXPORT int smlcore_native_parse_ast_json(
    const char* source,
    char* out_json,
    int out_json_capacity,
    std::int64_t* out_json_length,
    char* error,
    int error_capacity);
}
