#pragma once

#include <string>

namespace forge {

bool sms_error_is_missing_handler(const std::string& message);
bool sms_error_requires_exit(const std::string& message);

} // namespace forge

