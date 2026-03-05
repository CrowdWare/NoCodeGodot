#include "smlcore_native.h"

#include <algorithm>
#include <cctype>
#include <cstdint>
#include <cstring>
#include <iomanip>
#include <sstream>
#include <stdexcept>
#include <string>
#include <utility>
#include <vector>

namespace {

enum class TokenKind {
    Eof,
    Identifier,
    Number,
    String,
    Bool,
    LBrace,
    RBrace,
    Colon
};

struct Token {
    TokenKind kind{};
    std::string text;
    int line = 1;
    int column = 1;
};

enum class ValueKind {
    String,
    Number,
    Bool,
    Identifier
};

struct Property {
    std::string name;
    ValueKind kind{};
    std::string value;
};

struct Node {
    std::string name;
    std::vector<Property> properties;
    std::vector<Node> children;
};

struct Document {
    std::vector<Node> roots;
};

std::string normalize_number_text(const std::string& raw) {
    // Canonical number text so managed/native AST comparison stays stable.
    // Examples: ".5" -> "0.5", "-.5" -> "-0.5", "1.2300" -> "1.23"
    const auto parsed = std::stod(raw);
    std::ostringstream oss;
    oss << std::fixed << std::setprecision(15) << parsed;
    auto text = oss.str();

    while (!text.empty() && text.back() == '0') {
        text.pop_back();
    }
    if (!text.empty() && text.back() == '.') {
        text.pop_back();
    }
    if (text.empty() || text == "-0") {
        return "0";
    }
    return text;
}

class Lexer {
public:
    explicit Lexer(std::string source) : source_(std::move(source)) {}

    Token next() {
        skip_ignorables();

        if (is_at_end()) {
            return make_token(TokenKind::Eof, "");
        }

        const char c = peek();
        if (c == '{') {
            advance();
            return make_token(TokenKind::LBrace, "{");
        }
        if (c == '}') {
            advance();
            return make_token(TokenKind::RBrace, "}");
        }
        if (c == ':') {
            advance();
            return make_token(TokenKind::Colon, ":");
        }
        if (c == '"') {
            return read_string();
        }
        if (is_number_start(c)) {
            return read_number();
        }
        if (is_identifier_start(c)) {
            return read_identifier_or_bool();
        }

        std::ostringstream oss;
        oss << "Unexpected character '" << c << "' at line " << line_ << ", col " << column_;
        throw std::runtime_error(oss.str());
    }

private:
    static bool is_identifier_start(char c) {
        return std::isalpha(static_cast<unsigned char>(c)) || c == '_';
    }

    static bool is_identifier_char(char c) {
        return std::isalnum(static_cast<unsigned char>(c)) || c == '_' || c == '.';
    }

    static bool is_number_start(char c) {
        return std::isdigit(static_cast<unsigned char>(c)) || c == '-' || c == '.';
    }

    Token make_token(TokenKind kind, std::string text) const {
        return Token{kind, std::move(text), line_, column_};
    }

    bool is_at_end() const {
        return index_ >= source_.size();
    }

    char peek() const {
        return is_at_end() ? '\0' : source_[index_];
    }

    char peek_next() const {
        return (index_ + 1 < source_.size()) ? source_[index_ + 1] : '\0';
    }

    char advance() {
        const auto c = source_[index_++];
        if (c == '\n') {
            line_++;
            column_ = 1;
        } else {
            column_++;
        }
        return c;
    }

    void skip_ignorables() {
        while (!is_at_end()) {
            const char c = peek();
            if (c == ' ' || c == '\t' || c == '\r' || c == '\n') {
                advance();
                continue;
            }

            if (c == '/' && peek_next() == '/') {
                while (!is_at_end() && peek() != '\n') {
                    advance();
                }
                continue;
            }

            break;
        }
    }

    Token read_identifier_or_bool() {
        const auto token_line = line_;
        const auto token_col = column_;
        std::string text;
        text.push_back(advance());
        while (!is_at_end() && is_identifier_char(peek())) {
            text.push_back(advance());
        }

        if (text == "true" || text == "false") {
            return Token{TokenKind::Bool, text, token_line, token_col};
        }
        return Token{TokenKind::Identifier, text, token_line, token_col};
    }

    Token read_number() {
        const auto token_line = line_;
        const auto token_col = column_;
        std::string text;

        if (peek() == '-') {
            text.push_back(advance());
        }

        bool has_digits_before_dot = false;
        while (!is_at_end() && std::isdigit(static_cast<unsigned char>(peek()))) {
            has_digits_before_dot = true;
            text.push_back(advance());
        }

        if (!is_at_end() && peek() == '.') {
            text.push_back(advance());
            while (!is_at_end() && std::isdigit(static_cast<unsigned char>(peek()))) {
                text.push_back(advance());
            }
        } else if (!has_digits_before_dot && text == "-") {
            throw std::runtime_error("Invalid number token: '-' without digits.");
        }

        if (text == "." || text == "-.") {
            throw std::runtime_error("Invalid number token.");
        }

        return Token{TokenKind::Number, text, token_line, token_col};
    }

    Token read_string() {
        const auto token_line = line_;
        const auto token_col = column_;
        advance(); // opening quote
        std::string text;
        bool escape = false;

        while (!is_at_end()) {
            const char c = advance();
            if (escape) {
                switch (c) {
                    case 'n': text.push_back('\n'); break;
                    case 'r': text.push_back('\r'); break;
                    case 't': text.push_back('\t'); break;
                    case '"': text.push_back('"'); break;
                    case '\\': text.push_back('\\'); break;
                    default: text.push_back(c); break;
                }
                escape = false;
                continue;
            }

            if (c == '\\') {
                escape = true;
                continue;
            }

            if (c == '"') {
                return Token{TokenKind::String, text, token_line, token_col};
            }

            text.push_back(c);
        }

        throw std::runtime_error("Unterminated string literal.");
    }

    std::string source_;
    std::size_t index_ = 0;
    int line_ = 1;
    int column_ = 1;
};

class Parser {
public:
    explicit Parser(std::string source) : lexer_(std::move(source)) {
        consume();
    }

    Document parse_document() {
        Document doc;
        while (lookahead_.kind != TokenKind::Eof) {
            doc.roots.push_back(parse_element());
        }
        return doc;
    }

private:
    Node parse_element() {
        const auto name = consume_expect(TokenKind::Identifier, "Expected element name").text;
        consume_expect(TokenKind::LBrace, "Expected '{' after element name");

        Node node;
        node.name = name;

        while (lookahead_.kind != TokenKind::RBrace && lookahead_.kind != TokenKind::Eof) {
            const auto ident = consume_expect(TokenKind::Identifier, "Expected property or child element name");
            if (lookahead_.kind == TokenKind::Colon) {
                consume();
                node.properties.push_back(parse_property(ident.text));
                continue;
            }

            if (lookahead_.kind == TokenKind::LBrace) {
                Node child;
                child.name = ident.text;
                consume();
                parse_element_body(child);
                node.children.push_back(std::move(child));
                continue;
            }

            std::ostringstream oss;
            oss << "Expected ':' or '{' after '" << ident.text << "' at line " << lookahead_.line
                << ", col " << lookahead_.column;
            throw std::runtime_error(oss.str());
        }

        consume_expect(TokenKind::RBrace, "Expected '}' at end of element");
        return node;
    }

    void parse_element_body(Node& node) {
        while (lookahead_.kind != TokenKind::RBrace && lookahead_.kind != TokenKind::Eof) {
            const auto ident = consume_expect(TokenKind::Identifier, "Expected property or child element name");
            if (lookahead_.kind == TokenKind::Colon) {
                consume();
                node.properties.push_back(parse_property(ident.text));
                continue;
            }

            if (lookahead_.kind == TokenKind::LBrace) {
                Node child;
                child.name = ident.text;
                consume();
                parse_element_body(child);
                node.children.push_back(std::move(child));
                continue;
            }

            std::ostringstream oss;
            oss << "Expected ':' or '{' after '" << ident.text << "' at line " << lookahead_.line
                << ", col " << lookahead_.column;
            throw std::runtime_error(oss.str());
        }

        consume_expect(TokenKind::RBrace, "Expected '}' at end of nested element");
    }

    Property parse_property(const std::string& name) {
        if (lookahead_.kind == TokenKind::String) {
            const auto token = consume();
            return Property{name, ValueKind::String, token.text};
        }
        if (lookahead_.kind == TokenKind::Number) {
            const auto token = consume();
            return Property{name, ValueKind::Number, normalize_number_text(token.text)};
        }
        if (lookahead_.kind == TokenKind::Bool) {
            const auto token = consume();
            return Property{name, ValueKind::Bool, token.text};
        }
        if (lookahead_.kind == TokenKind::Identifier) {
            const auto token = consume();
            return Property{name, ValueKind::Identifier, token.text};
        }

        std::ostringstream oss;
        oss << "Expected scalar value after property '" << name << "' at line " << lookahead_.line
            << ", col " << lookahead_.column;
        throw std::runtime_error(oss.str());
    }

    Token consume_expect(TokenKind kind, const char* message) {
        if (lookahead_.kind != kind) {
            std::ostringstream oss;
            oss << message << " at line " << lookahead_.line << ", col " << lookahead_.column;
            throw std::runtime_error(oss.str());
        }
        return consume();
    }

    Token consume() {
        Token current = lookahead_;
        lookahead_ = lexer_.next();
        return current;
    }

    Lexer lexer_;
    Token lookahead_{};
};

std::string json_escape(const std::string& input) {
    std::string out;
    out.reserve(input.size() + 8);
    for (const auto c : input) {
        switch (c) {
            case '\\': out += "\\\\"; break;
            case '"': out += "\\\""; break;
            case '\n': out += "\\n"; break;
            case '\r': out += "\\r"; break;
            case '\t': out += "\\t"; break;
            default:
                out.push_back(c);
                break;
        }
    }
    return out;
}

const char* value_kind_name(ValueKind kind) {
    switch (kind) {
        case ValueKind::String: return "string";
        case ValueKind::Number: return "number";
        case ValueKind::Bool: return "bool";
        case ValueKind::Identifier: return "identifier";
        default: return "unknown";
    }
}

void append_node_json(std::ostringstream& oss, const Node& node) {
    oss << "{\"name\":\"" << json_escape(node.name) << "\",\"properties\":[";
    for (std::size_t i = 0; i < node.properties.size(); i++) {
        const auto& prop = node.properties[i];
        if (i > 0) {
            oss << ",";
        }
        oss << "{\"name\":\"" << json_escape(prop.name)
            << "\",\"kind\":\"" << value_kind_name(prop.kind)
            << "\",\"value\":\"" << json_escape(prop.value) << "\"}";
    }
    oss << "],\"children\":[";
    for (std::size_t i = 0; i < node.children.size(); i++) {
        if (i > 0) {
            oss << ",";
        }
        append_node_json(oss, node.children[i]);
    }
    oss << "]}";
}

std::string to_ast_json(const Document& document) {
    std::ostringstream oss;
    oss << "{\"roots\":[";
    for (std::size_t i = 0; i < document.roots.size(); i++) {
        if (i > 0) {
            oss << ",";
        }
        append_node_json(oss, document.roots[i]);
    }
    oss << "]}";
    return oss.str();
}

std::int64_t count_nodes(const Node& node) {
    std::int64_t total = 1;
    for (const auto& child : node.children) {
        total += count_nodes(child);
    }
    return total;
}

std::int64_t count_document_nodes(const Document& document) {
    std::int64_t total = 0;
    for (const auto& root : document.roots) {
        total += count_nodes(root);
    }
    return total;
}

void write_error(char* error, int capacity, const std::string& message) {
    if (error == nullptr || capacity <= 0) {
        return;
    }

    const auto count = static_cast<int>(std::min<std::size_t>(message.size(), static_cast<std::size_t>(capacity - 1)));
    std::memcpy(error, message.data(), static_cast<std::size_t>(count));
    error[count] = '\0';
}

int write_json_output(const std::string& json, char* out_json, int out_json_capacity, std::int64_t* out_json_length, char* error, int error_capacity) {
    if (out_json_length != nullptr) {
        *out_json_length = static_cast<std::int64_t>(json.size());
    }

    if (out_json == nullptr || out_json_capacity <= 0) {
        write_error(error, error_capacity, "out_json/out_json_capacity must be provided");
        return 2;
    }

    if (static_cast<std::size_t>(out_json_capacity) <= json.size()) {
        write_error(error, error_capacity, "out_json buffer too small");
        return 2;
    }

    std::memcpy(out_json, json.data(), json.size());
    out_json[json.size()] = '\0';
    return 0;
}

} // namespace

extern "C" int smlcore_native_parse(const char* source, std::int64_t* out_node_count, char* error, int error_capacity) {
    if (source == nullptr || out_node_count == nullptr) {
        write_error(error, error_capacity, "source/out_node_count must not be null");
        return 2;
    }

    try {
        Parser parser(source);
        auto document = parser.parse_document();
        *out_node_count = count_document_nodes(document);
        return 0;
    } catch (const std::exception& ex) {
        write_error(error, error_capacity, ex.what());
        return 1;
    } catch (...) {
        write_error(error, error_capacity, "unknown native exception");
        return 1;
    }
}

extern "C" int smlcore_native_parse_ast_json(
    const char* source,
    char* out_json,
    int out_json_capacity,
    std::int64_t* out_json_length,
    char* error,
    int error_capacity) {
    if (source == nullptr) {
        write_error(error, error_capacity, "source must not be null");
        return 2;
    }

    try {
        Parser parser(source);
        auto document = parser.parse_document();
        const auto json = to_ast_json(document);
        return write_json_output(json, out_json, out_json_capacity, out_json_length, error, error_capacity);
    } catch (const std::exception& ex) {
        write_error(error, error_capacity, ex.what());
        return 1;
    } catch (...) {
        write_error(error, error_capacity, "unknown native exception");
        return 1;
    }
}
