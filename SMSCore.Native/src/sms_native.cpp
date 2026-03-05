#include "sms_native.h"

#include <algorithm>
#include <cctype>
#include <cstddef>
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
    Data,
    Class,
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
        if (text == "data") return {TokenType::Data, text};
        if (text == "class") return {TokenType::Class, text};
        if (text == "return") return {TokenType::Return, text};
        return {TokenType::Identifier, text};
    }

    std::string source_;
    std::size_t index_ = 0;
};

struct FunctionDeclStmt;
struct DataClassDeclStmt;

struct Value {
    enum class Kind { Int, Array, Object, Null };

    Kind kind = Kind::Null;
    std::int64_t int_value = 0;
    std::shared_ptr<std::vector<Value>> array;
    std::string class_name;
    std::shared_ptr<std::unordered_map<std::string, Value>> object_fields;

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

    static Value Object(std::string class_name, std::unordered_map<std::string, Value> fields) {
        Value v;
        v.kind = Kind::Object;
        v.class_name = std::move(class_name);
        v.object_fields = std::make_shared<std::unordered_map<std::string, Value>>(std::move(fields));
        return v;
    }

    bool truthy() const {
        if (kind == Kind::Int) return int_value != 0;
        if (kind == Kind::Array) return array && !array->empty();
        if (kind == Kind::Object) return object_fields && !object_fields->empty();
        return false;
    }

    std::int64_t as_int(const std::string& where) const {
        if (kind != Kind::Int) {
            throw std::runtime_error(where + " expects integer value.");
        }
        return int_value;
    }

    bool operator==(const Value& other) const {
        if (kind != other.kind) {
            return false;
        }

        switch (kind) {
            case Kind::Int:
                return int_value == other.int_value;
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

struct MemberAccessExpr final : Expr {
    MemberAccessExpr(std::unique_ptr<Expr> receiver, std::string member)
        : receiver_(std::move(receiver)), member_(std::move(member)) {}

    Value eval(Env& env) const override {
        auto receiver = receiver_->eval(env);
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

struct DataClassDeclStmt final : Stmt {
    std::string name;
    std::vector<std::string> fields;

    void execute(Env& env, Value&) const override {
        env.define_data_class(name, this);
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
        if (match(TokenType::Data)) return parse_data_class_decl();
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
                target = std::make_unique<MemberAccessExpr>(std::move(target), member);
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
                continue;
            }
            if (const auto* data = dynamic_cast<const DataClassDeclStmt*>(stmt.get())) {
                env.define_data_class(data->name, data);
            }
        }

        for (const auto& stmt : program) {
            if (dynamic_cast<const FunctionDeclStmt*>(stmt.get()) != nullptr
                || dynamic_cast<const DataClassDeclStmt*>(stmt.get()) != nullptr) {
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
