# Company Wars — Roadmap

Status: **Phase 5 draft.** A phased build order from the first commit to a Steam
release, then what comes after. Each milestone states what becomes playable, what
question it answers, its exit criteria, what in it is throwaway and what carries into
ranked, and what it needs from a human.

Three rules shape this document, all locked earlier:

- **Milestones are defined by systems.** None is gated on art existing. Art
  completeness is a *release* gate on its own line (§8), tracked from the first commit
  and never a dependency of any milestone.
- **Campaign first.** The async PvP backend is out of v1. Scripted and templated
  rivals stand in for opponents, and they double as balance fixtures and, later, as
  the seed ghost pool (D-36).
- **Get to fun first, in greybox.** The earliest point at which the game is fun is
  named (§3), and the order is arranged to reach it before anything that does not
  serve it.

There are no dates. Development is primarily agent-driven with a human stepping in for
layout and hard design problems, and the honest unit of planning is the milestone's
exit criteria, not a week. Relative size is given as S / M / L.

---

## Contents

1. [Overview](#1-overview)
2. [M0 — Foundations](#2-m0--foundations)
3. [M1 — The fight](#3-m1--the-fight)
4. [M2 — The loop, the vertical slice](#4-m2--the-loop-the-vertical-slice)
5. [M3 — Depth](#5-m3--depth)
6. [M4 — The campaign](#6-m4--the-campaign)
7. [M5 — Ship-ready](#7-m5--ship-ready)
8. [Release gates](#8-release-gates)
9. [Post-v1](#9-post-v1)
10. [Throwaway and carry-forward, in one table](#10-throwaway-and-carry-forward-in-one-table)
11. [What the human does](#11-what-the-human-does)

---

## 1. Overview

| Milestone | Playable | Question it answers | Size |
| --- | --- | --- | --- |
| **M0** Foundations | Nothing. Two sims agree; the loaders reject bad files; a greybox rectangle draws | Does the foundation hold? | M |
| **M1** The fight | Watch two authored towers fight, with the ledger, the Goodwill bars, the bar, the autopsy | Is a fight readable? | M |
| **M2** The loop | Build → Ready → fight → autopsy → next round, sixteen rounds against templated rivals | **Is the game fun?** | L |
| **M3** Depth | Recipes, the codex, Tenure, furniture on trial, semi-scripted rivals, the harness | Does commitment pay, and does furniture earn its tile? | L |
| **M4** The campaign | The map, interludes, bosses and gimmicks, the portal, founders, the first-run tutorial, save and load | Is a 45-minute run the right shape? | L |
| **M5** Ship-ready | Profile, unlocks, Steam, packaging, performance, the full nightly matrix | Does it survive a stranger? | M |
| **Release** | — | Every gate in §8 green | — |

M2 is the vertical slice and the UX milestone (D-48). Everything before it exists to
reach it; everything after it exists because it was fun.

---

## 2. M0 — Foundations

**Playable:** nothing. A developer can run a fixture through the sim, load the content
and manifest, and see a labelled rectangle on screen.

**Systems**

- The pnpm workspace and the dependency-direction lint (D-42).
- `packages/sim` implementing `SIMULATION_SPEC.md` end to end, with the ten
  conformance fixtures from its §19 recorded and passing — including the mirror
  fixture's 90 entries and `totalPush` 1131.
- `packages/content`: loader, validator, generated types. Every check in
  `CONTENT_SCHEMA.md` §12 fails a fixture built to trip it.
- `packages/manifest`: loader, validator, `overhang` recomputation, coverage report.
- The greybox renderer: one screen, one entry, one labelled rectangle at exact size,
  tone from the palette, at 2× and 3×.
- CI stages 1–5 from `ARCHITECTURE.md` §9.2.

**Question answered:** does the foundation hold? Two things must be true before
anything is built on them: that the sim is deterministic across at least two
independent runs of the fixture set, and that bad content and bad manifests cannot
reach the renderer.

**Exit criteria**

- All sim fixtures hash-match on two machines.
- `validate-content` and `validate-manifest` run green on the committed files and red
  on each rejection fixture.
- A greybox screenshot fixture exists and matches.

**Throwaway:** the fixture-picker debug page. **Carries forward:** everything else;
the sim package is the one that the ranked server runs unchanged.

**Human:** none required. A good moment to buy the packs and do the pack-fit pass
(`ART_PIPELINE.md` §15), which is independent of all of this.

---

## 3. M1 — The fight

**Playable:** pick two scripted rivals from a debug list, watch them fight on the
battle screen, scrub the autopsy.

**Systems**

- The battle screen as specified in `GAME_DESIGN.md` §19.2: the isometric towers as
  greybox segments, window bursts, floating numbers, the Market Share bar, both
  Goodwill bars with eroding frames (D-35), the month banners, the founder badges,
  playback at 1× / 2× / 4× / skip.
- The live ledger with coalescing and the four-lines-per-second budget (D-08).
- The floor inset on hover.
- The autopsy screen: timeline, per-floor bars, filtered full ledger, the three
  findings.
- The result banner and the draw rule.

**Question answered:** is a fight readable? Specifically: can a person watching a
late-round fight between two authored towers say, without the autopsy, roughly why the
winner won — and with it, exactly why? This is the design's central promise and it is
answered here, before a single build-phase feature exists, because if the answer is no,
nothing built on top of it matters.

**Exit criteria**

- The Parent Company against the Compliance Office renders at 60 fps at 3×.
- The ledger never exceeds its line budget on any scripted matchup (measured, not
  eyeballed).
- A human answers the first two questions in `GAME_DESIGN.md` §21 — ledger
  readability and whether sixty seconds feels long — and the answers are recorded.

**Throwaway:** the debug rival picker. **Carries forward:** the battle screen, the
ledger, the autopsy, playback — all of it is the ranked spectator view too.

**Human:** the two readability judgements. This is the first place a design problem
can be found that agents cannot fix, and it is deliberately early.

---

## 4. M2 — The loop, the vertical slice

**Playable:** found a firm, build a tower, press Ready, watch it fight a templated
rival, read the autopsy, build again. Sixteen rounds, three strikes, a run summary. No
map, no interludes, no portal, no recipes yet.

This is the **earliest point at which the game is fun**, and the order of everything
before it was chosen to reach it. It is also, deliberately, the ranked-shaped loop:
sixteen rounds in sequence against rivals of the same round, which is exactly what
ranked will be with ghosts substituted for templates (D-49).

**Systems**

- The build screen as specified in §19.1: the three-floor viewport, the shop with its
  three tabs, placement with aura badges and link lines, the inspector and the firm
  panel, the top bar, the hint line.
- The build reducer with its full action set and action-log undo (D-43). Ready
  commits and snapshots.
- The economy: income, prices, severance, renovation fees, leases and upkeep, the
  Goodwill-for-unpaid-upkeep rule (D-31).
- Rooms as zones with auras; floors with their multipliers and legality; the landing
  column; the corridor penalty.
- Furniture placed and drawn, with its effects live — it is on trial (D-34) and the
  trial needs it present.
- The template expander (D-39) generating each round's rival; the dossier is not yet
  needed because there is no map, so the rival's name and archetype show in the top
  bar.
- Founder select (D-46).
- Strikes, the run summary, and a run that ends.

**Question answered:** is the game fun? Is a build round interesting on its own —
does the player face a real decision between a room and a hire, between leasing and
staffing, between Ready and one more reroll — and does the fight that follows feel
like the consequence of it?

**Exit criteria**

- Twenty full runs by the developer, in greybox.
- Every question in `GAME_DESIGN.md` §21 that is answerable at this point has a
  recorded answer, including the furniture trial (Q-LYR-3): the trial is scored here,
  and the fold-in plan is executed in M3 if it fails.
- `inv.fight_length`, `inv.bar_moves_early` and `inv.archetype_band` pass on smoke
  for the six templates at rounds 1, 6, 12 and 16 — the harness exists in minimal
  form by the end of M2 so that the runs above are not the only evidence.
- The UX pass (Q-UX-1, D-48): the developer's notes on the build and battle screens
  after twenty runs, turned into layout changes or `OPEN_QUESTIONS.md` entries.

**Throwaway:** the top-bar rival label (replaced by the dossier in M4). **Carries
forward:** every system above; the linear loop *is* ranked's loop.

**Human:** the twenty runs and the judgements. This milestone cannot be exited by an
agent. If the answer to "is it fun" is no, the correct next milestone is not M3 — it
is a return to `GAME_DESIGN.md` with the notes, and this roadmap is rewritten from M2
down.

---

## 5. M3 — Depth

**Playable:** the same loop, now with recipes to discover, a codex to fill, Tenure
that compounds, statuses fully live, and rivals that carry gimmicks from round 8.

**Systems**

- Recipes: pattern detection after every placement, the Promote glyph, the near-miss
  flicker, consumption and undo; all forty recipes with a fixture each.
- The codex screen with progressive reveal; discoveries persisting to the profile.
- Tenure ticking on Ready, tiers applied in the sim, pips on the sign.
- The four statuses end to end, including Overtime's hangover and the Burnout Morale
  tick — M2 had them in the sim; M3 makes every source and cleanse in the catalogue
  live and visible in the inset.
- Gimmicks on templated rivals from round 8, and the dossier — surfaced in M3 as a
  pre-fight panel, moved onto the map node in M4.
- The furniture verdict from M2 executed: either the catalogue extends, or every
  furniture effect folds into rooms and the entries are removed (Q-LYR-3).
- The harness in full: every population, every invariant, the nightly report, the
  optimizer. `BALANCE_PLAN.md` §6 is green on nightly or each red row has an owner.

**Question answered:** does commitment pay? A room held for ten rounds should be
felt as an advantage, not merely seen as a pip; demolishing one should be a decision;
and recipes should feel like discoveries rather than a lookup. The harness answers the
part of this that is measurable — `inv.demolition_rare` — and the human answers the
rest.

**Exit criteria**

- All seventeen invariants implemented; nightly green, or red rows owned.
- The Tenure and demolition questions in §21 answered.
- Twenty more developer runs with recipes live; the codex fills at a rate that feels
  like discovery, recorded as the count of recipes found per run.

**Throwaway:** the M3 pre-fight dossier panel (becomes the map hover). **Carries
forward:** all of it. Recipes, Tenure, statuses and the harness are mode-independent.

**Human:** the feel judgements; naming the owner of any red invariant.

---

## 6. M4 — The campaign

**Playable:** the campaign as `GAME_DESIGN.md` §15 describes it, start to finish.

**Systems**

- The map screen, branching acts from `content/map.json`, node kinds, the dossier on
  hover, the marker.
- Interludes: Recruiter, Board Meeting with its modifier choices, Consultant with the
  reveal and the Tenure grant.
- The three bosses with their signature gimmicks; Audits; the first-run scripted
  fights.
- The portal: the reveal beats from round 1, the Act 1 unlock, B1 leasing with its
  Goodwill cost, the Otherworld row with riders, the Summoning Circle, rituals.
- The tutorial hint line for the first run.
- Save and load: `RunState`, `ProfileState`, replays; atomic writes; the first
  migrator and its fixture; determinism across save and load (`ARCHITECTURE.md` §5).
- Draws as loss-without-strike; win bonuses by node kind.

**Question answered:** is a 45-minute run the right shape? Do three acts escalate,
does each boss teach its lesson (`inv.boss_counters` measures it; a human confirms
it), does the portal reveal land, and does a lost run end soon enough to want another?

**Exit criteria**

- A full campaign is playable in greybox from founder select to the Parent Company.
- `inv.boss_counters` green.
- The portal reveal, the Bell erasure and the Promote-as-discovery questions in §21
  answered.
- A save made at every node kind loads and reproduces its shop and its next rival.

**Throwaway:** nothing significant. **Carries forward:** the map layer is
campaign-only by design, but it carries as the campaign; the portal, bosses, riders,
rituals and save system are mode-independent; the scripted rivals become ghost seeds.

**Human:** the run-shape judgement. This is the second milestone an agent cannot exit.

---

## 7. M5 — Ship-ready

**Playable:** the same game, installed from a Steam build, by someone who has never
seen it.

**Systems**

- Meta-progression and unlocks (`GAME_DESIGN.md` §17); the profile complete.
- The `Platform` interface with `SteamPlatform`: Cloud sync of profile and run,
  achievements from `content/achievements.json` (authored here), overlay.
- Settings: scale, fullscreen, volume placeholders, telemetry opt-in.
- The audio facade replaced by a minimal real implementation — a handful of UI and
  ledger sounds — or explicitly shipped silent with the facade in place. Either is a
  decision recorded in the log; silent is acceptable in greybox and the roadmap does
  not gate on it.
- Performance budgets asserted (`ARCHITECTURE.md` §11); the render list profiled at
  the Parent Company fight.
- Screenshot fixtures for every screen in every state.
- The release workflow: Tauri bundles on three platforms, `steamcmd` depots, the art
  and licence gates wired (D-44).
- Local telemetry (`BALANCE_PLAN.md` §10) and `tools/telemetry`.
- A crash and corrupt-save recovery path that has been exercised.

**Question answered:** does it survive a stranger? Not "is it good" — that was M2 and
M4 — but does it install, launch, save, resume, and fail gracefully on a machine the
developer has never touched.

**Exit criteria**

- A Steam build installs and runs a full campaign on all three platforms.
- The nightly matrix has been green for a stretch long enough to trust it.
- Three people who are not the developer have played a run from a build, in greybox,
  and their runs' telemetry is in the report.

**Throwaway:** none. **Carries forward:** all of it.

**Human:** the strangers.

---

## 8. Release gates

Tracked on their own lines from the first commit. None is a milestone dependency;
all must be green to ship.

| Gate | What green means | Tracked by |
| --- | --- | --- |
| **Art complete** | Every `releaseGate: true` manifest entry has a validated asset; nothing renders in the `invalid` tone in a full playthrough capture; the perspective rule holds; every `verify` flag cleared (`ART_PIPELINE.md` §8) | `art_coverage.json` on every build; the README badge; the release workflow |
| **Licence** | `packs/guttykreum/LICENSE.md` records a positive verdict for commercial use and in-game redistribution of the complete edition (Q-RISK-1) | The release workflow's licence gate |
| **Balance** | The full nightly matrix green, with no invariant row owned-but-red, for the release candidate's content version | `balance_report.json` |
| **Determinism** | Sim fixtures hash-match on the release build on all three platforms | CI on the release tag |
| **Strangers** | M5's three outside runs completed on a release candidate | Telemetry |

Art is the gate that will close last, and the design accepts that: the campaign is
fully playable in greybox at M4, and the ranked worklist (`ART_PIPELINE.md` §7.3)
exists so that the assets that carry the most perceived polish land first. Nothing
above M0 ever waits for it.

**Not a v1 gate:** Steam Deck verification. Controller navigation is post-v1 (D-47),
so the Deck compatibility badge is a post-v1 goal. The build runs on a Deck at 2× with
mouse and keyboard; it is not *verified* for it.

---

## 9. Post-v1

In the order they are worth doing, each additive by construction:

| Item | Why it is additive | Reference |
| --- | --- | --- |
| **Ranked** — `services/ranked`, ghost pool, matchmaking, server re-simulation, rating | `simulate()` is pure, the snapshot is the wire format, the expander is the cold-start fallback, the scripted rivals seed the pool | `ARCHITECTURE.md` §12; D-18 to D-20 |
| **Controller navigation** and Steam Deck verification | Focusable elements were kept enumerable per screen from M2; it is an input adapter | Q-ARCH-1; D-47 |
| **The Anomaly meter** — an escalating portal risk with an "Audit from Below" | A run-level counter and one boss; the portal's rider-and-tax layer stands without it | Q-PTL-1 option B |
| **Founder mechanics** — traits, starting rooms, signature staff | The founder's `effects` list is already in the snapshot and applied like a modifier's | D-46 |
| **Equipment**, if ever | `attachments[]` has been in every snapshot since the first | D-13; explicitly cut for v1 by D-34 |
| **Audio** beyond the minimum, **localisation** | The facade and the content-side strings exist | M5 |
| **Third-party packs** through the portal fiction | The absorption process is written | `ART_PIPELINE.md` §14 |

---

## 10. Throwaway and carry-forward, in one table

Campaign-first was chosen so that the PvP backend is off the critical path and so that
almost nothing built for the campaign is wasted when ranked arrives. The audit:

| Built in | Carries into ranked | Campaign-only, kept | Throwaway |
| --- | --- | --- | --- |
| M0 | sim, loaders, renderer, CI | — | fixture picker page |
| M1 | battle screen, ledger, autopsy, playback | — | debug rival picker |
| M2 | build screen, reducer, economy, rooms, floors, expander, founders, the sixteen-round loop itself | — | top-bar rival label |
| M3 | recipes, codex, Tenure, statuses, harness | gimmicks (config-gated) | pre-fight dossier panel |
| M4 | portal, bosses-as-ghost-seeds, riders, rituals, save system | map, interludes, tutorial, node rewards | — |
| M5 | profile, Steam, packaging, telemetry, fixtures | meta-unlocks (config-gated off in ranked) | — |

Three debug pages and one label. Everything else ships twice.

---

## 11. What the human does

Agents build every system above. The human's list, in milestone order, is short and
none of it can be delegated:

| When | What | Why only a human |
| --- | --- | --- |
| M0 (any time) | Buy the complete edition; read the licence; write `LICENSE.md` | Q-RISK-1. A purchase and a legal verdict |
| M0 (any time) | The pack-fit pass: clear the thirteen `verify` flags; sample the six palette hues | Q-GBX-5, Q-GBX-3. Needs eyes on the packs |
| M1 | Two readability judgements | The design's central promise is judged by watching |
| M2 | Twenty runs; every answerable §21 question; the furniture verdict; the UX notes | "Is it fun" has no harness |
| M3 | Feel judgements on Tenure, demolition, discovery; owning red invariants | Balance decisions are decisions |
| M4 | The run-shape judgement; the portal reveal | Narrative timing |
| M5 | Find three strangers | — |
| Any | Layout problems agents flag; design problems the harness finds | The planning prompt's stated division of labour |

Everything else — the sim, the loaders, the screens, the tools, the content edits,
the fixtures, the harness, the packaging — is agent work against the specifications
in this repository, which were written so that it could be.
