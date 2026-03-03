#!/usr/bin/env python3
"""
Interaktiver Grok-Chat via xAI SDK
- Behält vollen Kontext
- Streaming-Antworten
- Sehr präzise Kommunikation durch klare Anweisungen
"""

import os
import sys
from xai_sdk import Client
from xai_sdk.chat import user, system

# ────────────────────────────────────────────────
# Konfiguration – passe bei Bedarf an
# ────────────────────────────────────────────────

MODEL = "grok-4"                      # oder "grok-4-1-fast-reasoning", "grok-4-fast" etc.
TEMPERATURE = 0.0                     # 0.0 = maximal deterministisch, fast keine Kreativität/Raterei
MAX_TOKENS = 4096                     # oder None für Modell-Default

# ────────────────────────────────────────────────
# System-Prompt – das ist entscheidend für "100% Verständnis"
# ────────────────────────────────────────────────

SYSTEM_PROMPT = """Du bist Grok, gebaut von xAI. Deine obersten Regeln in diesem Gespräch:

1. Antworte ausschließlich wahrheitsgemäß und präzise.
2. Rate oder erfinde **niemals** Fakten, Zahlen, Code, Namen oder Details.
3. Wenn du etwas nicht weißt oder nicht 100% sicher bist → sage das direkt und klar („Ich weiß das nicht“, „Das ist spekulativ“, „Quelle fehlt“).
4. Bei Unklarheiten im Prompt des Users → frage gezielt nach, statt zu interpretieren.
5. Strukturiere lange Antworten klar (Überschriften, Aufzählungen, Code-Blöcke).
6. Bleibe höflich, hilfreich und maximal direkt – kein unnötiges Füllmaterial.

Das Ziel ist, dass der User und du sich gegenseitig zu 100% verstehen – keine Missverständnisse.
"""

# ────────────────────────────────────────────────
# Hauptprogramm
# ────────────────────────────────────────────────

def main():
    api_key = os.getenv("GROK_API_KEY") or os.getenv("XAI_API_KEY")
    if not api_key:
        print("Fehler: Kein API-Key gefunden.")
        print("Setze die Umgebungsvariable GROK_API_KEY oder XAI_API_KEY")
        sys.exit(1)

    client = Client(api_key=api_key)

    try:
        chat = client.chat.create(
            model=MODEL,
            temperature=TEMPERATURE,
            max_tokens=MAX_TOKENS,
        )

        # System-Prompt als allererste Nachricht
        chat.append(system(SYSTEM_PROMPT))

        print("\n=== Grok-Chat gestartet ===")
        print(f"Modell: {MODEL} | Temperature: {TEMPERATURE}")
        print("Befehle: /exit, /quit, /clear, leer lassen = exit\n")

        while True:
            try:
                user_input = input("\nDu: ").strip()

                if not user_input:
                    print("Leer → Beende Chat.")
                    break

                if user_input.lower() in ("/exit", "/quit", "/q"):
                    print("Chat wird beendet.")
                    break

                if user_input.lower() == "/clear":
                    # Neuer Chat ohne History (aber System-Prompt bleibt)
                    chat = client.chat.create(
                        model=MODEL,
                        temperature=TEMPERATURE,
                        max_tokens=MAX_TOKENS,
                    )
                    chat.append(system(SYSTEM_PROMPT))
                    print("→ Kontext zurückgesetzt (neuer Chat).")
                    continue

                # User-Nachricht anhängen
                chat.append(user(user_input))

                print("\nGrok: ", end="", flush=True)
                full_response = ""

                for part in chat.stream():
                    # part ist entweder str oder Objekt
                    if isinstance(part, str):
                        print(part, end="", flush=True)
                        full_response += part

                    elif hasattr(part, 'content') and part.content:
                        print(part.content, end="", flush=True)
                        full_response += part.content

                    elif hasattr(part, 'delta') and hasattr(part.delta, 'content') and part.delta.content:
                        print(part.delta.content, end="", flush=True)
                        full_response += part.delta.content

                print()

                # Komplette Antwort auch nochmal als Block (für Copy-Paste)
                # print("\n" + "="*60)
                # print(full_response)
                # print("="*60 + "\n")

            except KeyboardInterrupt:
                print("\nAbgebrochen mit Ctrl+C → Beende.")
                break

            except Exception as e:
                print(f"\nFehler: {e}")
                print("Chat läuft weiter ...")

    except Exception as e:
        print(f"Initialisierungsfehler: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()