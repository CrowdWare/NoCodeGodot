#pragma once

#include <string>
#include <vector>

namespace forge {

enum class BlockKind { Heading, Paragraph, CodeBlock, ListItem, HRule, Image };

struct MarkdownBlock {
    BlockKind   kind  = BlockKind::Paragraph;
    int         level = 0;     // heading level 1–6
    std::string text;          // inline content (may contain inline markup)
    std::string lang;          // fenced code block language hint
    std::string src;           // image source URL / path
    std::string alt;           // image alt text
};

// Parse markdown text into a flat list of blocks.
std::vector<MarkdownBlock> parse_markdown(const std::string& md);

// Convert inline markdown markup to Godot BBCode.
// Handles: **bold**, _italic_, `code`, [label](url)
std::string inline_to_bbcode(const std::string& text);

} // namespace forge
