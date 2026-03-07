#pragma once

#include <cctype>
#include <string>
#include <vector>

namespace smlcore {

enum class ValueKind {
    String,
    Number,
    Bool,
    Identifier,
    Tuple
};

struct Property {
    std::string name;
    ValueKind   kind{};
    std::string value;
};

struct Node {
    std::string            name;
    std::vector<Property>  properties;
    std::vector<Node>      children;

    bool has_property(const std::string& key) const {
        for (const auto& p : properties) {
            if (iequal(p.name, key)) return true;
        }
        return false;
    }

    const Property* find_property(const std::string& key) const {
        for (const auto& p : properties) {
            if (iequal(p.name, key)) return &p;
        }
        return nullptr;
    }

    std::string get_value(const std::string& key, const std::string& fallback = "") const {
        const auto* p = find_property(key);
        return p ? p->value : fallback;
    }

private:
    static bool iequal(const std::string& a, const std::string& b) {
        if (a.size() != b.size()) return false;
        for (std::size_t i = 0; i < a.size(); ++i) {
            if (std::tolower(static_cast<unsigned char>(a[i])) !=
                std::tolower(static_cast<unsigned char>(b[i])))
                return false;
        }
        return true;
    }
};

struct Document {
    std::vector<Node> roots;
};

/// Parse an SML source string and return the resulting document.
/// Throws std::runtime_error on parse errors.
Document parse_document(const std::string& source);

} // namespace smlcore
