#include "forge_sms_error_policy.h"

namespace forge {

bool sms_error_is_missing_handler(const std::string& message) {
    return message.find("No SMS event handler found") != std::string::npos;
}

bool sms_error_requires_exit(const std::string& message) {
    if (message.find("RuntimeError:") != std::string::npos) return true;
    if (message.find("Stack overflow") != std::string::npos) return true;
    return false;
}

} // namespace forge

