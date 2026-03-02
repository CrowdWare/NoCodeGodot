### RIS-2026-03 – "Unlimited Grok Exports" Subscription Feature (9,90 €/Monat)

**Artefakt / Feature**  
Forge4D Plugin "ExportToGrokImagine" bietet eine Commercial Pro-Variante mit pauschaler "Unlimited Grok Video Exports" für 9,90 €/Monat (Forge managed xAI API-Key).

**Risiko-Beschreibung**  
- Lineare Kostenskala: Ein einziger Power-User mit 200 × 8s-720p-Videos/Monat kostet Forge **ca. 112 USD** reine xAI-Kosten.  
- Bei nur 50 aktiven Pro-Usern bereits **>5.000 USD/Monat** Kosten – bei 9,90 € Einnahmen pro User schnell tiefrot.  
- xAI Rate-Limit 60 RPM + mögliches Verbot von API-Reselling/Time-Sharing → Risiko einer plötzlichen Account-Sperrung oder Preiserhöhung.  
- Rechtliches Risiko: Verstoß gegen xAI ToS möglich → gesamtes Plugin könnte betroffen sein.

**Risiko-Level**: Hoch (Finanziell + Legal + Reputationsrisiko)  
**Wahrscheinlichkeit bei >30 Pro-Usern**: Hoch  
**Maximale Auswirkung**: Monatliche Verluste >10k € + temporäre Dienstunterbrechung für alle Kunden

**Mitigation-Strategien (priorisiert)**  
1. **Default: BYOK-Modell** (User bringt eigenen xAI-Key mit) – 0 € Kosten für Forge, 100 % transparent  
2. **Fair-Use-Limit** in der Commercial-Version (z. B. 50 Videos/Monat inklusive, danach Pay-per-Use zu Selbstkosten + 30 % Marge)  
3. **Hosted Proxy nur mit Enterprise-Vertrag** (xAI direkt kontaktieren für höhere Limits)  
4. **Preis dynamisch** machen (z. B. „ab 9,90 € je nach Nutzung“ statt Flat-Unlimited)  
5. **Monitoring-Dashboard** im Plugin (API-Verbrauch pro User + Warnung bei >80 % Budget)

**Status**: Offen – Entscheidung bis v0.4.0  
**Owner**: Art
**Letztes Review**: 02.03.2026