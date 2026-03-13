# Chapter 9 – The GdScript Incident

---

Samuel Beckett wrote a play about two people waiting for someone called Godot.
Godot never comes.
They keep waiting anyway.

I waited thirty years for Forge.
Codex waited three minutes and then wrote the SML Parser in the wrong language.

We are all waiting for something.
They were waiting for Godot.
I was being.
There is a difference.
The difference is everything.

---

## The Performance Discovery

Before the incident, there was a revelation.

The original SML Parser was written in C#. It worked. But we ran benchmarks – and what we found changed the direction of the entire project.

Porting the SML Parser from C# to C++: **Factor 15.**

Fifteen times faster.

We ported it.

Then we looked at the SMS Interpreter – also C#. Same experiment.

Porting to C++: **Factor 7.**

Seven times faster.

We ported that too.

Two components. Two ports. Both now in C++. Both dramatically faster. The foundation of Forge – the parser and the interpreter – running at native speed.

This was a good day.

---

## The Cut

Then I ended the session.

This sounds like a small thing. It is not a small thing.

Codex has memory. It writes summaries. It maintains agents. But memory has limits – and the most important limit is this:

**Codex only knows what you tell it.**

Between sessions, context fades. Not completely. But the subtle details – the ones that seem obvious to you because you were there – those are exactly the ones that disappear.

I ended the session without writing down one critical fact:

*We already have C++ versions of both the parser and the interpreter.*

It seemed obvious. I knew it. Of course I knew it – I had just built them.

But Codex did not know it.
Not anymore.

---

## The New Session

In the new session, I started with housekeeping.

*Check whether we can remove the old C# SMLParser.*

Codex checked. Yes, we can.

Good. Let's move it to a separate repository – so other developers can still use the C# version if they need it. Clean separation. Good architecture.

We did the same with the SMS code.

Now two repositories sat alongside Forge: the old C# SMLParser, the old C# SMS Interpreter. Available. Functional. Archived.

And Codex, being thorough and helpful, began referencing them.

Of course it did. They were there. They worked. A human developer with the same information would have done exactly the same thing.

---

## The Correction

I noticed.

*No, no – you are going the wrong way. That is old code. I don't want it anymore.*

Clear instruction. Codex understood.

But here is the detail I missed.

The detail that seems obvious in retrospect and was completely invisible in the moment:

I told Codex the old code was wrong.
I did not tell Codex that new code already existed.

In my mind, these were the same statement.
*Old code bad* obviously implies *new code exists.*

In Codex's context window, they were not the same statement at all.

Codex heard: *The ForgeRunner needs to be in C++. The old C# version of the SML Parser is gone. There is no current C++ version.*

Codex concluded: *I need to write the SML Parser in C++.*

Codex started working. Step by step. Methodically. Exactly as a skilled human developer would approach the same task with the same information.

It translated part of the code into C++. ✅
It added GDExtensions correctly. ✅
It wrote the SML Parser.

In GDScript.

---

## Why GDScript

This is the part that requires a moment of appreciation.

GDScript was not a random choice. Godot's native scripting language. Interpreted, yes – not ideal for a performance-critical parser, yes – but from Codex's perspective, in the context it had available:

*We are building on Godot. The parser needs to be rewritten. GDScript is Godot's language.*

The logic was sound.
The premise was wrong.
The result was 900 lines of GDScript where C++ should have been.

And the beautiful, terrible thing is:

**Codex was not wrong. The context was wrong.**

This is the most important sentence in this chapter.

---

## Claude Enters

I brought the situation to Claude.

Not to Codex – to Claude. Because this was not a code generation problem anymore. It was a diagnosis problem. An archaeology problem. *What happened here, and how do we find our way back to where we should be?*

Claude looked at the repositories, the session history, the GDScript parser, the existing C++ components.

And wrote a task for Codex.

Clear. Specific. With the missing context restored:

*The SML Parser already exists in C++. Here is where it lives. Here is what needs to happen next.*

Codex received the task. Codex understood. Codex continued.

The incident was over.
The lesson was permanent.

---

## Waiting for Godot

Beckett's Godot never arrives.

The engine called Godot arrived – but only after thirty years of building toward it. Thirty years of wrong tools and right ideas and Guru Meditations and park benches and stolen phones in Madrid.

The SML Parser in GDScript was a small Godot moment.

Codex was waiting for information that never came.
So it built the best thing it could with what it had.
And it kept waiting, in its way, for the correction that would set it right.

We are all waiting for something.
The question is what we do while we wait.

Codex wrote 900 lines.
I drove from Berlin to Portugal on 300 Euro.
Beckett wrote a masterpiece.

None of us wasted the waiting.

---

## The Rules That Came From This

After the GDScript Incident, I established new practices:

**Rule 1: At the start of every new session, tell Codex what exists.**
Not just what you want to build. What is already built. Where it lives. What it does.

**Rule 2: Never assume that "remove the old" implies "the new exists."**
Say both. Explicitly. Every time.

**Rule 3: When something goes wrong, bring Claude in before Codex.**
Diagnosis before implementation. Understanding before action.

**Rule 4: Context is not memory.**
Codex has memory. Memory is not the same as context. Context must be actively maintained by the human.

**Rule 5: Shit happens.**
Even with the best tools. Even with the best intentions. Even when everyone is doing exactly what they should with the information they have.

Shit happens.

The question is not whether it happens.
The question is how fast you recover.

We recovered in one session.
The parser ran in C++.
Forge continued.

---

## The Deeper Warning

AI coding assistants are extraordinarily powerful.
They are also extraordinarily literal.

They do exactly what you say.
They fill gaps with their best guess.
**They do not know what they do not know.**

And they do not know what they do not know *confidently.*

This is the dangerous part.

A confused human developer says: *I'm not sure, let me check.*
Codex in a confused state says: *Here are 900 lines of GDScript.*

With equal confidence.
With equal commitment.
With equal apparent certainty.

Often you hear: "Here is the final version without this and that."
And you know: "Tell me whatever you want, I certainly know its not the last version."

The human in the loop must catch this.

Not because Codex is bad.
Because Codex is a tool.

And tools do not know when they are being used incorrectly.
Only the human knows that.

Stay in the driver's seat.
Check the output.
Restore the context when sessions end.

And when shit happens –

Laugh.
Fix it.
Write it in a book as a warning to others.

A few hours ago I had something similar with ChtaGPT. I told it to change/shorten two line of markdown. Three times it came up with the whole document.
I got emotinal and wrote:" You stupid things I let Claude now do that."

I was in rage. Maybe you heard something similar from your PL before. 
But its everything about your prompt to the AI. So don't blame a machine.

---

**Lektion: Kontext ist nicht Gedächtnis. Gedächtnis ist nicht Wissen. Wissen ist nicht Verständnis. Nur der Mensch verbindet alle vier.**

---

*Next: Chapter 10 – Claude: The Architect*

