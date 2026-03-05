#include "sms_native.h"

#include <algorithm>
#include <cctype>
#include <cstddef>
#include <cstdint>
#include <cstring>
#include <chrono>
#include <cstdlib>
#include <filesystem>
#include <iostream>
#include <memory>
#include <mutex>
#include <stdexcept>
#include <string>
#include <unordered_map>
#include <utility>
#include <vector>

namespace {

struct SmsSessionRuntime;

struct SmsSessionState {
    std::string source;
    std::shared_ptr<SmsSessionRuntime> runtime;
};

std::mutex g_sessions_mutex;
std::unordered_map<std::int64_t, SmsSessionState> g_sessions;
std::int64_t g_next_session_id = 1;
sms_native_ui_get_prop_fn g_ui_get_prop = nullptr;
sms_native_ui_set_prop_fn g_ui_set_prop = nullptr;
sms_native_ui_invoke_fn g_ui_invoke = nullptr;

enum class TokenType {
    Eof,
    Identifier,
    Number,
    String,
    Var,
    For,
    In,
    If,
    Else,
    When,
    While,
    Fun,
    Data,
    Class,
    On,
    True,
    False,
    Null,
    Break,
    Continue,
    Return,
    LeftParen,
    RightParen,
    LeftBrace,
    RightBrace,
    LeftBracket,
    RightBracket,
    Dot,
    Comma,
    Semicolon,
    Assign,
    Plus,
    Increment,
    Minus,
    Decrement,
    Star,
    Slash,
    Percent,
    Arrow,
    Less,
    Greater,
    LessEqual,
    GreaterEqual,
    EqualEqual,
    BangEqual,
    Bang,
    AndAnd,
    OrOr
};

struct Token {
    TokenType type;
    std::string text;
};

class Lexer {
public:
    explicit Lexer(std::string src) : source_(std::move(src)) {}

    std::vector<Token> tokenize() {
        std::vector<Token> out;
        while (true) {
            skip_whitespace();
            if (is_at_end()) {
                out.push_back({TokenType::Eof, ""});
                return out;
            }

            const char c = advance();
            switch (c) {
                case '(':
                    out.push_back({TokenType::LeftParen, "("});
                    break;
                case ')':
                    out.push_back({TokenType::RightParen, ")"});
                    break;
                case '{':
                    out.push_back({TokenType::LeftBrace, "{"});
                    break;
                case '}':
                    out.push_back({TokenType::RightBrace, "}"});
                    break;
                case '[':
                    out.push_back({TokenType::LeftBracket, "["});
                    break;
                case ']':
                    out.push_back({TokenType::RightBracket, "]"});
                    break;
                case '.':
                    out.push_back({TokenType::Dot, "."});
                    break;
                case ',':
                    out.push_back({TokenType::Comma, ","});
                    break;
                case ';':
                    out.push_back({TokenType::Semicolon, ";"});
                    break;
                case '=':
                    if (!is_at_end() && peek() == '=') {
                        advance();
                        out.push_back({TokenType::EqualEqual, "=="});
                    } else {
                        out.push_back({TokenType::Assign, "="});
                    }
                    break;
                case '+':
                    if (!is_at_end() && peek() == '+') {
                        advance();
                        out.push_back({TokenType::Increment, "++"});
                    } else {
                        out.push_back({TokenType::Plus, "+"});
                    }
                    break;
                case '-':
                    if (!is_at_end() && peek() == '>') {
                        advance();
                        out.push_back({TokenType::Arrow, "->"});
                    } else if (!is_at_end() && peek() == '-') {
                        advance();
                        out.push_back({TokenType::Decrement, "--"});
                    } else {
                        out.push_back({TokenType::Minus, "-"});
                    }
                    break;
                case '*':
                    out.push_back({TokenType::Star, "*"});
                    break;
                case '/':
                    if (!is_at_end() && peek() == '/') {
                        // line comment
                        advance(); // consume second '/'
                        while (!is_at_end()) {
                            const char ch = advance();
                            if (ch == '\n') {
                                break;
                            }
                        }
                    } else if (!is_at_end() && peek() == '*') {
                        // block comment
                        advance(); // consume '*'
                        bool closed = false;
                        while (!is_at_end()) {
                            const char ch = advance();
                            if (ch == '*' && !is_at_end() && peek() == '/') {
                                advance(); // consume '/'
                                closed = true;
                                break;
                            }
                        }
                        if (!closed) {
                            throw std::runtime_error("Unterminated block comment.");
                        }
                    } else {
                        out.push_back({TokenType::Slash, "/"});
                    }
                    break;
                case '%':
                    out.push_back({TokenType::Percent, "%"});
                    break;
                case '<':
                    if (!is_at_end() && peek() == '=') {
                        advance();
                        out.push_back({TokenType::LessEqual, "<="});
                    } else {
                        out.push_back({TokenType::Less, "<"});
                    }
                    break;
                case '>':
                    if (!is_at_end() && peek() == '=') {
                        advance();
                        out.push_back({TokenType::GreaterEqual, ">="});
                    } else {
                        out.push_back({TokenType::Greater, ">"});
                    }
                    break;
                case '!':
                    if (!is_at_end() && peek() == '=') {
                        advance();
                        out.push_back({TokenType::BangEqual, "!="});
                    } else {
                        out.push_back({TokenType::Bang, "!"});
                    }
                    break;
                case '&':
                    if (!is_at_end() && peek() == '&') {
                        advance();
                        out.push_back({TokenType::AndAnd, "&&"});
                    } else {
                        throw std::runtime_error("Unexpected character in source.");
                    }
                    break;
                case '|':
                    if (!is_at_end() && peek() == '|') {
                        advance();
                        out.push_back({TokenType::OrOr, "||"});
                    } else {
                        throw std::runtime_error("Unexpected character in source.");
                    }
                    break;
                default:
                    if (std::isdigit(static_cast<unsigned char>(c))) {
                        out.push_back(number_token(c));
                    } else if (c == '"') {
                        out.push_back(string_token());
                    } else if (std::isalpha(static_cast<unsigned char>(c)) || c == '_') {
                        out.push_back(identifier_token(c));
                    } else {
                        throw std::runtime_error("Unexpected character in source.");
                    }
                    break;
            }
        }
    }

private:
    bool is_at_end() const { return index_ >= source_.size(); }
    char advance() { return source_[index_++]; }
    char peek() const { return is_at_end() ? '\0' : source_[index_]; }

    void skip_whitespace() {
        while (!is_at_end()) {
            const char c = peek();
            if (c == ' ' || c == '\t' || c == '\n' || c == '\r') {
                index_++;
                continue;
            }
            break;
        }
    }

    Token number_token(char first) {
        std::string text;
        text.push_back(first);
        while (!is_at_end() && std::isdigit(static_cast<unsigned char>(peek()))) {
            text.push_back(advance());
        }
        return {TokenType::Number, text};
    }

    Token string_token() {
        std::string text;
        bool escape = false;
        while (!is_at_end()) {
            const char c = advance();
            if (escape) {
                switch (c) {
                    case '"': text.push_back('"'); break;
                    case '\\': text.push_back('\\'); break;
                    case '/': text.push_back('/'); break;
                    case 'b': text.push_back('\b'); break;
                    case 'f': text.push_back('\f'); break;
                    case 'n': text.push_back('\n'); break;
                    case 'r': text.push_back('\r'); break;
                    case 't': text.push_back('\t'); break;
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
                return {TokenType::String, text};
            }

            text.push_back(c);
        }

        throw std::runtime_error("Unterminated string literal.");
    }

    Token identifier_token(char first) {
        std::string text;
        text.push_back(first);
        while (!is_at_end()) {
            const char c = peek();
            if (std::isalnum(static_cast<unsigned char>(c)) || c == '_') {
                text.push_back(advance());
                continue;
            }
            break;
        }

        if (text == "var") return {TokenType::Var, text};
        if (text == "for") return {TokenType::For, text};
        if (text == "in") return {TokenType::In, text};
        if (text == "if") return {TokenType::If, text};
        if (text == "else") return {TokenType::Else, text};
        if (text == "when") return {TokenType::When, text};
        if (text == "while") return {TokenType::While, text};
        if (text == "fun") return {TokenType::Fun, text};
        if (text == "data") return {TokenType::Data, text};
        if (text == "class") return {TokenType::Class, text};
        if (text == "on") return {TokenType::On, text};
        if (text == "true") return {TokenType::True, text};
        if (text == "false") return {TokenType::False, text};
        if (text == "null") return {TokenType::Null, text};
        if (text == "break") return {TokenType::Break, text};
        if (text == "continue") return {TokenType::Continue, text};
        if (text == "return") return {TokenType::Return, text};
        return {TokenType::Identifier, text};
    }

    std::string source_;
    std::size_t index_ = 0;
};

struct FunctionDeclStmt;
struct DataClassDeclStmt;
struct EventHandlerDeclStmt;

struct Value {
    enum class Kind { Int, Bool, String, Array, Object, Null };

    Kind kind = Kind::Null;
    std::int64_t int_value = 0;
    bool bool_value = false;
    std::string string_value;
    std::shared_ptr<std::vector<Value>> array;
    std::string class_name;
    std::shared_ptr<std::unordered_map<std::string, Value>> object_fields;

    static Value Int(std::int64_t value) {
        Value v;
        v.kind = Kind::Int;
        v.int_value = value;
        return v;
    }

    static Value Bool(bool value) {
        Value v;
        v.kind = Kind::Bool;
        v.bool_value = value;
        return v;
    }

    static Value String(std::string value) {
        Value v;
        v.kind = Kind::String;
        v.string_value = std::move(value);
        return v;
    }

    static Value Array(std::vector<Value> elements) {
        Value v;
        v.kind = Kind::Array;
        v.array = std::make_shared<std::vector<Value>>(std::move(elements));
        return v;
    }

    static Value Null() {
        return Value{};
    }

    static Value Object(std::string class_name, std::unordered_map<std::string, Value> fields) {
        Value v;
        v.kind = Kind::Object;
        v.class_name = std::move(class_name);
        v.object_fields = std::make_shared<std::unordered_map<std::string, Value>>(std::move(fields));
        return v;
    }

    bool truthy() const {
        if (kind == Kind::Int) return int_value != 0;
        if (kind == Kind::Bool) return bool_value;
        if (kind == Kind::String) return !string_value.empty();
        if (kind == Kind::Array) return array && !array->empty();
        if (kind == Kind::Object) return object_fields && !object_fields->empty();
        return false;
    }

    std::int64_t as_int(const std::string& where) const {
        if (kind == Kind::Int) {
            return int_value;
        }

        if (kind == Kind::Bool) {
            return bool_value ? 1 : 0;
        }

        throw std::runtime_error(where + " expects integer value.");
    }

    bool operator==(const Value& other) const {
        if (kind != other.kind) {
            return false;
        }

        switch (kind) {
            case Kind::Int:
                return int_value == other.int_value;
            case Kind::Bool:
                return bool_value == other.bool_value;
            case Kind::String:
                return string_value == other.string_value;
            case Kind::Null:
                return true;
            case Kind::Array:
                if (!array || !other.array) {
                    return array == other.array;
                }
                return *array == *other.array;
            case Kind::Object:
                if (!object_fields || !other.object_fields) {
                    return object_fields == other.object_fields;
                }
                return class_name == other.class_name && *object_fields == *other.object_fields;
        }

        return false;
    }
};

static std::string value_to_string(const Value& value) {
    switch (value.kind) {
        case Value::Kind::Int:
            return std::to_string(value.int_value);
        case Value::Kind::Bool:
            return value.bool_value ? "true" : "false";
        case Value::Kind::String:
            return value.string_value;
        case Value::Kind::Null:
            return "null";
        case Value::Kind::Array:
            return "[array]";
        case Value::Kind::Object:
            return "[object]";
    }
    return "";
}

static std::string json_escape(const std::string& text) {
    std::string out;
    out.reserve(text.size() + 8);
    for (const auto ch : text) {
        switch (ch) {
            case '\\': out += "\\\\"; break;
            case '"': out += "\\\""; break;
            case '\n': out += "\\n"; break;
            case '\r': out += "\\r"; break;
            case '\t': out += "\\t"; break;
            default: out.push_back(ch); break;
        }
    }
    return out;
}

static std::string value_to_json(const Value& value) {
    switch (value.kind) {
        case Value::Kind::Int:
            return std::to_string(value.int_value);
        case Value::Kind::Bool:
            return value.bool_value ? "true" : "false";
        case Value::Kind::String:
            return std::string("\"") + json_escape(value.string_value) + "\"";
        case Value::Kind::Null:
            return "null";
        case Value::Kind::Array:
            return "null";
        case Value::Kind::Object:
            if (value.class_name == "__host_ref" && value.object_fields) {
                const auto it = value.object_fields->find("__nativeObjectId");
                if (it != value.object_fields->end() && it->second.kind == Value::Kind::Int) {
                    return std::string("{\"__nativeObjectId\":") + std::to_string(it->second.int_value) + "}";
                }
            }
            return "null";
    }
    return "null";
}

static Value parse_json_scalar(std::string text) {
    auto trim = [](std::string& s) {
        auto start = std::size_t{0};
        while (start < s.size() && std::isspace(static_cast<unsigned char>(s[start])) != 0) {
            start++;
        }
        auto end = s.size();
        while (end > start && std::isspace(static_cast<unsigned char>(s[end - 1])) != 0) {
            end--;
        }
        s = s.substr(start, end - start);
    };

    trim(text);
    if (text.empty() || text == "null") {
        return Value::Null();
    }
    if (text == "true") {
        return Value::Bool(true);
    }
    if (text == "false") {
        return Value::Bool(false);
    }
    if (text.front() == '"' && text.back() == '"' && text.size() >= 2) {
        std::string out;
        out.reserve(text.size() - 2);
        bool escape = false;
        for (std::size_t i = 1; i + 1 < text.size(); i++) {
            const auto c = text[i];
            if (escape) {
                switch (c) {
                    case 'n': out.push_back('\n'); break;
                    case 'r': out.push_back('\r'); break;
                    case 't': out.push_back('\t'); break;
                    case '"': out.push_back('"'); break;
                    case '\\': out.push_back('\\'); break;
                    default: out.push_back(c); break;
                }
                escape = false;
                continue;
            }
            if (c == '\\') {
                escape = true;
                continue;
            }
            out.push_back(c);
        }
        return Value::String(std::move(out));
    }

    try {
        std::size_t consumed = 0;
        const auto number = std::stoll(text, &consumed, 10);
        if (consumed == text.size()) {
            return Value::Int(number);
        }
    } catch (...) {
    }

    return Value::String(text);
}

static Value parse_json_value(std::string text) {
    auto trim = [](std::string& s) {
        auto start = std::size_t{0};
        while (start < s.size() && std::isspace(static_cast<unsigned char>(s[start])) != 0) {
            start++;
        }
        auto end = s.size();
        while (end > start && std::isspace(static_cast<unsigned char>(s[end - 1])) != 0) {
            end--;
        }
        s = s.substr(start, end - start);
    };

    trim(text);
    if (text.rfind("{\"__nativeObjectId\":", 0) == 0 && !text.empty() && text.back() == '}') {
        const auto prefix = std::string("{\"__nativeObjectId\":");
        const auto number_text = text.substr(prefix.size(), text.size() - prefix.size() - 1);
        try {
            const auto id = std::stoll(number_text);
            std::unordered_map<std::string, Value> fields;
            fields.emplace("__nativeObjectId", Value::Int(id));
            return Value::Object("__host_ref", std::move(fields));
        } catch (...) {
            return Value::Null();
        }
    }

    if (!text.empty() && text.front() == '{' && text.back() == '}') {
        std::unordered_map<std::string, Value> fields;
        std::size_t i = 1;

        auto skip_ws = [&]() {
            while (i < text.size() && std::isspace(static_cast<unsigned char>(text[i])) != 0) {
                i++;
            }
        };

        auto parse_string_token = [&]() -> std::string {
            if (i >= text.size() || text[i] != '"') {
                throw std::runtime_error("Expected JSON string token.");
            }
            i++;
            std::string out;
            bool escape = false;
            while (i < text.size()) {
                const char c = text[i++];
                if (escape) {
                    switch (c) {
                        case 'n': out.push_back('\n'); break;
                        case 'r': out.push_back('\r'); break;
                        case 't': out.push_back('\t'); break;
                        case '"': out.push_back('"'); break;
                        case '\\': out.push_back('\\'); break;
                        default: out.push_back(c); break;
                    }
                    escape = false;
                    continue;
                }
                if (c == '\\') {
                    escape = true;
                    continue;
                }
                if (c == '"') {
                    return out;
                }
                out.push_back(c);
            }
            throw std::runtime_error("Unterminated JSON string token.");
        };

        auto parse_scalar_until = [&](char delimiter_a, char delimiter_b) -> Value {
            skip_ws();
            if (i < text.size() && text[i] == '"') {
                return Value::String(parse_string_token());
            }
            const auto start = i;
            while (i < text.size() && text[i] != delimiter_a && text[i] != delimiter_b) {
                i++;
            }
            auto raw = text.substr(start, i - start);
            return parse_json_scalar(std::move(raw));
        };

        skip_ws();
        while (i < text.size() && text[i] != '}') {
            skip_ws();
            const auto key = parse_string_token();
            skip_ws();
            if (i >= text.size() || text[i] != ':') {
                throw std::runtime_error("Expected ':' in JSON object.");
            }
            i++;
            auto value = parse_scalar_until(',', '}');
            fields[key] = std::move(value);
            skip_ws();
            if (i < text.size() && text[i] == ',') {
                i++;
                continue;
            }
            if (i < text.size() && text[i] == '}') {
                break;
            }
        }

        return Value::Object("__json_obj", std::move(fields));
    }

    return parse_json_scalar(std::move(text));
}

class Env {
public:
    explicit Env(Env* parent = nullptr) : parent_(parent) {}

    void define_var(const std::string& name, const Value& value) {
        vars_[name] = value;
    }

    Value get_var(const std::string& name) const {
        const auto it = vars_.find(name);
        if (it != vars_.end()) {
            return it->second;
        }
        if (parent_ != nullptr) {
            return parent_->get_var(name);
        }
        throw std::runtime_error("Unknown variable: " + name);
    }

    bool assign_var(const std::string& name, const Value& value) {
        const auto it = vars_.find(name);
        if (it != vars_.end()) {
            it->second = value;
            return true;
        }
        if (parent_ != nullptr) {
            return parent_->assign_var(name, value);
        }
        return false;
    }

    void define_function(const std::string& name, const FunctionDeclStmt* decl) {
        root()->functions_[name] = decl;
    }

    const FunctionDeclStmt* get_function(const std::string& name) const {
        const auto* r = root();
        const auto it = r->functions_.find(name);
        return it == r->functions_.end() ? nullptr : it->second;
    }

    void define_data_class(const std::string& name, const DataClassDeclStmt* decl) {
        root()->data_classes_[name] = decl;
    }

    const DataClassDeclStmt* get_data_class(const std::string& name) const {
        const auto* r = root();
        const auto it = r->data_classes_.find(name);
        return it == r->data_classes_.end() ? nullptr : it->second;
    }

    void define_event_handler(const std::string& key, const EventHandlerDeclStmt* decl) {
        root()->event_handlers_[key] = decl;
    }

    const EventHandlerDeclStmt* get_event_handler(const std::string& key) const {
        const auto* r = root();
        const auto it = r->event_handlers_.find(key);
        return it == r->event_handlers_.end() ? nullptr : it->second;
    }

private:
    Env* root() {
        return parent_ == nullptr ? this : parent_->root();
    }

    const Env* root() const {
        return parent_ == nullptr ? this : parent_->root();
    }

    Env* parent_ = nullptr;
    std::unordered_map<std::string, Value> vars_;
    std::unordered_map<std::string, const FunctionDeclStmt*> functions_;
    std::unordered_map<std::string, const DataClassDeclStmt*> data_classes_;
    std::unordered_map<std::string, const EventHandlerDeclStmt*> event_handlers_;
};

struct ReturnSignal final : std::exception {
    explicit ReturnSignal(Value v) : value(std::move(v)) {}
    Value value;
};

struct BreakSignal final : std::exception {};
struct ContinueSignal final : std::exception {};

struct Expr {
    virtual ~Expr() = default;
    virtual Value eval(Env& env) const = 0;
};

struct NumberExpr final : Expr {
    explicit NumberExpr(std::int64_t v) : value(v) {}
    Value eval(Env&) const override { return Value::Int(value); }
    std::int64_t value;
};

struct StringExpr final : Expr {
    explicit StringExpr(std::string v) : value(std::move(v)) {}
    Value eval(Env&) const override { return Value::String(value); }
    std::string value;
};

struct BoolExpr final : Expr {
    explicit BoolExpr(bool v) : value(v) {}
    Value eval(Env&) const override { return Value::Bool(value); }
    bool value;
};

struct NullExpr final : Expr {
    Value eval(Env&) const override { return Value::Null(); }
};

struct VarExpr final : Expr {
    explicit VarExpr(std::string n) : name(std::move(n)) {}
    Value eval(Env& env) const override { return env.get_var(name); }
    std::string name;
};

struct ArrayLiteralExpr final : Expr {
    explicit ArrayLiteralExpr(std::vector<std::unique_ptr<Expr>> elements)
        : elements_(std::move(elements)) {}

    Value eval(Env& env) const override {
        std::vector<Value> out;
        out.reserve(elements_.size());
        for (const auto& expr : elements_) {
            out.push_back(expr->eval(env));
        }
        return Value::Array(std::move(out));
    }

    std::vector<std::unique_ptr<Expr>> elements_;
};

struct ArrayAccessExpr final : Expr {
    ArrayAccessExpr(std::unique_ptr<Expr> receiver, std::unique_ptr<Expr> index)
        : receiver_(std::move(receiver)), index_(std::move(index)) {}

    Value eval(Env& env) const override {
        auto receiver = receiver_->eval(env);
        auto index = index_->eval(env).as_int("Array index");
        if (receiver.kind != Value::Kind::Array || !receiver.array) {
            throw std::runtime_error("Array access expects array receiver.");
        }
        if (index < 0 || static_cast<std::size_t>(index) >= receiver.array->size()) {
            return Value::Null();
        }
        return (*receiver.array)[static_cast<std::size_t>(index)];
    }

    std::unique_ptr<Expr> receiver_;
    std::unique_ptr<Expr> index_;
};

struct MemberAccessExpr final : Expr {
    MemberAccessExpr(std::unique_ptr<Expr> receiver, std::string member)
        : receiver_(std::move(receiver)), member_(std::move(member)) {}

    Value eval(Env& env) const override {
        auto receiver = receiver_->eval(env);
        if (receiver.kind == Value::Kind::Object
            && receiver.class_name == "__ui_ref"
            && receiver.object_fields) {
            const auto id_it = receiver.object_fields->find("id");
            if (id_it == receiver.object_fields->end() || id_it->second.kind != Value::Kind::String) {
                throw std::runtime_error("ui ref missing id.");
            }
            if (g_ui_get_prop == nullptr) {
                throw std::runtime_error("ui bridge unavailable.");
            }
            char out_json[2048] = {0};
            char error[1024] = {0};
            const auto rc = g_ui_get_prop(
                id_it->second.string_value.c_str(),
                member_.c_str(),
                out_json,
                static_cast<int>(sizeof(out_json)),
                error,
                static_cast<int>(sizeof(error)));
            if (rc != 0) {
                throw std::runtime_error(error[0] != '\0' ? error : "ui get failed");
            }
            return parse_json_scalar(out_json);
        }

        if (receiver.kind == Value::Kind::Array && member_ == "size" && receiver.array) {
            return Value::Int(static_cast<std::int64_t>(receiver.array->size()));
        }

        if (receiver.kind != Value::Kind::Object || !receiver.object_fields) {
            throw std::runtime_error("Member access expects object receiver.");
        }

        const auto it = receiver.object_fields->find(member_);
        if (it == receiver.object_fields->end()) {
            return Value::Null();
        }
        return it->second;
    }

    std::unique_ptr<Expr> receiver_;
    std::string member_;
};

struct MethodCallExpr final : Expr {
    MethodCallExpr(std::unique_ptr<Expr> receiver, std::string method, std::vector<std::unique_ptr<Expr>> args)
        : receiver_(std::move(receiver)), method_(std::move(method)), args_(std::move(args)) {}

    Value eval(Env& env) const override {
        auto receiver = receiver_->eval(env);
        std::vector<Value> args;
        args.reserve(args_.size());
        for (const auto& arg : args_) {
            args.push_back(arg->eval(env));
        }

        if (receiver.kind == Value::Kind::Object && receiver.class_name == "log") {
            if (method_ == "info" || method_ == "success" || method_ == "warning" || method_ == "error" || method_ == "debug") {
                if (!args.empty()) {
                    std::cout << "[" << method_ << "] " << value_to_string(args[0]) << std::endl;
                } else {
                    std::cout << "[" << method_ << "]" << std::endl;
                }
                return Value::Null();
            }
            throw std::runtime_error("Unknown log method: " + method_);
        }

        if (receiver.kind == Value::Kind::Object && receiver.class_name == "os") {
            if (method_ == "now" && args.empty()) {
                const auto now = std::chrono::time_point_cast<std::chrono::milliseconds>(std::chrono::system_clock::now());
                const auto epoch_ms = now.time_since_epoch().count();
                return Value::Int(static_cast<std::int64_t>(epoch_ms));
            }

            if (method_ == "fileExists" && args.size() == 1) {
                const auto path = value_to_string(args[0]);
                if (path.empty()) {
                    return Value::Int(0);
                }
                const auto exists = std::filesystem::exists(std::filesystem::path(path));
                return Value::Int(exists ? 1 : 0);
            }

            if (method_ == "getEnv" && args.size() == 1) {
                const auto key = value_to_string(args[0]);
                if (key.empty()) {
                    return Value::Null();
                }
                const char* value = std::getenv(key.c_str());
                if (value == nullptr) {
                    return Value::Null();
                }
                return Value::String(value);
            }

            if (g_ui_invoke == nullptr) {
                throw std::runtime_error("ui invoke bridge unavailable.");
            }

            std::string args_json = "[";
            for (std::size_t i = 0; i < args.size(); i++) {
                if (i > 0) {
                    args_json.push_back(',');
                }
                args_json += value_to_json(args[i]);
            }
            args_json.push_back(']');

            char out_json[4096] = {0};
            char error[1024] = {0};
            const auto rc = g_ui_invoke(
                "__os__",
                method_.c_str(),
                args_json.c_str(),
                out_json,
                static_cast<int>(sizeof(out_json)),
                error,
                static_cast<int>(sizeof(error)));
            if (rc != 0) {
                throw std::runtime_error(error[0] != '\0' ? error : ("Unknown os method: " + method_));
            }

            return parse_json_value(out_json);
        }

        if (receiver.kind == Value::Kind::Object && receiver.class_name == "ui") {
            if (method_ == "getObject" && args.size() == 1) {
                if (g_ui_get_prop == nullptr) {
                    throw std::runtime_error("ui bridge unavailable.");
                }
                const auto id = value_to_string(args[0]);
                char out_json[256] = {0};
                char error[1024] = {0};
                const auto rc = g_ui_get_prop(
                    id.c_str(),
                    "__exists",
                    out_json,
                    static_cast<int>(sizeof(out_json)),
                    error,
                    static_cast<int>(sizeof(error)));
                if (rc != 0) {
                    throw std::runtime_error(error[0] != '\0' ? error : "ui exists probe failed");
                }
                const auto exists = parse_json_scalar(out_json).truthy();
                if (!exists) {
                    return Value::Null();
                }
                std::unordered_map<std::string, Value> fields;
                fields.emplace("id", Value::String(id));
                return Value::Object("__ui_ref", std::move(fields));
            }

            if (g_ui_invoke == nullptr) {
                throw std::runtime_error("ui invoke bridge unavailable.");
            }

            std::string args_json = "[";
            for (std::size_t i = 0; i < args.size(); i++) {
                if (i > 0) {
                    args_json.push_back(',');
                }
                args_json += value_to_json(args[i]);
            }
            args_json.push_back(']');

            char out_json[4096] = {0};
            char error[1024] = {0};
            const auto rc = g_ui_invoke(
                "__ui__",
                method_.c_str(),
                args_json.c_str(),
                out_json,
                static_cast<int>(sizeof(out_json)),
                error,
                static_cast<int>(sizeof(error)));
            if (rc != 0) {
                throw std::runtime_error(error[0] != '\0' ? error : ("Unknown ui method: " + method_));
            }

            return parse_json_value(out_json);
        }

        if (receiver.kind == Value::Kind::Object
            && receiver.class_name == "__ui_ref"
            && receiver.object_fields) {
            const auto id_it = receiver.object_fields->find("id");
            if (id_it == receiver.object_fields->end() || id_it->second.kind != Value::Kind::String) {
                throw std::runtime_error("ui ref missing id.");
            }
            if (g_ui_invoke == nullptr) {
                throw std::runtime_error("ui invoke bridge unavailable.");
            }

            std::string args_json = "[";
            for (std::size_t i = 0; i < args.size(); i++) {
                if (i > 0) {
                    args_json.push_back(',');
                }
                args_json += value_to_json(args[i]);
            }
            args_json.push_back(']');

            char out_json[4096] = {0};
            char error[1024] = {0};
            const auto rc = g_ui_invoke(
                id_it->second.string_value.c_str(),
                method_.c_str(),
                args_json.c_str(),
                out_json,
                static_cast<int>(sizeof(out_json)),
                error,
                static_cast<int>(sizeof(error)));
            if (rc != 0) {
                throw std::runtime_error(error[0] != '\0' ? error : "ui invoke failed");
            }

            return parse_json_value(out_json);
        }

        if (receiver.kind == Value::Kind::Array && receiver.array) {
            if (method_ == "add" && args.size() == 1) {
                receiver.array->push_back(args[0]);
                return Value::Null();
            }

            if (method_ == "remove" && args.size() == 1) {
                const auto it = std::find(receiver.array->begin(), receiver.array->end(), args[0]);
                if (it == receiver.array->end()) {
                    return Value::Int(0);
                }
                receiver.array->erase(it);
                return Value::Int(1);
            }

            if (method_ == "removeAt" && args.size() == 1) {
                const auto index = args[0].as_int("removeAt index");
                if (index < 0 || static_cast<std::size_t>(index) >= receiver.array->size()) {
                    return Value::Null();
                }
                auto value = (*receiver.array)[static_cast<std::size_t>(index)];
                receiver.array->erase(receiver.array->begin() + static_cast<std::ptrdiff_t>(index));
                return value;
            }

            if (method_ == "contains" && args.size() == 1) {
                const auto it = std::find(receiver.array->begin(), receiver.array->end(), args[0]);
                return Value::Int(it != receiver.array->end() ? 1 : 0);
            }

            throw std::runtime_error("Unknown array method: " + method_);
        }

        throw std::runtime_error("Method call expects supported receiver.");
    }

    std::unique_ptr<Expr> receiver_;
    std::string method_;
    std::vector<std::unique_ptr<Expr>> args_;
};

struct BinaryExpr final : Expr {
    BinaryExpr(TokenType op, std::unique_ptr<Expr> left, std::unique_ptr<Expr> right)
        : oper(op), left_(std::move(left)), right_(std::move(right)) {}

    Value eval(Env& env) const override {
        const auto left_value = left_->eval(env);
        const auto right_value = right_->eval(env);
        switch (oper) {
            case TokenType::Plus:
                if (left_value.kind == Value::Kind::String || right_value.kind == Value::Kind::String) {
                    return Value::String(value_to_string(left_value) + value_to_string(right_value));
                }
                return Value::Int(left_value.as_int("Binary expression") + right_value.as_int("Binary expression"));
            case TokenType::Minus:
                return Value::Int(left_value.as_int("Binary expression") - right_value.as_int("Binary expression"));
            case TokenType::Star:
                return Value::Int(left_value.as_int("Binary expression") * right_value.as_int("Binary expression"));
            case TokenType::Slash:
                if (right_value.as_int("Binary expression") == 0) {
                    throw std::runtime_error("Division by zero.");
                }
                return Value::Int(left_value.as_int("Binary expression") / right_value.as_int("Binary expression"));
            case TokenType::Percent:
                if (right_value.as_int("Binary expression") == 0) {
                    throw std::runtime_error("Modulo by zero.");
                }
                return Value::Int(left_value.as_int("Binary expression") % right_value.as_int("Binary expression"));
            case TokenType::Less:
                return Value::Int(left_value.as_int("Binary expression") < right_value.as_int("Binary expression") ? 1 : 0);
            case TokenType::LessEqual:
                return Value::Int(left_value.as_int("Binary expression") <= right_value.as_int("Binary expression") ? 1 : 0);
            case TokenType::Greater:
                return Value::Int(left_value.as_int("Binary expression") > right_value.as_int("Binary expression") ? 1 : 0);
            case TokenType::GreaterEqual:
                return Value::Int(left_value.as_int("Binary expression") >= right_value.as_int("Binary expression") ? 1 : 0);
            case TokenType::EqualEqual:
                return Value::Int(left_value == right_value ? 1 : 0);
            case TokenType::BangEqual:
                return Value::Int(left_value == right_value ? 0 : 1);
            default:
                throw std::runtime_error("Unsupported operator.");
        }
    }

    TokenType oper;
    std::unique_ptr<Expr> left_;
    std::unique_ptr<Expr> right_;
};

struct LogicalExpr final : Expr {
    LogicalExpr(TokenType op, std::unique_ptr<Expr> left, std::unique_ptr<Expr> right)
        : oper(op), left_(std::move(left)), right_(std::move(right)) {}

    Value eval(Env& env) const override {
        const auto left = left_->eval(env);
        const auto right = right_->eval(env);
        if (oper == TokenType::AndAnd) {
            return Value::Int(left.truthy() && right.truthy() ? 1 : 0);
        }

        if (oper == TokenType::OrOr) {
            return Value::Int(left.truthy() || right.truthy() ? 1 : 0);
        }

        throw std::runtime_error("Unsupported logical operator.");
    }

    TokenType oper;
    std::unique_ptr<Expr> left_;
    std::unique_ptr<Expr> right_;
};

struct UnaryExpr final : Expr {
    UnaryExpr(TokenType op, std::unique_ptr<Expr> operand)
        : oper(op), operand_(std::move(operand)) {}

    Value eval(Env& env) const override {
        const auto value = operand_->eval(env);
        if (oper == TokenType::Bang) {
            return Value::Int(value.truthy() ? 0 : 1);
        }
        if (oper == TokenType::Minus) {
            return Value::Int(-value.as_int("Unary '-'"));
        }
        throw std::runtime_error("Unsupported unary operator.");
    }

    TokenType oper;
    std::unique_ptr<Expr> operand_;
};

struct PostfixExpr final : Expr {
    PostfixExpr(TokenType op, std::unique_ptr<Expr> operand)
        : oper(op), operand_(std::move(operand)) {}

    Value eval(Env& env) const override {
        const auto* variable = dynamic_cast<const VarExpr*>(operand_.get());
        if (variable == nullptr) {
            throw std::runtime_error("Postfix operators only work on variables.");
        }

        auto current = env.get_var(variable->name);
        if (current.kind != Value::Kind::Int) {
            throw std::runtime_error("Postfix operators only work on integer variables.");
        }

        Value next = current;
        if (oper == TokenType::Increment) {
            next.int_value = next.int_value + 1;
        } else if (oper == TokenType::Decrement) {
            next.int_value = next.int_value - 1;
        } else {
            throw std::runtime_error("Unsupported postfix operator.");
        }

        if (!env.assign_var(variable->name, next)) {
            throw std::runtime_error("Assignment to unknown variable: " + variable->name);
        }

        // postfix returns old value
        return current;
    }

    TokenType oper;
    std::unique_ptr<Expr> operand_;
};

struct WhenBranch {
    std::unique_ptr<Expr> condition;
    std::unique_ptr<Expr> result;
    bool is_else = false;
};

struct WhenExpr final : Expr {
    explicit WhenExpr(std::unique_ptr<Expr> subject, std::vector<WhenBranch> branches)
        : subject_(std::move(subject)), branches_(std::move(branches)) {}

    Value eval(Env& env) const override {
        const auto subject = subject_ ? subject_->eval(env) : Value::Null();
        const auto has_subject = static_cast<bool>(subject_);
        for (const auto& branch : branches_) {
            if (branch.is_else) {
                return branch.result->eval(env);
            }

            if (!branch.condition) {
                throw std::runtime_error("Invalid when branch.");
            }

            const auto condition = branch.condition->eval(env);
            const auto matches = has_subject ? (condition == subject) : condition.truthy();
            if (matches) {
                return branch.result->eval(env);
            }
        }
        return Value::Null();
    }

    std::unique_ptr<Expr> subject_;
    std::vector<WhenBranch> branches_;
};

struct Stmt {
    virtual ~Stmt() = default;
    virtual void execute(Env& env, Value& last) const = 0;
};

static void execute_statements(const std::vector<std::unique_ptr<Stmt>>& body, Env& env, Value& last) {
    for (const auto& stmt : body) {
        stmt->execute(env, last);
    }
}

struct FunctionDeclStmt final : Stmt {
    std::string name;
    std::vector<std::string> params;
    std::vector<std::unique_ptr<Stmt>> body;

    void execute(Env& env, Value&) const override {
        env.define_function(name, this);
    }
};

struct DataClassDeclStmt final : Stmt {
    std::string name;
    std::vector<std::string> fields;

    void execute(Env& env, Value&) const override {
        env.define_data_class(name, this);
    }
};

struct EventHandlerDeclStmt final : Stmt {
    std::string target_id;
    std::string event_name;
    std::vector<std::string> params;
    std::vector<std::unique_ptr<Stmt>> body;

    std::string key() const {
        return target_id + "." + event_name;
    }

    void execute(Env& env, Value&) const override {
        env.define_event_handler(key(), this);
    }
};

struct CallExpr final : Expr {
    CallExpr(std::string n, std::vector<std::unique_ptr<Expr>> args)
        : name(std::move(n)), args_(std::move(args)) {}

    Value eval(Env& env) const override {
        if (name == "size") {
            if (args_.size() != 1) {
                throw std::runtime_error("size expects 1 argument.");
            }
            auto value = args_[0]->eval(env);
            if (value.kind != Value::Kind::Array || !value.array) {
                return Value::Int(0);
            }
            return Value::Int(static_cast<std::int64_t>(value.array->size()));
        }

        const auto* fn = env.get_function(name);
        if (fn == nullptr) {
            const auto* data_class = env.get_data_class(name);
            if (data_class == nullptr) {
                throw std::runtime_error("Unknown function: " + name);
            }

            if (args_.size() != data_class->fields.size()) {
                throw std::runtime_error("Data class '" + name + "' expects "
                    + std::to_string(data_class->fields.size()) + " arg(s), got "
                    + std::to_string(args_.size()) + ".");
            }

            std::unordered_map<std::string, Value> fields;
            fields.reserve(data_class->fields.size());
            for (std::size_t i = 0; i < args_.size(); i++) {
                fields[data_class->fields[i]] = args_[i]->eval(env);
            }
            return Value::Object(data_class->name, std::move(fields));
        }

        if (args_.size() != fn->params.size()) {
            throw std::runtime_error("Function '" + name + "' expects "
                + std::to_string(fn->params.size()) + " arg(s), got "
                + std::to_string(args_.size()) + ".");
        }

        Env local(&env);
        for (std::size_t i = 0; i < args_.size(); i++) {
            local.define_var(fn->params[i], args_[i]->eval(env));
        }

        Value last = Value::Null();
        try {
            execute_statements(fn->body, local, last);
            return last;
        } catch (const ReturnSignal& ret) {
            return ret.value;
        }
    }

    std::string name;
    std::vector<std::unique_ptr<Expr>> args_;
};

struct VarDeclStmt final : Stmt {
    VarDeclStmt(std::string n, std::unique_ptr<Expr> v) : name(std::move(n)), value(std::move(v)) {}
    void execute(Env& env, Value&) const override {
        env.define_var(name, value->eval(env));
    }

    std::string name;
    std::unique_ptr<Expr> value;
};

struct AssignStmt final : Stmt {
    AssignStmt(std::unique_ptr<Expr> target, std::unique_ptr<Expr> value)
        : target_expr(std::move(target)), value_expr(std::move(value)) {}

    void execute(Env& env, Value&) const override {
        const auto value = value_expr->eval(env);
        if (const auto* variable = dynamic_cast<const VarExpr*>(target_expr.get())) {
            if (!env.assign_var(variable->name, value)) {
                throw std::runtime_error("Assignment to unknown variable: " + variable->name);
            }
            return;
        }

        if (const auto* member = dynamic_cast<const MemberAccessExpr*>(target_expr.get())) {
            auto receiver = member->receiver_->eval(env);
            if (receiver.kind == Value::Kind::Object
                && receiver.class_name == "__ui_ref"
                && receiver.object_fields) {
                const auto id_it = receiver.object_fields->find("id");
                if (id_it == receiver.object_fields->end() || id_it->second.kind != Value::Kind::String) {
                    throw std::runtime_error("ui ref missing id.");
                }
                if (g_ui_set_prop == nullptr) {
                    throw std::runtime_error("ui bridge unavailable.");
                }
                char error[1024] = {0};
                const auto payload = value_to_json(value);
                const auto rc = g_ui_set_prop(
                    id_it->second.string_value.c_str(),
                    member->member_.c_str(),
                    payload.c_str(),
                    error,
                    static_cast<int>(sizeof(error)));
                if (rc != 0) {
                    throw std::runtime_error(error[0] != '\0' ? error : "ui set failed");
                }
                return;
            }
            if (receiver.kind != Value::Kind::Object || !receiver.object_fields) {
                throw std::runtime_error("Member assignment expects object receiver.");
            }
            (*receiver.object_fields)[member->member_] = value;
            return;
        }

        if (const auto* array_access = dynamic_cast<const ArrayAccessExpr*>(target_expr.get())) {
            auto receiver = array_access->receiver_->eval(env);
            if (receiver.kind != Value::Kind::Array || !receiver.array) {
                throw std::runtime_error("Array assignment expects array receiver.");
            }

            const auto idx = array_access->index_->eval(env).as_int("Array assignment index");
            if (idx < 0) {
                throw std::runtime_error("Array assignment index must be >= 0.");
            }

            const auto target = static_cast<std::size_t>(idx);
            if (target >= receiver.array->size()) {
                receiver.array->resize(target + 1, Value::Null());
            }

            (*receiver.array)[target] = value;
            return;
        }

        throw std::runtime_error("Invalid assignment target.");
    }

    std::unique_ptr<Expr> target_expr;
    std::unique_ptr<Expr> value_expr;
};

struct ReturnStmt final : Stmt {
    explicit ReturnStmt(std::unique_ptr<Expr> v) : value(std::move(v)) {}
    void execute(Env& env, Value&) const override {
        throw ReturnSignal(value ? value->eval(env) : Value::Null());
    }

    std::unique_ptr<Expr> value;
};

struct BreakStmt final : Stmt {
    void execute(Env&, Value&) const override {
        throw BreakSignal();
    }
};

struct ContinueStmt final : Stmt {
    void execute(Env&, Value&) const override {
        throw ContinueSignal();
    }
};

struct ExprStmt final : Stmt {
    explicit ExprStmt(std::unique_ptr<Expr> v) : value(std::move(v)) {}
    void execute(Env& env, Value& last) const override {
        last = value->eval(env);
    }

    std::unique_ptr<Expr> value;
};

struct ForStmt final : Stmt {
    std::unique_ptr<Stmt> init;
    std::unique_ptr<Expr> condition;
    std::unique_ptr<Stmt> update;
    std::vector<std::unique_ptr<Stmt>> body;

    void execute(Env& env, Value& last) const override {
        init->execute(env, last);
        while (condition->eval(env).truthy()) {
            try {
                execute_statements(body, env, last);
            } catch (const ContinueSignal&) {
                update->execute(env, last);
                continue;
            } catch (const BreakSignal&) {
                break;
            }
            update->execute(env, last);
        }
    }
};

struct ForInStmt final : Stmt {
    std::string variable;
    std::unique_ptr<Expr> iterable;
    std::vector<std::unique_ptr<Stmt>> body;

    void execute(Env& env, Value& last) const override {
        auto value = iterable->eval(env);
        if (value.kind != Value::Kind::Array || !value.array) {
            throw std::runtime_error("for-in requires an array");
        }

        for (const auto& element : *value.array) {
            if (!env.assign_var(variable, element)) {
                env.define_var(variable, element);
            }
            try {
                execute_statements(body, env, last);
            } catch (const ContinueSignal&) {
                continue;
            } catch (const BreakSignal&) {
                break;
            }
        }
    }
};

struct WhileStmt final : Stmt {
    std::unique_ptr<Expr> condition;
    std::vector<std::unique_ptr<Stmt>> body;

    void execute(Env& env, Value& last) const override {
        while (condition->eval(env).truthy()) {
            try {
                execute_statements(body, env, last);
            } catch (const ContinueSignal&) {
                continue;
            } catch (const BreakSignal&) {
                break;
            }
        }
    }
};

struct IfStmt final : Stmt {
    std::unique_ptr<Expr> condition;
    std::vector<std::unique_ptr<Stmt>> then_body;
    std::vector<std::unique_ptr<Stmt>> else_body;

    void execute(Env& env, Value& last) const override {
        if (condition->eval(env).truthy()) {
            execute_statements(then_body, env, last);
        } else {
            execute_statements(else_body, env, last);
        }
    }
};

class Parser {
public:
    explicit Parser(std::vector<Token> tokens) : tokens_(std::move(tokens)) {}

    std::vector<std::unique_ptr<Stmt>> parse_program() {
        std::vector<std::unique_ptr<Stmt>> out;
        while (!check(TokenType::Eof)) {
            out.push_back(parse_statement());
            match(TokenType::Semicolon);
        }
        return out;
    }

private:
    std::unique_ptr<Stmt> parse_statement() {
        if (match(TokenType::Var)) return parse_var_decl(false);
        if (match(TokenType::Data)) return parse_data_class_decl();
        if (match(TokenType::For)) return parse_for();
        if (match(TokenType::While)) return parse_while();
        if (match(TokenType::If)) return parse_if();
        if (match(TokenType::Fun)) return parse_function_decl();
        if (match(TokenType::On)) return parse_event_handler_decl();
        if (match(TokenType::Break)) return parse_break();
        if (match(TokenType::Continue)) return parse_continue();
        if (match(TokenType::Return)) return parse_return();

        if (check(TokenType::Identifier)) {
            const auto checkpoint = current_;
            try {
                return parse_assignment(false);
            } catch (const std::runtime_error&) {
                current_ = checkpoint;
            }
        }

        return parse_expr_stmt();
    }

    std::unique_ptr<Stmt> parse_var_decl(bool inside_for_clause) {
        const auto name = consume(TokenType::Identifier, "Expected variable name").text;
        consume(TokenType::Assign, "Expected '=' after variable name");
        auto value = parse_expression();
        if (!inside_for_clause) {
            match(TokenType::Semicolon);
        }
        return std::make_unique<VarDeclStmt>(name, std::move(value));
    }

    std::unique_ptr<Stmt> parse_data_class_decl() {
        consume(TokenType::Class, "Expected 'class' after 'data'");
        const auto name = consume(TokenType::Identifier, "Expected data class name").text;
        consume(TokenType::LeftParen, "Expected '(' after data class name");

        std::vector<std::string> fields;
        if (!check(TokenType::RightParen)) {
            do {
                fields.push_back(consume(TokenType::Identifier, "Expected field name").text);
            } while (match(TokenType::Comma));
        }

        consume(TokenType::RightParen, "Expected ')' after data class fields");
        match(TokenType::Semicolon);

        auto data = std::make_unique<DataClassDeclStmt>();
        data->name = name;
        data->fields = std::move(fields);
        return data;
    }

    std::unique_ptr<Stmt> parse_assignment(bool inside_for_clause) {
        auto target = parse_assignment_target();

        consume(TokenType::Assign, "Expected '=' in assignment");
        auto value = parse_expression();
        if (!inside_for_clause) {
            match(TokenType::Semicolon);
        }
        return std::make_unique<AssignStmt>(std::move(target), std::move(value));
    }

    std::unique_ptr<Expr> parse_assignment_target() {
        std::unique_ptr<Expr> target =
            std::make_unique<VarExpr>(consume(TokenType::Identifier, "Expected assignment target").text);

        while (true) {
            if (match(TokenType::Dot)) {
                const auto member = consume(TokenType::Identifier, "Expected property name after '.'").text;
                if (match(TokenType::LeftParen)) {
                    auto args = parse_arguments();
                    target = std::make_unique<MethodCallExpr>(std::move(target), member, std::move(args));
                } else {
                    target = std::make_unique<MemberAccessExpr>(std::move(target), member);
                }
                continue;
            }

            if (match(TokenType::LeftBracket)) {
                auto index = parse_expression();
                consume(TokenType::RightBracket, "Expected ']' after assignment index");
                target = std::make_unique<ArrayAccessExpr>(std::move(target), std::move(index));
                continue;
            }

            break;
        }

        if (!dynamic_cast<VarExpr*>(target.get())
            && !dynamic_cast<MemberAccessExpr*>(target.get())
            && !dynamic_cast<ArrayAccessExpr*>(target.get())) {
            throw std::runtime_error("Invalid assignment target.");
        }

        return target;
    }

    std::unique_ptr<Stmt> parse_expr_stmt() {
        auto value = parse_expression();
        match(TokenType::Semicolon);
        return std::make_unique<ExprStmt>(std::move(value));
    }

    std::unique_ptr<Stmt> parse_return() {
        std::unique_ptr<Expr> value;
        if (!check(TokenType::Semicolon)
            && !check(TokenType::RightBrace)
            && !check(TokenType::Eof)) {
            value = parse_expression();
        }
        match(TokenType::Semicolon);
        return std::make_unique<ReturnStmt>(std::move(value));
    }

    std::unique_ptr<Stmt> parse_break() {
        match(TokenType::Semicolon);
        return std::make_unique<BreakStmt>();
    }

    std::unique_ptr<Stmt> parse_continue() {
        match(TokenType::Semicolon);
        return std::make_unique<ContinueStmt>();
    }

    std::unique_ptr<Stmt> parse_function_decl() {
        const auto name = consume(TokenType::Identifier, "Expected function name").text;
        consume(TokenType::LeftParen, "Expected '(' after function name");

        std::vector<std::string> params;
        if (!check(TokenType::RightParen)) {
            do {
                params.push_back(consume(TokenType::Identifier, "Expected parameter name").text);
            } while (match(TokenType::Comma));
        }

        consume(TokenType::RightParen, "Expected ')' after function parameters");
        auto body = parse_block();

        auto fn = std::make_unique<FunctionDeclStmt>();
        fn->name = name;
        fn->params = std::move(params);
        fn->body = std::move(body);
        return fn;
    }

    std::unique_ptr<Stmt> parse_event_handler_decl() {
        const auto target_id = consume(TokenType::Identifier, "Expected target id after on").text;
        consume(TokenType::Dot, "Expected '.' after event target id");
        const auto event_name = consume(TokenType::Identifier, "Expected event name after '.'").text;
        consume(TokenType::LeftParen, "Expected '(' after event name");

        std::vector<std::string> params;
        if (!check(TokenType::RightParen)) {
            do {
                params.push_back(consume(TokenType::Identifier, "Expected parameter name").text);
            } while (match(TokenType::Comma));
        }
        consume(TokenType::RightParen, "Expected ')' after event parameters");
        auto body = parse_block();

        auto handler = std::make_unique<EventHandlerDeclStmt>();
        handler->target_id = target_id;
        handler->event_name = event_name;
        handler->params = std::move(params);
        handler->body = std::move(body);
        return handler;
    }

    std::unique_ptr<Stmt> parse_for() {
        consume(TokenType::LeftParen, "Expected '(' after for");

        if (check(TokenType::Identifier)) {
            const auto checkpoint = current_;
            const auto variable = advance().text;
            if (match(TokenType::In)) {
                auto iterable = parse_expression();
                consume(TokenType::RightParen, "Expected ')' after for-in");
                auto body = parse_block();

                auto stmt = std::make_unique<ForInStmt>();
                stmt->variable = variable;
                stmt->iterable = std::move(iterable);
                stmt->body = std::move(body);
                return stmt;
            }
            current_ = checkpoint;
        }

        std::unique_ptr<Stmt> init;
        if (match(TokenType::Var)) {
            init = parse_var_decl(true);
        } else {
            init = parse_assignment(true);
        }

        consume(TokenType::Semicolon, "Expected ';' after for init");
        auto condition = parse_expression();
        consume(TokenType::Semicolon, "Expected ';' after for condition");
        auto update = parse_assignment(true);
        consume(TokenType::RightParen, "Expected ')' after for clauses");

        auto body = parse_block();

        auto for_stmt = std::make_unique<ForStmt>();
        for_stmt->init = std::move(init);
        for_stmt->condition = std::move(condition);
        for_stmt->update = std::move(update);
        for_stmt->body = std::move(body);
        return for_stmt;
    }

    std::unique_ptr<Stmt> parse_while() {
        consume(TokenType::LeftParen, "Expected '(' after while");
        auto condition = parse_expression();
        consume(TokenType::RightParen, "Expected ')' after while condition");
        auto body = parse_block();

        auto while_stmt = std::make_unique<WhileStmt>();
        while_stmt->condition = std::move(condition);
        while_stmt->body = std::move(body);
        return while_stmt;
    }

    std::unique_ptr<Stmt> parse_if() {
        consume(TokenType::LeftParen, "Expected '(' after if");
        auto condition = parse_expression();
        consume(TokenType::RightParen, "Expected ')' after if condition");

        auto then_body = parse_block();
        std::vector<std::unique_ptr<Stmt>> else_body;
        if (match(TokenType::Else)) {
            if (match(TokenType::If)) {
                // Preserve else-if semantics by nesting the chained if in else_body.
                else_body.push_back(parse_if());
            } else {
                else_body = parse_block();
            }
        }

        auto if_stmt = std::make_unique<IfStmt>();
        if_stmt->condition = std::move(condition);
        if_stmt->then_body = std::move(then_body);
        if_stmt->else_body = std::move(else_body);
        return if_stmt;
    }

    std::vector<std::unique_ptr<Stmt>> parse_block() {
        consume(TokenType::LeftBrace, "Expected '{' before block");
        std::vector<std::unique_ptr<Stmt>> body;
        while (!check(TokenType::RightBrace) && !check(TokenType::Eof)) {
            body.push_back(parse_statement());
            match(TokenType::Semicolon);
        }
        consume(TokenType::RightBrace, "Expected '}' after block");
        return body;
    }

    std::unique_ptr<Expr> parse_expression() { return parse_or(); }

    std::unique_ptr<Expr> parse_or() {
        auto expr = parse_and();
        while (match(TokenType::OrOr)) {
            auto rhs = parse_and();
            expr = std::make_unique<LogicalExpr>(TokenType::OrOr, std::move(expr), std::move(rhs));
        }
        return expr;
    }

    std::unique_ptr<Expr> parse_and() {
        auto expr = parse_equality();
        while (match(TokenType::AndAnd)) {
            auto rhs = parse_equality();
            expr = std::make_unique<LogicalExpr>(TokenType::AndAnd, std::move(expr), std::move(rhs));
        }
        return expr;
    }

    std::unique_ptr<Expr> parse_equality() {
        auto expr = parse_comparison();
        while (true) {
            if (match(TokenType::EqualEqual)) {
                auto rhs = parse_comparison();
                expr = std::make_unique<BinaryExpr>(TokenType::EqualEqual, std::move(expr), std::move(rhs));
                continue;
            }
            if (match(TokenType::BangEqual)) {
                auto rhs = parse_comparison();
                expr = std::make_unique<BinaryExpr>(TokenType::BangEqual, std::move(expr), std::move(rhs));
                continue;
            }
            break;
        }
        return expr;
    }

    std::unique_ptr<Expr> parse_comparison() {
        auto expr = parse_term();
        while (true) {
            if (match(TokenType::Less)) {
                auto rhs = parse_term();
                expr = std::make_unique<BinaryExpr>(TokenType::Less, std::move(expr), std::move(rhs));
                continue;
            }
            if (match(TokenType::LessEqual)) {
                auto rhs = parse_term();
                expr = std::make_unique<BinaryExpr>(TokenType::LessEqual, std::move(expr), std::move(rhs));
                continue;
            }
            if (match(TokenType::Greater)) {
                auto rhs = parse_term();
                expr = std::make_unique<BinaryExpr>(TokenType::Greater, std::move(expr), std::move(rhs));
                continue;
            }
            if (match(TokenType::GreaterEqual)) {
                auto rhs = parse_term();
                expr = std::make_unique<BinaryExpr>(TokenType::GreaterEqual, std::move(expr), std::move(rhs));
                continue;
            }
            break;
        }
        return expr;
    }

    std::unique_ptr<Expr> parse_term() {
        auto expr = parse_factor();
        while (true) {
            if (match(TokenType::Plus)) {
                auto rhs = parse_factor();
                expr = std::make_unique<BinaryExpr>(TokenType::Plus, std::move(expr), std::move(rhs));
                continue;
            }
            if (match(TokenType::Minus)) {
                auto rhs = parse_factor();
                expr = std::make_unique<BinaryExpr>(TokenType::Minus, std::move(expr), std::move(rhs));
                continue;
            }
            break;
        }
        return expr;
    }

    std::unique_ptr<Expr> parse_factor() {
        auto expr = parse_unary();
        while (true) {
            if (match(TokenType::Star)) {
                auto rhs = parse_unary();
                expr = std::make_unique<BinaryExpr>(TokenType::Star, std::move(expr), std::move(rhs));
                continue;
            }
            if (match(TokenType::Slash)) {
                auto rhs = parse_unary();
                expr = std::make_unique<BinaryExpr>(TokenType::Slash, std::move(expr), std::move(rhs));
                continue;
            }
            if (match(TokenType::Percent)) {
                auto rhs = parse_unary();
                expr = std::make_unique<BinaryExpr>(TokenType::Percent, std::move(expr), std::move(rhs));
                continue;
            }
            break;
        }
        return expr;
    }

    std::unique_ptr<Expr> parse_unary() {
        if (match(TokenType::Bang)) {
            return std::make_unique<UnaryExpr>(TokenType::Bang, parse_unary());
        }
        if (match(TokenType::Minus)) {
            return std::make_unique<UnaryExpr>(TokenType::Minus, parse_unary());
        }
        return parse_postfix();
    }

    std::unique_ptr<Expr> parse_postfix() {
        auto expr = parse_primary();

        while (true) {
            if (match(TokenType::LeftParen)) {
                auto args = parse_arguments();
                if (auto* var = dynamic_cast<VarExpr*>(expr.get())) {
                    expr = std::make_unique<CallExpr>(var->name, std::move(args));
                } else {
                    throw std::runtime_error("Invalid call target.");
                }
                continue;
            }

            if (match(TokenType::Dot)) {
                const auto member = consume(TokenType::Identifier, "Expected property name after '.'").text;
                if (match(TokenType::LeftParen)) {
                    auto args = parse_arguments();
                    expr = std::make_unique<MethodCallExpr>(std::move(expr), member, std::move(args));
                } else {
                    expr = std::make_unique<MemberAccessExpr>(std::move(expr), member);
                }
                continue;
            }

            if (match(TokenType::LeftBracket)) {
                auto index = parse_expression();
                consume(TokenType::RightBracket, "Expected ']' after array index");
                expr = std::make_unique<ArrayAccessExpr>(std::move(expr), std::move(index));
                continue;
            }

            break;
        }

        if (match(TokenType::Increment)) {
            return std::make_unique<PostfixExpr>(TokenType::Increment, std::move(expr));
        }
        if (match(TokenType::Decrement)) {
            return std::make_unique<PostfixExpr>(TokenType::Decrement, std::move(expr));
        }

        return expr;
    }

    std::vector<std::unique_ptr<Expr>> parse_arguments() {
        std::vector<std::unique_ptr<Expr>> args;
        if (!check(TokenType::RightParen)) {
            do {
                args.push_back(parse_expression());
            } while (match(TokenType::Comma));
        }
        consume(TokenType::RightParen, "Expected ')' after arguments");
        return args;
    }

    std::unique_ptr<Expr> parse_primary() {
        if (match(TokenType::When)) {
            return parse_when_expression();
        }

        if (match(TokenType::String)) {
            return std::make_unique<StringExpr>(previous().text);
        }

        if (match(TokenType::True)) {
            return std::make_unique<BoolExpr>(true);
        }

        if (match(TokenType::False)) {
            return std::make_unique<BoolExpr>(false);
        }

        if (match(TokenType::Null)) {
            return std::make_unique<NullExpr>();
        }

        if (match(TokenType::Number)) {
            return std::make_unique<NumberExpr>(std::stoll(previous().text));
        }

        if (match(TokenType::Identifier)) {
            return std::make_unique<VarExpr>(previous().text);
        }

        if (match(TokenType::LeftParen)) {
            auto inner = parse_expression();
            consume(TokenType::RightParen, "Expected ')' after expression");
            return inner;
        }

        if (match(TokenType::LeftBracket)) {
            std::vector<std::unique_ptr<Expr>> elements;
            if (!check(TokenType::RightBracket)) {
                do {
                    elements.push_back(parse_expression());
                } while (match(TokenType::Comma));
            }
            consume(TokenType::RightBracket, "Expected ']' after array elements");
            return std::make_unique<ArrayLiteralExpr>(std::move(elements));
        }

        throw std::runtime_error("Expected expression.");
    }

    std::unique_ptr<Expr> parse_when_expression() {
        std::unique_ptr<Expr> subject;
        if (match(TokenType::LeftParen)) {
            subject = parse_expression();
            consume(TokenType::RightParen, "Expected ')' after when subject");
        }

        consume(TokenType::LeftBrace, "Expected '{' after when");
        std::vector<WhenBranch> branches;
        bool else_seen = false;
        while (!check(TokenType::RightBrace) && !check(TokenType::Eof)) {
            WhenBranch branch;
            if (match(TokenType::Else)) {
                if (else_seen) {
                    throw std::runtime_error("Multiple else branches in when.");
                }
                else_seen = true;
                branch.is_else = true;
            } else {
                branch.condition = parse_expression();
            }

            consume(TokenType::Arrow, "Expected '->' after when branch condition");
            branch.result = parse_expression();
            branches.push_back(std::move(branch));
            match(TokenType::Semicolon);
        }
        consume(TokenType::RightBrace, "Expected '}' after when branches");
        return std::make_unique<WhenExpr>(std::move(subject), std::move(branches));
    }

    bool match(TokenType type) {
        if (!check(type)) {
            return false;
        }
        advance();
        return true;
    }

    bool check(TokenType type) const {
        if (is_at_end()) {
            return type == TokenType::Eof;
        }
        return tokens_[current_].type == type;
    }

    bool check_next(TokenType type) const {
        if (current_ + 1 >= tokens_.size()) {
            return false;
        }
        return tokens_[current_ + 1].type == type;
    }

    const Token& advance() {
        if (!is_at_end()) {
            current_++;
        }
        return previous();
    }

    bool is_at_end() const { return tokens_[current_].type == TokenType::Eof; }
    const Token& previous() const { return tokens_[current_ - 1]; }

    Token consume(TokenType type, const char* message) {
        if (check(type)) {
            return advance();
        }
        throw std::runtime_error(message);
    }

    std::vector<Token> tokens_;
    std::size_t current_ = 0;
};

void write_error(char* error, int capacity, const std::string& message) {
    if (error == nullptr || capacity <= 0) {
        return;
    }

    const auto count = static_cast<int>(std::min<std::size_t>(message.size(), static_cast<std::size_t>(capacity - 1)));
    std::memcpy(error, message.data(), static_cast<std::size_t>(count));
    error[count] = '\0';
}

std::int64_t count_sml_nodes(const char* source) {
    enum class Mode { Normal, String, LineComment };

    const std::string text(source);
    Mode mode = Mode::Normal;
    bool escape = false;
    bool pending_identifier = false;
    std::int64_t nodes = 0;

    auto is_identifier_start = [](char c) {
        return std::isalpha(static_cast<unsigned char>(c)) || c == '_';
    };
    auto is_identifier_char = [](char c) {
        return std::isalnum(static_cast<unsigned char>(c)) || c == '_' || c == '.';
    };

    for (std::size_t i = 0; i < text.size(); i++) {
        const char c = text[i];
        const char next = (i + 1 < text.size()) ? text[i + 1] : '\0';

        if (mode == Mode::LineComment) {
            if (c == '\n') {
                mode = Mode::Normal;
            }
            continue;
        }

        if (mode == Mode::String) {
            if (!escape && c == '"') {
                mode = Mode::Normal;
                continue;
            }
            if (c == '\\' && !escape) {
                escape = true;
            } else {
                escape = false;
            }
            continue;
        }

        if (c == '/' && next == '/') {
            mode = Mode::LineComment;
            i++;
            continue;
        }

        if (c == '"') {
            mode = Mode::String;
            escape = false;
            continue;
        }

        if (is_identifier_start(c)) {
            std::size_t j = i + 1;
            while (j < text.size() && is_identifier_char(text[j])) {
                j++;
            }
            pending_identifier = true;
            i = j - 1;
            continue;
        }

        if (c == '{') {
            if (pending_identifier) {
                nodes++;
            }
            pending_identifier = false;
            continue;
        }

        if (c == ':' || c == '=' || c == '}' || c == '(' || c == ')' || c == ';') {
            pending_identifier = false;
            continue;
        }
    }

    return nodes;
}

std::vector<Value> parse_event_args_json(const char* args_json);

struct SmsSessionRuntime {
    std::vector<std::unique_ptr<Stmt>> program;
    Env env;
    Value top_level_last = Value::Null();
    std::mutex mutex;
};

static std::shared_ptr<SmsSessionRuntime> build_session_runtime_or_throw(const char* source) {
    Lexer lexer(source);
    auto tokens = lexer.tokenize();
    Parser parser(std::move(tokens));
    auto program = parser.parse_program();

    auto runtime = std::make_shared<SmsSessionRuntime>();
    runtime->program = std::move(program);
    runtime->env.define_var("log", Value::Object("log", {}));
    runtime->env.define_var("os", Value::Object("os", {}));
    runtime->env.define_var("ui", Value::Object("ui", {}));

    for (const auto& stmt : runtime->program) {
        if (const auto* fn = dynamic_cast<const FunctionDeclStmt*>(stmt.get())) {
            runtime->env.define_function(fn->name, fn);
            continue;
        }
        if (const auto* data = dynamic_cast<const DataClassDeclStmt*>(stmt.get())) {
            runtime->env.define_data_class(data->name, data);
            continue;
        }
        if (const auto* handler = dynamic_cast<const EventHandlerDeclStmt*>(stmt.get())) {
            runtime->env.define_event_handler(handler->key(), handler);
        }
    }

    Value last = Value::Null();
    for (const auto& stmt : runtime->program) {
        if (dynamic_cast<const FunctionDeclStmt*>(stmt.get()) != nullptr
            || dynamic_cast<const DataClassDeclStmt*>(stmt.get()) != nullptr
            || dynamic_cast<const EventHandlerDeclStmt*>(stmt.get()) != nullptr) {
            continue;
        }
        stmt->execute(runtime->env, last);
    }

    runtime->top_level_last = last;
    return runtime;
}

static int invoke_event_on_runtime(
    SmsSessionRuntime& runtime,
    const char* target_id,
    const char* event_name,
    const char* args_json,
    std::int64_t* out_result,
    char* error,
    int error_capacity) {
    if (target_id == nullptr || event_name == nullptr || out_result == nullptr) {
        write_error(error, error_capacity, "target_id/event_name/out_result must not be null");
        return 2;
    }

    try {
        auto args = parse_event_args_json(args_json);
        std::lock_guard<std::mutex> lock(runtime.mutex);

        const std::string key = std::string(target_id) + "." + std::string(event_name);
        const auto* handler = runtime.env.get_event_handler(key);
        if (handler == nullptr) {
            write_error(error, error_capacity, "No SMS event handler found for '" + key + "'.");
            return 1;
        }

        if (args.size() != handler->params.size()) {
            write_error(error, error_capacity, "Event handler '" + key + "' expects "
                + std::to_string(handler->params.size()) + " arg(s), got "
                + std::to_string(args.size()) + ".");
            return 1;
        }

        Env local(&runtime.env);
        for (std::size_t idx = 0; idx < args.size(); idx++) {
            local.define_var(handler->params[idx], args[idx]);
        }

        Value handler_last = Value::Null();
        try {
            execute_statements(handler->body, local, handler_last);
        } catch (const ReturnSignal& ret) {
            handler_last = ret.value;
        }

        *out_result = handler_last.kind == Value::Kind::Int
            ? handler_last.int_value
            : (handler_last.kind == Value::Kind::Bool ? (handler_last.bool_value ? 1 : 0) : 0);
        return 0;
    } catch (const std::exception& ex) {
        write_error(error, error_capacity, ex.what());
        return 1;
    } catch (...) {
        write_error(error, error_capacity, "unknown native exception");
        return 1;
    }
}

int execute_sms_source(const char* source, std::int64_t* out_result, char* error, int error_capacity) {
    if (source == nullptr || out_result == nullptr) {
        write_error(error, error_capacity, "source/out_result must not be null");
        return 2;
    }

    try {
        auto runtime = build_session_runtime_or_throw(source);
        const auto& last = runtime->top_level_last;
        *out_result = last.kind == Value::Kind::Int
            ? last.int_value
            : (last.kind == Value::Kind::Bool ? (last.bool_value ? 1 : 0) : 0);
        return 0;
    } catch (const ReturnSignal&) {
        write_error(error, error_capacity, "return outside function");
        return 1;
    } catch (const BreakSignal&) {
        write_error(error, error_capacity, "break outside loop");
        return 1;
    } catch (const ContinueSignal&) {
        write_error(error, error_capacity, "continue outside loop");
        return 1;
    } catch (const std::exception& ex) {
        write_error(error, error_capacity, ex.what());
        return 1;
    } catch (...) {
        write_error(error, error_capacity, "unknown native exception");
        return 1;
    }
}

std::vector<Value> parse_event_args_json(const char* args_json) {
    std::vector<Value> args;
    if (args_json == nullptr) {
        return args;
    }

    const std::string text(args_json);
    std::size_t i = 0;
    auto skip_ws = [&]() {
        while (i < text.size() && std::isspace(static_cast<unsigned char>(text[i]))) {
            i++;
        }
    };
    auto starts_with = [&](const char* literal) {
        const auto len = std::strlen(literal);
        return i + len <= text.size() && text.compare(i, len, literal) == 0;
    };
    auto parse_int = [&]() -> Value {
        const auto start = i;
        if (i < text.size() && (text[i] == '-' || text[i] == '+')) {
            i++;
        }
        bool has_digit = false;
        while (i < text.size() && std::isdigit(static_cast<unsigned char>(text[i]))) {
            has_digit = true;
            i++;
        }
        if (!has_digit) {
            throw std::runtime_error("Invalid numeric argument in args_json.");
        }
        return Value::Int(std::stoll(text.substr(start, i - start)));
    };
    auto parse_string = [&]() -> Value {
        i++; // opening quote
        std::string out;
        bool escape = false;
        while (i < text.size()) {
            const char c = text[i++];
            if (escape) {
                switch (c) {
                    case '"': out.push_back('"'); break;
                    case '\\': out.push_back('\\'); break;
                    case '/': out.push_back('/'); break;
                    case 'b': out.push_back('\b'); break;
                    case 'f': out.push_back('\f'); break;
                    case 'n': out.push_back('\n'); break;
                    case 'r': out.push_back('\r'); break;
                    case 't': out.push_back('\t'); break;
                    default: out.push_back(c); break;
                }
                escape = false;
                continue;
            }
            if (c == '\\') {
                escape = true;
                continue;
            }
            if (c == '"') {
                return Value::String(std::move(out));
            }
            out.push_back(c);
        }
        throw std::runtime_error("Unterminated string in args_json.");
    };

    skip_ws();
    if (i >= text.size()) {
        return args;
    }
    if (text[i] != '[') {
        throw std::runtime_error("args_json must be a JSON array.");
    }
    i++;

    while (true) {
        skip_ws();
        if (i >= text.size()) {
            throw std::runtime_error("Unterminated args_json array.");
        }
        if (text[i] == ']') {
            i++;
            break;
        }

        Value value = Value::Null();
        if (text[i] == '"') {
            value = parse_string();
        } else if (starts_with("true")) {
            i += 4;
            value = Value::Bool(true);
        } else if (starts_with("false")) {
            i += 5;
            value = Value::Bool(false);
        } else if (starts_with("null")) {
            i += 4;
            value = Value::Null();
        } else {
            value = parse_int();
        }

        args.push_back(value);
        skip_ws();
        if (i < text.size() && text[i] == ',') {
            i++;
            continue;
        }
        if (i < text.size() && text[i] == ']') {
            i++;
            break;
        }
        if (i >= text.size()) {
            throw std::runtime_error("Unterminated args_json array.");
        }
        throw std::runtime_error("Expected ',' or ']' in args_json array.");
    }

    return args;
}

int invoke_sms_event(
    const char* source,
    const char* target_id,
    const char* event_name,
    const char* args_json,
    std::int64_t* out_result,
    char* error,
    int error_capacity) {
    if (source == nullptr || out_result == nullptr || target_id == nullptr || event_name == nullptr) {
        write_error(error, error_capacity, "source/target_id/event_name/out_result must not be null");
        return 2;
    }

    try {
        auto runtime = build_session_runtime_or_throw(source);
        return invoke_event_on_runtime(*runtime, target_id, event_name, args_json, out_result, error, error_capacity);
    } catch (const std::exception& ex) {
        write_error(error, error_capacity, ex.what());
        return 1;
    } catch (...) {
        write_error(error, error_capacity, "unknown native exception");
        return 1;
    }
}

} // namespace

extern "C" int sms_native_execute(const char* source, std::int64_t* out_result, char* error, int error_capacity) {
    return execute_sms_source(source, out_result, error, error_capacity);
}

extern "C" int sms_native_session_create(std::int64_t* out_session, char* error, int error_capacity) {
    if (out_session == nullptr) {
        write_error(error, error_capacity, "out_session must not be null");
        return 2;
    }

    std::lock_guard<std::mutex> lock(g_sessions_mutex);
    const auto session_id = g_next_session_id++;
    g_sessions.emplace(session_id, SmsSessionState{});
    *out_session = session_id;
    return 0;
}

extern "C" int sms_native_session_load(std::int64_t session, const char* source, char* error, int error_capacity) {
    if (source == nullptr) {
        write_error(error, error_capacity, "source must not be null");
        return 2;
    }

    try {
        auto runtime = build_session_runtime_or_throw(source);

        std::lock_guard<std::mutex> lock(g_sessions_mutex);
        const auto it = g_sessions.find(session);
        if (it == g_sessions.end()) {
            write_error(error, error_capacity, "invalid session");
            return 2;
        }

        it->second.source = source;
        it->second.runtime = std::move(runtime);
        return 0;
    } catch (const ReturnSignal&) {
        write_error(error, error_capacity, "return outside function");
        return 1;
    } catch (const BreakSignal&) {
        write_error(error, error_capacity, "break outside loop");
        return 1;
    } catch (const ContinueSignal&) {
        write_error(error, error_capacity, "continue outside loop");
        return 1;
    } catch (const std::exception& ex) {
        write_error(error, error_capacity, ex.what());
        return 1;
    } catch (...) {
        write_error(error, error_capacity, "unknown native exception");
        return 1;
    }
}

extern "C" int sms_native_session_invoke(
    std::int64_t session,
    const char* target_id,
    const char* event_name,
    const char* args_json,
    std::int64_t* out_result,
    char* error,
    int error_capacity) {
    std::shared_ptr<SmsSessionRuntime> runtime;
    {
        std::lock_guard<std::mutex> lock(g_sessions_mutex);
        const auto it = g_sessions.find(session);
        if (it == g_sessions.end()) {
            write_error(error, error_capacity, "invalid session");
            return 2;
        }
        runtime = it->second.runtime;
    }

    if (!runtime) {
        write_error(error, error_capacity, "session has no loaded source");
        return 2;
    }

    return invoke_event_on_runtime(*runtime, target_id, event_name, args_json, out_result, error, error_capacity);
}

extern "C" int sms_native_session_dispose(std::int64_t session, char* error, int error_capacity) {
    std::lock_guard<std::mutex> lock(g_sessions_mutex);
    if (g_sessions.erase(session) == 0) {
        write_error(error, error_capacity, "invalid session");
        return 2;
    }
    return 0;
}

extern "C" int sms_native_set_ui_callbacks(
    sms_native_ui_get_prop_fn get_prop,
    sms_native_ui_set_prop_fn set_prop,
    sms_native_ui_invoke_fn invoke,
    char*,
    int) {
    g_ui_get_prop = get_prop;
    g_ui_set_prop = set_prop;
    g_ui_invoke = invoke;
    return 0;
}

extern "C" int sms_native_sml_parse(const char* source, std::int64_t* out_node_count, char* error, int error_capacity) {
    if (source == nullptr || out_node_count == nullptr) {
        write_error(error, error_capacity, "source/out_node_count must not be null");
        return 2;
    }

    try {
        *out_node_count = count_sml_nodes(source);
        return 0;
    } catch (const std::exception& ex) {
        write_error(error, error_capacity, ex.what());
        return 1;
    } catch (...) {
        write_error(error, error_capacity, "unknown native exception");
        return 1;
    }
}
