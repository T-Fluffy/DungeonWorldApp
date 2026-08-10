# Fighting System — Seas of Blood

Rules extracted from the parsed *Seas of Blood* PDF (Fighting Fantasy gamebook by Steve Jackson / Ian Livingstone).

## Character Stats

Rolled at character creation and recorded on the Adventure Sheet:

| Stat | Roll | Notes |
| --- | --- | --- |
| SKILL | 1 die + 6 | Attack prowess. A 0 result during combat can mean a disabled limb. |
| STAMINA | 2 dice + 12 | Hit points. Reaching **0 = death**. |
| LUCK | 1 die + 6 | Luck tests. Each test costs 1 LUCK whether you pass or fail. |
| CREW STRIKE | 1 die + 6 | Your crew's attack score for large-scale battles. |
| CREW STRENGTH | 2 dice + 6 | Crew hit points. Reaching **0 = crew wiped out**. |
| LOG | 0 (start) | Days at sea. You must finish the King of Pirates bet within **50 days**. |
| BOOTY | 20 GP (start) | Gold + captured slaves. Gold can be spent on crew/passage. |

Roll results can be swapped between the captain (SKILL/STAMINA/LUCK) and the
crew (CREW STRIKE/CREW STRENGTH) if it helps.

## Individual Combat

Used when the captain fights a single creature (SKILL/STAMINA per the encounter box).

1. Both sides roll **2 dice** and add their **SKILL** → their **Attack Strength**.
2. Higher Attack Strength wins the round; the loser loses **2 STAMINA**.
3. Tied Attack Strengths = both miss; the round is wasted.
4. Repeat until one side's STAMINA reaches 0.
   - Player at 0 STAMINA = death (the book then gives the death ruling).
   - Enemy at 0 STAMINA = victory; follow the section's "If you defeat it, turn to N".

**Special hit modifiers** — some creatures roll an extra die on a hit:

- e.g. Section 63 (Awkmute): roll 1 die when it hits —
  - 1–2 → normal damage (−2 STAMINA)
  - 3–4 → lose **1 SKILL**
  - 5–6 → lose **1 LUCK**

**Multi-enemy boxes** — some sections (e.g. Section 81) list several foes
(each with their own SKILL/STAMINA); they are fought **one at a time**.

## Large-Scale Battles

Used for fleet/army engagements. The opponent has **STRIKE/STRENGTH**; you use
**CREW STRIKE/CREW STRENGTH**.

- Same mechanic: 2 dice + STRIKE vs 2 dice + STRIKE, loser −2 STRENGTH.
- **Escape** is only possible when the book offers it, and costs **2 CREW STRENGTH**.

## Dice-Check Branching

The book often asks for a specific roll:

- **Test your Luck**: roll 2 dice; if the result is **≤ your current LUCK**, you are
  Lucky (go to the Lucky section); otherwise Unlucky. Costs 1 LUCK either way.
- **Sailing checks** (e.g. Sections 7/10): roll **3 dice vs CREW STRENGTH** —
  different results add different amounts to your LOG.

## Implementation Notes

- The parsed sections flag combat with a boolean `HasCombat` (set when literal
  `SKILL` + `STAMINA` both appear in the text) — only ~19 of 400 sections match.
- Encounter boxes are unstructured prose with OCR artifacts (e.g. `AWKMUTE SKILLS
  STAMINAS` with mangled numbers). The frontend parses them heuristically via
  `frontend/src/utils/combat.ts`; unparseable fights degrade to manual `ROLL DICE`.
- App-facing stats live on `User` (skill/stamina/luck). CREW STRIKE/STRENGTH, LOG
  and BOOTY are not yet persisted (planned for the full engine phase).
