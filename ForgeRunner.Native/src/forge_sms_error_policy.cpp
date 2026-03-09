#include "forge_sms_error_policy.h"

namespace forge {

bool sms_error_is_missing_handler(const std::string& message) {
    return message.find("No SMS event handler found") != std::string::npos;
}

bool sms_error_requires_exit(const std::string& message) {
    return message.find("RuntimeError:") != std::string::npos;
}

} // namespace forge

