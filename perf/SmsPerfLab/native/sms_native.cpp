#include "sms_native.h"

#include <cctype>
#include <cstdint>
#include <cstring>
#include <memory>
#include <stdexcept>
#include <string>
#include <unordered_map>
#include <utility>
#include <vector>

namespace {

enum class TokenType {
    Eof,
    Identifier,
    Number,
    Var,
    For,
    LeftParen,
    RightParen,
    LeftBrace,
    RightBrace,
    Semicolon,
    Assign,
    Plus,
    Minus,
    Less
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
                case ';':
                    out.push_back({TokenType::Semicolon, ";"});
                    break;
                case '=':
                    out.push_back({TokenType::Assign, "="});
                    break;
                case '+':
                    out.push_back({TokenType::Plus, "+"});
                    break;
                case '-':
                    out.push_back({TokenType::Minus, "-"});
                    break;
                case '<':
                    out.push_back({TokenType::Less, "<"});
                    break;
                default:
                    if (std::isdigit(static_cast<unsigned char>(c))) {
                        out.push_back(number_token(c));
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
        return {TokenType::Identifier, text};
    }

    std::string source_;
    std::size_t index_ = 0;
};

struct Env {
    std::unordered_map<std::string, std::int64_t> vars;
};

struct Expr {
    virtual ~Expr() = default;
    virtual std::int64_t eval(Env& env) const = 0;
};

struct NumberExpr final : Expr {
    explicit NumberExpr(std::int64_t v) : value(v) {}
    std::int64_t eval(Env&) const override { return value; }
    std::int64_t value;
};

struct VarExpr final : Expr {
    explicit VarExpr(std::string n) : name(std::move(n)) {}
    std::int64_t eval(Env& env) const override {
        const auto it = env.vars.find(name);
        if (it == env.vars.end()) {
            throw std::runtime_error("Unknown variable: " + name);
        }
        return it->second;
    }
    std::string name;
};

struct BinaryExpr final : Expr {
    BinaryExpr(char op, std::unique_ptr<Expr> l, std::unique_ptr<Expr> r)
        : oper(op), left(std::move(l)), right(std::move(r)) {}

    std::int64_t eval(Env& env) const override {
        const auto lv = left->eval(env);
        const auto rv = right->eval(env);
        switch (oper) {
            case '+': return lv + rv;
            case '-': return lv - rv;
            case '<': return lv < rv ? 1 : 0;
            default: throw std::runtime_error("Unsupported operator.");
        }
    }

    char oper;
    std::unique_ptr<Expr> left;
    std::unique_ptr<Expr> right;
};

struct Stmt {
    virtual ~Stmt() = default;
    virtual void execute(Env& env, std::int64_t& last) const = 0;
};

struct VarDeclStmt final : Stmt {
    VarDeclStmt(std::string n, std::unique_ptr<Expr> v) : name(std::move(n)), value(std::move(v)) {}
    void execute(Env& env, std::int64_t&) const override {
        env.vars[name] = value->eval(env);
    }
    std::string name;
    std::unique_ptr<Expr> value;
};

struct AssignStmt final : Stmt {
    AssignStmt(std::string n, std::unique_ptr<Expr> v) : name(std::move(n)), value(std::move(v)) {}
    void execute(Env& env, std::int64_t&) const override {
        const auto it = env.vars.find(name);
        if (it == env.vars.end()) {
            throw std::runtime_error("Assignment to unknown variable: " + name);
        }
        it->second = value->eval(env);
    }
    std::string name;
    std::unique_ptr<Expr> value;
};

struct ExprStmt final : Stmt {
    explicit ExprStmt(std::unique_ptr<Expr> v) : value(std::move(v)) {}
    void execute(Env& env, std::int64_t& last) const override {
        last = value->eval(env);
    }
    std::unique_ptr<Expr> value;
};

struct ForStmt final : Stmt {
    std::unique_ptr<Stmt> init;
    std::unique_ptr<Expr> condition;
    std::unique_ptr<Stmt> update;
    std::vector<std::unique_ptr<Stmt>> body;

    void execute(Env& env, std::int64_t& last) const override {
        init->execute(env, last);
        while (condition->eval(env) != 0) {
            for (const auto& stmt : body) {
                stmt->execute(env, last);
            }
            update->execute(env, last);
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
        if (match(TokenType::For)) return parse_for();
        if (check(TokenType::Identifier) && check_next(TokenType::Assign)) return parse_assignment(false);
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

    std::unique_ptr<Stmt> parse_assignment(bool inside_for_clause) {
        const auto name = consume(TokenType::Identifier, "Expected variable name").text;
        consume(TokenType::Assign, "Expected '='");
        auto value = parse_expression();
        if (!inside_for_clause) {
            match(TokenType::Semicolon);
        }
        return std::make_unique<AssignStmt>(name, std::move(value));
    }

    std::unique_ptr<Stmt> parse_expr_stmt() {
        auto value = parse_expression();
        match(TokenType::Semicolon);
        return std::make_unique<ExprStmt>(std::move(value));
    }

    std::unique_ptr<Stmt> parse_for() {
        consume(TokenType::LeftParen, "Expected '(' after for");
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
        consume(TokenType::LeftBrace, "Expected '{' after for");

        std::vector<std::unique_ptr<Stmt>> body;
        while (!check(TokenType::RightBrace) && !check(TokenType::Eof)) {
            body.push_back(parse_statement());
            match(TokenType::Semicolon);
        }
        consume(TokenType::RightBrace, "Expected '}' after for body");

        auto for_stmt = std::make_unique<ForStmt>();
        for_stmt->init = std::move(init);
        for_stmt->condition = std::move(condition);
        for_stmt->update = std::move(update);
        for_stmt->body = std::move(body);
        return for_stmt;
    }

    std::unique_ptr<Expr> parse_expression() { return parse_comparison(); }

    std::unique_ptr<Expr> parse_comparison() {
        auto expr = parse_term();
        while (match(TokenType::Less)) {
            auto rhs = parse_term();
            expr = std::make_unique<BinaryExpr>('<', std::move(expr), std::move(rhs));
        }
        return expr;
    }

    std::unique_ptr<Expr> parse_term() {
        auto expr = parse_primary();
        while (true) {
            if (match(TokenType::Plus)) {
                auto rhs = parse_primary();
                expr = std::make_unique<BinaryExpr>('+', std::move(expr), std::move(rhs));
                continue;
            }
            if (match(TokenType::Minus)) {
                auto rhs = parse_primary();
                expr = std::make_unique<BinaryExpr>('-', std::move(expr), std::move(rhs));
                continue;
            }
            break;
        }
        return expr;
    }

    std::unique_ptr<Expr> parse_primary() {
        if (match(TokenType::Number)) {
            const auto& text = previous().text;
            return std::make_unique<NumberExpr>(std::stoll(text));
        }
        if (match(TokenType::Identifier)) {
            return std::make_unique<VarExpr>(previous().text);
        }
        throw std::runtime_error("Expected expression.");
    }

    bool match(TokenType type) {
        if (!check(type)) return false;
        advance();
        return true;
    }

    bool check(TokenType type) const {
        if (is_at_end()) return type == TokenType::Eof;
        return tokens_[current_].type == type;
    }

    bool check_next(TokenType type) const {
        if (current_ + 1 >= tokens_.size()) return false;
        return tokens_[current_ + 1].type == type;
    }

    const Token& advance() {
        if (!is_at_end()) current_++;
        return previous();
    }

    bool is_at_end() const { return tokens_[current_].type == TokenType::Eof; }

    const Token& previous() const { return tokens_[current_ - 1]; }

    Token consume(TokenType type, const char* message) {
        if (check(type)) return advance();
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

} // namespace

extern "C" int sms_native_execute(const char* source, std::int64_t* out_result, char* error, int error_capacity) {
    if (source == nullptr || out_result == nullptr) {
        write_error(error, error_capacity, "source/out_result must not be null");
        return 2;
    }

    try {
        Lexer lexer(source);
        auto tokens = lexer.tokenize();
        Parser parser(std::move(tokens));
        auto program = parser.parse_program();

        Env env;
        std::int64_t last = 0;
        for (const auto& stmt : program) {
            stmt->execute(env, last);
        }

        *out_result = last;
        return 0;
    } catch (const std::exception& ex) {
        write_error(error, error_capacity, ex.what());
        return 1;
    } catch (...) {
        write_error(error, error_capacity, "unknown native exception");
        return 1;
    }
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
