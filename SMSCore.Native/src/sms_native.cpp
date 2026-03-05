#include "sms_native.h"

#include <algorithm>
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
    In,
    If,
    Else,
    While,
    Fun,
    Return,
    LeftParen,
    RightParen,
    LeftBrace,
    RightBrace,
    LeftBracket,
    RightBracket,
    Comma,
    Semicolon,
    Assign,
    Plus,
    Minus,
    Less,
    Greater,
    LessEqual,
    GreaterEqual,
    EqualEqual,
    BangEqual
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
                    out.push_back({TokenType::Plus, "+"});
                    break;
                case '-':
                    out.push_back({TokenType::Minus, "-"});
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
                        throw std::runtime_error("Unexpected character in source.");
                    }
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
        if (text == "in") return {TokenType::In, text};
        if (text == "if") return {TokenType::If, text};
        if (text == "else") return {TokenType::Else, text};
        if (text == "while") return {TokenType::While, text};
        if (text == "fun") return {TokenType::Fun, text};
        if (text == "return") return {TokenType::Return, text};
        return {TokenType::Identifier, text};
    }

    std::string source_;
    std::size_t index_ = 0;
};

struct FunctionDeclStmt;

struct Value {
    enum class Kind { Int, Array, Null };

    Kind kind = Kind::Null;
    std::int64_t int_value = 0;
    std::shared_ptr<std::vector<Value>> array;

    static Value Int(std::int64_t value) {
        Value v;
        v.kind = Kind::Int;
        v.int_value = value;
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

    bool truthy() const {
        if (kind == Kind::Int) return int_value != 0;
        if (kind == Kind::Array) return array && !array->empty();
        return false;
    }

    std::int64_t as_int(const std::string& where) const {
        if (kind != Kind::Int) {
            throw std::runtime_error(where + " expects integer value.");
        }
        return int_value;
    }
};

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
};

struct ReturnSignal final : std::exception {
    explicit ReturnSignal(Value v) : value(std::move(v)) {}
    Value value;
};

struct Expr {
    virtual ~Expr() = default;
    virtual Value eval(Env& env) const = 0;
};

struct NumberExpr final : Expr {
    explicit NumberExpr(std::int64_t v) : value(v) {}
    Value eval(Env&) const override { return Value::Int(value); }
    std::int64_t value;
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

struct BinaryExpr final : Expr {
    BinaryExpr(TokenType op, std::unique_ptr<Expr> left, std::unique_ptr<Expr> right)
        : oper(op), left_(std::move(left)), right_(std::move(right)) {}

    Value eval(Env& env) const override {
        const auto left = left_->eval(env).as_int("Binary expression");
        const auto right = right_->eval(env).as_int("Binary expression");
        switch (oper) {
            case TokenType::Plus: return Value::Int(left + right);
            case TokenType::Minus: return Value::Int(left - right);
            case TokenType::Less: return Value::Int(left < right ? 1 : 0);
            case TokenType::LessEqual: return Value::Int(left <= right ? 1 : 0);
            case TokenType::Greater: return Value::Int(left > right ? 1 : 0);
            case TokenType::GreaterEqual: return Value::Int(left >= right ? 1 : 0);
            case TokenType::EqualEqual: return Value::Int(left == right ? 1 : 0);
            case TokenType::BangEqual: return Value::Int(left != right ? 1 : 0);
            default:
                throw std::runtime_error("Unsupported operator.");
        }
    }

    TokenType oper;
    std::unique_ptr<Expr> left_;
    std::unique_ptr<Expr> right_;
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
            throw std::runtime_error("Unknown function: " + name);
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

enum class AssignTargetKind {
    Variable,
    ArrayElement
};

struct AssignStmt final : Stmt {
    AssignStmt(AssignTargetKind kind, std::string name, std::unique_ptr<Expr> index, std::unique_ptr<Expr> value)
        : target_kind(kind), target_name(std::move(name)), index_expr(std::move(index)), value_expr(std::move(value)) {}

    void execute(Env& env, Value&) const override {
        const auto value = value_expr->eval(env);

        if (target_kind == AssignTargetKind::Variable) {
            if (!env.assign_var(target_name, value)) {
                throw std::runtime_error("Assignment to unknown variable: " + target_name);
            }
            return;
        }

        auto receiver = env.get_var(target_name);
        if (receiver.kind != Value::Kind::Array || !receiver.array) {
            throw std::runtime_error("Array assignment expects array receiver: " + target_name);
        }

        const auto idx = index_expr->eval(env).as_int("Array assignment index");
        if (idx < 0) {
            throw std::runtime_error("Array assignment index must be >= 0.");
        }

        const auto target = static_cast<std::size_t>(idx);
        if (target >= receiver.array->size()) {
            receiver.array->resize(target + 1, Value::Null());
        }

        (*receiver.array)[target] = value;
        if (!env.assign_var(target_name, receiver)) {
            throw std::runtime_error("Assignment to unknown array variable: " + target_name);
        }
    }

    AssignTargetKind target_kind;
    std::string target_name;
    std::unique_ptr<Expr> index_expr;
    std::unique_ptr<Expr> value_expr;
};

struct ReturnStmt final : Stmt {
    explicit ReturnStmt(std::unique_ptr<Expr> v) : value(std::move(v)) {}
    void execute(Env& env, Value&) const override {
        throw ReturnSignal(value ? value->eval(env) : Value::Null());
    }

    std::unique_ptr<Expr> value;
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
            execute_statements(body, env, last);
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
            execute_statements(body, env, last);
        }
    }
};

struct WhileStmt final : Stmt {
    std::unique_ptr<Expr> condition;
    std::vector<std::unique_ptr<Stmt>> body;

    void execute(Env& env, Value& last) const override {
        while (condition->eval(env).truthy()) {
            execute_statements(body, env, last);
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
        if (match(TokenType::For)) return parse_for();
        if (match(TokenType::While)) return parse_while();
        if (match(TokenType::If)) return parse_if();
        if (match(TokenType::Fun)) return parse_function_decl();
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

    std::unique_ptr<Stmt> parse_assignment(bool inside_for_clause) {
        const auto name = consume(TokenType::Identifier, "Expected assignment target").text;

        AssignTargetKind kind = AssignTargetKind::Variable;
        std::unique_ptr<Expr> index;
        if (match(TokenType::LeftBracket)) {
            kind = AssignTargetKind::ArrayElement;
            index = parse_expression();
            consume(TokenType::RightBracket, "Expected ']' after assignment index");
        }

        consume(TokenType::Assign, "Expected '=' in assignment");
        auto value = parse_expression();
        if (!inside_for_clause) {
            match(TokenType::Semicolon);
        }
        return std::make_unique<AssignStmt>(kind, name, std::move(index), std::move(value));
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
            else_body = parse_block();
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

    std::unique_ptr<Expr> parse_expression() { return parse_equality(); }

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
        auto expr = parse_postfix();
        while (true) {
            if (match(TokenType::Plus)) {
                auto rhs = parse_postfix();
                expr = std::make_unique<BinaryExpr>(TokenType::Plus, std::move(expr), std::move(rhs));
                continue;
            }
            if (match(TokenType::Minus)) {
                auto rhs = parse_postfix();
                expr = std::make_unique<BinaryExpr>(TokenType::Minus, std::move(expr), std::move(rhs));
                continue;
            }
            break;
        }
        return expr;
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

            if (match(TokenType::LeftBracket)) {
                auto index = parse_expression();
                consume(TokenType::RightBracket, "Expected ']' after array index");
                expr = std::make_unique<ArrayAccessExpr>(std::move(expr), std::move(index));
                continue;
            }

            break;
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
        Value last = Value::Null();

        for (const auto& stmt : program) {
            if (const auto* fn = dynamic_cast<const FunctionDeclStmt*>(stmt.get())) {
                env.define_function(fn->name, fn);
            }
        }

        for (const auto& stmt : program) {
            if (dynamic_cast<const FunctionDeclStmt*>(stmt.get()) != nullptr) {
                continue;
            }
            stmt->execute(env, last);
        }

        *out_result = last.kind == Value::Kind::Int ? last.int_value : 0;
        return 0;
    } catch (const ReturnSignal&) {
        write_error(error, error_capacity, "return outside function");
        return 1;
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
