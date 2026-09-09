# Company Wars — Research Verification Notes

Status: **post-sign-off verification pass** (D-53). Five research agents were run after
the design phase closed, each with a distinct brief: genre practice, the technology
stack, Steam publishing, asset licensing and naming, and agent-driven development
process. This document records what they found, how much to trust each finding, and
what was done about it.

A finding that changes a logged decision is a new decision-log entry, never a silent
edit. A finding that raises a question the human must answer is a register entry. A
finding that only informs implementation is recorded here and, where useful, as a line
in the relevant document.

**Trust levels.** The agents ran behind a restrictive egress proxy. Several primary
sources (Steamworks docs, itch.io pages, GDC Vault, tauri.app) could not be fetched
directly and were read from search-engine summaries. Each finding below is marked
**[S]** sourced from a fetched page, **[s]** sourced from a search summary of the cited
page, or **[I]** inference. Anything marked [s] should be re-read at the source before
it is relied on for money or law.

---

## Contents

1. [Blockers and decision changes](#1-blockers-and-decision-changes)
2. [Genre practice](#2-genre-practice)
3. [Technology stack](#3-technology-stack)
4. [Steam publishing](#4-steam-publishing)
5. [Assets, fonts and the title](#5-assets-fonts-and-the-title)
6. [Agent-driven process](#6-agent-driven-process)
7. [What was changed](#7-what-was-changed)
8. [What the human must do](#8-what-the-human-must-do)

---

## 1. Blockers and decision changes

Three findings are serious enough to name first.

**The Steam overlay does not work in Tauri.** [S] The overlay hooks the game process's
graphics present call; WebView2, WKWebView and WebKitGTK render in a separate GPU
process it cannot reach. Tauri issue #6196 was closed "not planned". A 2026
workaround exists — a transparent decoy swapchain the overlay can hook
(`tauri-steam-overlay-surface`, Windows; a macOS sibling) — but it is two months old,
single-maintainer, and judged unsolvable on Linux. Consequences: no Shift+Tab overlay,
no achievement toasts, and on Steam Deck **no on-screen keyboard**, because the OSK is
drawn by the overlay. The overlay is not a Deck Verified criterion; the OSK is.
→ **Q-TECH-1** opened. The architecture already isolates Steam behind one interface
(D-45), so the cost of the answer is contained; the answer itself is the human's.

**Linux and Steam Deck are unverified.** [S/I] Tauri requires the system's
`libwebkit2gtk-4.1` and cannot bundle it. Whether SteamOS or the Steam Linux Runtime
provides it could not be confirmed. Under Proton a fresh prefix has no WebView2, so a
native Linux depot is the only Deck route, and it is the unverified one. Code:
Terraform ships a Tauri build on Steam and its Linux users hit the missing-library
error. → part of **Q-TECH-1**; a one-day spike at M0 answers it.

**Three strikes over sixteen fights is far harsher than the genre.** [S] Backpack
Battles and Super Auto Pets end at ten wins or five losses (a ~70% win floor over at
most fourteen fights). Three strikes over a fixed sixteen means at most two losses —
an 87% floor. The Bazaar softens further with day-scaled loss cost and a
"blessing" the first time prestige hits zero. → **Q-STR-5** opened with a
recommendation of five strikes.

---

## 2. Genre practice

Sources: Backpack Battles wiki and Steam discussions, TFT dev blogs and the GDC 2020
talk summary, Hearthstone Battlegrounds season notes, Super Auto Pets patch notes, The
Bazaar guides, Slay the Spire GDC 2019 summary, Astronarch and Backpack Hero reviews.
Most were [s]; the GDC transcripts themselves were not reachable.

| Finding | Trust | Bears on | Action |
| --- | --- | --- | --- |
| Reference games use ~10 wins / 5 losses; our 3 strikes over 16 demands an 87% win rate | [S] | D-21 | Q-STR-5 |
| Fights are forced to conclude from 17s (BB fatigue) or 30s (Bazaar sandstorm); genre norm 30–60s; the Bazaar's un-skippable length is its most repeated complaint | [S] | D-21, D-05, §21 | Kept 60s ceiling and the 35–50s median target; playback speed controls stay (the Bazaar refused them and paid for it). Watch at M1 |
| SAP's cheap uncapped rerolls generate "same pets again" complaints; shared pools and pity systems are the mitigations | [s] | Shop | **D-54**: shop draws from a per-tab bag without replacement |
| TFT: passive interest must not outpay active spending or the game solves toward turtling; Tenure is interest-shaped | [s] | D-25 | **D-57**: `inv.standing_pat_loses` |
| HS Battlegrounds' stepped damage cap (5/10/15 by turn) stopped early snowballs; The Bazaar's blessing-at-zero is the single-player equivalent | [S] | Goodwill scaling, strikes | Goodwill cap already scales by round; strikes → Q-STR-5 |
| BB's three-layer recipe disclosure — silhouette, a compatibility hint line between owned items, a recipe sheet — is the model players ask for; some want a veteran toggle | [S] | D-17, codex | Compatibility glow added to §13.4; toggle noted as post-v1 |
| BB players still request ledger filtering by source, per-item post-fight totals, step scrubbing and unambiguous attribution | [S] | D-08, D-33 | All four already in the autopsy; attribution is the source/target model |
| Ghost pools: BB matches on rounds + W/L only and players cannot gauge build strength; The Bazaar replaces beaten ghosts with the winner so the pool drifts harder | [s] | D-19 | Noted in ARCHITECTURE §12: match on round + rating + a build-strength score; decay the pool |
| Backpack Hero is criticised for boss-required drops not being guaranteed | [s] | §15 | Already handled: the Act 2 Consultant always offers a Morale recipe |
| TFT's costliest lessons came from content that forced core-system re-tuning | [s] | D-50 | Confirms the not-a-knob list |
| No quantitative data on build-phase duration; SAP and BB cite "no timer" as a retention feature | [S] | D-17 | Confirms untimed build phase |

Gap: the cold-start seeding of Backpack Battles' ghost pool was not documented anywhere
found. Our answer (scripted rivals seed the pool, templates as fallback) stands on its
own reasoning.

## 3. Technology stack

Sources: Tauri and WebView2 issue trackers, the overlay-surface plugin README,
steamworks-rs issues, PixiJS issues and discussions, TC39, bryc's PRNG survey,
Playwright issues, Tauri's Vite docs.

| Finding | Trust | Bears on | Action |
| --- | --- | --- | --- |
| Overlay and Deck OSK unavailable in Tauri; decoy-swapchain plugin is young; Linux unsolvable | [S] | Locked decision 10, D-45 | Q-TECH-1 |
| SteamOS / SLR availability of `libwebkit2gtk-4.1` unknown; Tauri cannot bundle it | [S/I] | Linux depot, Deck | Q-TECH-1 spike |
| Electron is meaningfully safer for Steam: one Chromium renderer, no system webview, mature overlay paths (`steamworks.js`), at ~100 MB and Steamworks moving into Node | [S] | Stack | Q-TECH-1 option |
| macOS 13–15 WKWebView caps `requestAnimationFrame` at 60 Hz; WebView2 may run uncapped | [S] | Playback | ARCHITECTURE §3: playback clock is accumulator-based, tolerant of 60/120/144 Hz |
| `steamworks` crate 0.13 tracks SDK 1.80, actively maintained; Linux needs `$ORIGIN` rpath for `libsteam_api.so`; use `GetAuthTicketForWebApi` for a web-API server, not `GetAuthSessionTicket` | [S] | D-45, §12 | Both noted in ARCHITECTURE |
| Valve recommends the Cloud API with write batches over Auto-Cloud for control; Dynamic Cloud Sync loses data if the game does not reload on the file-changed callback | [s] | ARCHITECTURE §5, §8 | Noted |
| PixiJS 8: set `TextureStyle.defaultOptions.scaleMode = 'nearest'` before any texture is created; `roundPixels` had rounding regressions (8.0.4); own the integer snapping; prefer `'webgl'` because WKWebView and WebKitGTK lack default WebGPU | [S] | D-26, §7 | Noted in ARCHITECTURE §7 |
| Integer arithmetic below 2^53 is bit-identical across engines; `Math.imul` and `>>> 0` for 32-bit; ban `Math.pow`, float division, `Math.sqrt` in the sim | [S] | D-27 | Confirms; ban list noted for the sim package's lint |
| Stable sort is guaranteed since ES2019 but ties still depend on input order; always break by id | [S] | D-15, §8.2 | Already done (trailing `id` key) |
| bryc's survey: sfc32 and mulberry32 rank best; xoshiro128** has weak low bits; mulberry32 skips some outputs | [S] | D-28 | Kept mulberry32: one u32 of state in the hash, and two selectors do not need statistical quality |
| Rollback projects verify determinism with per-frame FNV-1a checksums and "first diverging frame" reports | [S] | §16.3 | Confirms; a cross-engine harness (Node, WebView2, WKWebView, WebKitGTK) noted for M0 |
| `eslint-plugin-boundaries` silently did not fire in pnpm workspaces before 5.4.0; `dependency-cruiser` for CI | [S] | D-42 | ARCHITECTURE §1: pin ≥5.4, add a deliberate-violation test |
| `json-schema-to-typescript` maintained but weak on 2020-12 tuples; TypeBox is the alternative when schemas are authored in TS | [S] | §6 | Ours are authored as JSON; keep, note TypeBox |
| Playwright WebGL screenshots: enable GPU flags or xvfb; baselines from a pinned Docker image | [S] | §7.5 of the pipeline | Noted |

## 4. Steam publishing

Sources: Steamworks documentation [s], Steam Deck programme pages [s], Next Fest event
pages [s], `steam-deploy` README [S], pricing of comparable titles [s].

| Finding | Trust | Bears on | Action |
| --- | --- | --- | --- |
| Hard timelines: 30 days from paying the fee before release; Coming Soon page public ≥2 weeks; store and build review 3–5 business days each, plan 7 | [s] | ROADMAP | Encoded in the T-minus table |
| **Store assets need final-quality art months before release**: five real gameplay screenshots, capsules at the 2023 sizes, a trailer. Greybox screenshots on a Coming Soon page hurt wishlists | [s/I] | D-16, D-44 | **D-56**: a "store-ready" gate on tiers 1–2, ahead of "art complete" |
| Next Fest is one participation per title ever; the February 2027 edition needs a demo and page by 25 Jan 2027, and release after 1 Mar 2027 | [s] | ROADMAP | Noted; a demo build is now a named roadmap item |
| Deck Verified needs full controller support, a default config, and OSK via the API; a keyboard-and-mouse game gets Playable at best; text ≥9 px absolute, 12 px recommended at 1280×800 | [s] | D-47 | §19: no required text input (**D-55**); 8 px font at 2× is 16 px — fine |
| Cloud: quota and file count set in Steamworks; a JSON profile plus one run is trivial; Dynamic Cloud Sync needs reload-on-change | [s] | §5 | Noted |
| Achievements: 256×256 icons; overlay-drawn toasts will not appear in Tauri | [s] | §8 | Noted under Q-TECH-1 |
| Linux runtime is self-selected; SLR 4.0 (steamrt4, Debian 13) is Valve's recommendation for new native Linux games since Nov 2025 | [s] | §9 | Noted; the Deck spike should use steamrt4 |
| macOS builds must be notarized with two entitlements; Windows signing is not required by Steam | [s] | §9 | Noted |
| Launch discount ≤40%; no other discounts within 30 days; review score shows at 10 reviews | [s] | Post-v1 | Noted |
| Comparable prices: Backpack Battles $14.99, Despot's Game $19.99, He Is Coming $19.99, The Bazaar $39.99 premium; band $14.99–19.99 | [s] | Not in scope of the prompt | Recorded for later |
| No exact "Company Wars" title on Steam; nearest are "Store Wars", "Business Wars", "STAR WARS Zero Company"; "The Company War" is a dormant 1983 board game | [s] | Title | Q-RISK-3 |

## 5. Assets, fonts and the title

Sources: itch.io pack pages [s], a 2021 creator thread [s], font repositories [S],
Chevy Ray's and Somepx's licence pages [S/s].

**GuttyKreum licence — CLEAR WITH CONDITIONS.** The Japan Collection pages carry a
common licence: commercial use explicit; derivative works permitted; use in any number
of projects; but the purchaser may not redistribute the assets "other than as part of
the relevant Media Product" nor "allow the user of the Media Product to extract" them.
The creator confirmed commercial use in writing (2021) and that editing is fine, and
said an open-source game whose art could be reused would not comply. The Backgrounds
page says credit is required; it is unconfirmed whether the tile packs say the same.
No AI clause was found on GuttyKreum's own pages (one that surfaced belongs to an
unrelated itch blog post). The complete edition bundle grants each pack's own licence.

Conditions adopted: ship only packed atlases inside the binary, never the raw sheets or
a mod kit exposing them; credit GuttyKreum for every pack in the credits; never
open-source the art. Two questions to put to the creator at purchase: whether credit is
required on the tile packs or only Backgrounds, and whether heavy recolours are fine.
→ Q-RISK-1 updated.

**Pixel fonts.** m5x7 (Daniel Linssen, CC0) is a 5×7 proportional face that matches
the `font.ui.8` specification exactly. Pixel Operator (CC0) and Silkscreen (OFL) are
alternatives; Press Start 2P is monospace; Somepx caps at $1M turnover; Chevy Ray's
pack forbids easy extraction. → m5x7 recorded as the candidate in ART_PIPELINE §13,
to be confirmed when the digit widths are checked (tabular digits are a requirement).

**Title.** No direct collision found on Steam, itch.io or mobile stores; registry
searches (USPTO, EUIPO) were not reachable. "Wars" marks are policed by Lucasfilm but
"X Wars" titles routinely survive. A free knock-out search in classes 9 and 41 is the
next step; an attorney search is ~$150–500. → Q-RISK-3.

**Asset-flip perception.** Valve has no rule against purchased art; community norms
are: integrate and modify, do original UI and characters, credit the source, and do not
use the pack's own preview shots on the store page. The extraplanar recolours and the
firm's own UI are the visible originality; the store page should lead with them.

## 6. Agent-driven process

Sources: Anthropic's Claude Code documentation [S], the ImpossibleBench paper [S], a
shipped agent-built game write-up [s], dependency-cruiser and paths-filter READMEs [S].

| Finding | Trust | Action |
| --- | --- | --- |
| Keep `CLAUDE.md` under ~200 lines (ideally ~100), and only what Claude cannot derive: commands, conventions that differ from defaults, gotchas, decisions | [S] | `CLAUDE.md` added |
| Per-package `CLAUDE.md` files load on demand; a routing index beats importing docs | [S] | `docs/INDEX.md` added; per-package files come with the packages |
| Agents modify tests to pass them; hiding or read-only tests cut this to near zero; an explicit "stop and report if the spec and the test disagree" option cuts it further | [S] | A `PreToolUse` hook blocks edits under `fixtures/`; the rule is in `CLAUDE.md` |
| Hooks are deterministic, `CLAUDE.md` is advisory | [S] | Fixture protection is a hook, not a sentence |
| A solo developer cannot approve their own PR, so CODEOWNERS is weak; a status check requiring a human-applied label on fixture changes works | [I] | ARCHITECTURE §9.2 step added |
| A shipped agent-built game relied on a headless simulation matrix as a merge gate | [s] | Confirms the harness-as-required-check design |
| Anthropic's long-running-agent harness: one feature per session, a JSON task list, a progress file | [s] | Noted for M0: the roadmap's milestones become a task list when implementation starts |

## 7. What was changed

| Change | Where | Log |
| --- | --- | --- |
| Shop draws from a per-tab bag without replacement | `content/shop.json`, GAME_DESIGN §5.1 | D-54 |
| No screen may require text input; the firm name field is optional with a generated default | GAME_DESIGN §19.7 | D-55 |
| A store-ready art gate on visibility tiers 1–2, ahead of art complete | ROADMAP §8, ART_PIPELINE §8 | D-56 |
| `inv.standing_pat_loses` in the harness | `content/balance.json`, BALANCE_PLAN §6 | D-57 |
| Compatibility glow on hover in the shop and codex | GAME_DESIGN §13.4 | note |
| Playback clock, Pixi settings, lint versions, Cloud API, web-API tickets, matchmaking strength score and pool decay, the M0 cross-engine determinism check | ARCHITECTURE §1, §3, §5, §7, §8, §12 | notes |
| M0 gains a stack spike; a demo build is a named item | ROADMAP §2, §9 | note |
| m5x7 as the font candidate | ART_PIPELINE §13 | note |
| `CLAUDE.md`, `docs/INDEX.md`, `.claude/settings.json` fixture hook | repository root | — |

## 8. What the human must do

In addition to the list in `ROADMAP.md` §11:

1. **Answer Q-TECH-1** — stay on Tauri and accept no overlay while a one-day M0 spike
   verifies Deck, or switch to Electron now. The architecture isolates the choice; the
   roadmap cannot start M0 without it.
2. **Answer Q-STR-5** — five strikes, or keep three.
3. **At purchase**, read the licence on the store page (not a search summary), ask
   GuttyKreum the two questions above, and write `packs/guttykreum/LICENSE.md`.
4. **Run the free USPTO and EUIPO knock-out searches** for "Company Wars" in classes 9
   and 41 before the store page exists (Q-RISK-3).
5. **Decide on Next Fest February 2027** as a target, which fixes a demo by 25 January
   2027 and a release after 1 March 2027 — or let it go and take a later edition.

---

## 9. Stack selection (after D-59)

Tauri was dropped (D-59). Three further research passes evaluated the field against the
six constraints the design already fixes — pixel discipline; a headless deterministic
sim that runs in the game, in CI at ten thousand matches a minute, and later on a
server; JSON content and manifest with a greybox renderer and screenshot tests; Steam
on three platforms and the Deck with overlay, Cloud and auth tickets; agent-driven
development; JSON saves. The three passes were briefed separately (engine fit, Steam
integration per binding, agent-friendliness) and reached the same first choice
independently.

### 9.1 The field, scored

Scores 1–5 from the engine pass, adjusted where the other two passes disagreed.

| Constraint | Godot 4 + C# | Godot 4 + GDScript | MonoGame / FNA + C# | Unity + C# | Bevy (Rust) | Electron + PixiJS | LÖVE (Lua) |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Pixel art | 5 | 5 | 4 | 4 | 4 | 4 | 4 |
| Headless sim in CI and on a server | 5 | 2 | 5 | 4 | 5 | 4 | 3 |
| JSON, greybox, screenshots | 4 | 4 | 4 | 3 | 4 | 5 | 3 |
| Steam and Deck | 4 | 4 | 4 | 5 | 3 | 2 | 4 |
| Agent-friendly | 4 | 3 | 5 | 2 | 2 | 4 | 3 |
| JSON saves and migrations | 5 | 4 | 5 | 5 | 5 | 5 | 3 |
| **Total** | **27** | 22 | **27** | 23 | 23 | 24 | 20 |

The decisive column is the second. The sim must be a plain library that runs without
the engine — that is what makes the balance harness and the later server possible —
and only a language with a first-class standalone runtime satisfies it. C# and Rust
do; TypeScript does with integer discipline; GDScript can only run inside a headless
Godot binary; LuaJIT's doubles need guarding on every multiply.

### 9.2 Why each of the others loses

| Candidate | Trust | Why not |
| --- | --- | --- |
| **Electron + PixiJS** | [S] | The same failure that removed Tauri, slower: `steamworks.js` has open "no overlay" issues on all three platforms and its Linux one is unresolved; an Electron 35 regression kills the overlay on SteamOS until reboot; Vampire Survivors left Electron for Unity to get Deck Verified. Nobody has shown the overlay in Deck gaming mode from Electron. It keeps our TypeScript, and that is not enough |
| **Unity** | [S/s] | Best Steam story of all, worst agent story: scenes and prefabs are YAML that practitioners tell agents never to touch, and the official MCP needs a live editor. Licensing is repaired but the trust cost is real. A shipped agent-built Unity game was not found |
| **Bevy** | [S] | The best sim language and the worst API churn — a breaking release every three to six months, an open Bevy issue proposing versioned agent instructions because "the machine is always working with outdated knowledge", and an open bug where the screen only repaints a few times a second under the Deck's gaming-mode compositor |
| **Godot + GDScript** | [S/s] | The most shipped agent-built evidence of any engine, and the wrong language for constraint two: the sim would live inside a 90 MB engine binary on CI and the server, with no engine-free test runner. Godot-3-versus-4 API confusion is the top reported agent error class and fails silently at runtime |
| **LÖVE** | [S] | LuaJIT numbers are doubles with 32-bit bit-ops; workable for permille math but every multiply needs a guard; the server would need the same LuaJIT; LÖVE 12 is two years late |
| **raylib** | [s] | Strictly dominated by MonoGame for C# |

### 9.3 The recommendation

**Godot 4 (4.6 or 4.7) with C#, the simulation as a plain .NET class library that
references no Godot assembly, the Compatibility (OpenGL) renderer, the X11 display
driver on Linux, GodotSteam for Steamworks.** Trust: [S] on every load-bearing claim
except the overlay-on-SteamOS state, which is [s] from an open issue.

What each part buys:

- **C#** is on par with Java and ahead of TypeScript and Rust in real-repository
  agent benchmarks, and it turns the Godot-3-versus-4 hallucination class into a
  compile error rather than a silent no-op. Integer arithmetic is fully specified.
- **The sim as a class library** with no `Godot.NET.Sdk` reference means `dotnet test`
  and the ten-thousand-match harness run in CI with no engine, and the same DLL is
  referenced by the Godot client now and an ASP.NET server later. A working open-source
  project (Klotho) demonstrates exactly that topology: engine-agnostic C# core, Godot
  adapter, dedicated server as a plain console app, hash checksums, no floats.
- **Godot** gives the human an editor for layout and feel, text scene files agents can
  read and diff, a headless CLI, two test frameworks that run in GitHub Actions, three
  active MCP servers, and the best Deck track record of any engine a solo developer
  reaches for: Brotato, Dome Keeper, Halls of Torment, Buckshot Roulette.
- **GodotSteam** covers the full SDK 1.65 including `getAuthTicketForWebApi` for the
  future server, releases monthly (it moved from GitHub to Codeberg on 4 September
  2026), and ships headless-exportable builds for all three platforms.

The trade-offs accepted:

- **The overlay path must be pinned.** Godot's Vulkan renderer does not get the
  overlay when launched outside Steam (closed as not planned), and two 2026 issues
  report it missing on SteamOS with the Forward+ renderer and under Wayland. A 2D
  pixel game loses nothing on the Compatibility renderer, and X11 is the known-good
  driver. This is the one live risk and it is a settings choice, verified by the
  spike below, not an architectural one.
- **Screenshot tests need a virtual display.** `godot --headless` renders nothing;
  the off-screen proposal is still open. Tests run under xvfb with Mesa's software
  rasteriser, which the gdUnit4 action already does.
- **Export size is roughly 100 MB** with the .NET runtime embedded. Acceptable.
- **Godot's C# has no web export.** Irrelevant for Steam.

**The close second is MonoGame 3.8.5 with C#**, taking the same sim library verbatim.
It has the smallest surface for an agent to hallucinate against — pure code, an API
stable since 2010, no editor state — and FNA titles like Celeste are Deck Verified on
native Linux. It loses the editor, which is where the human does layout and feel, and
every screen becomes hand-built code; with a manifest driving every pixel that is
smaller than it sounds. It is the fallback if the Godot overlay spike fails.

### 9.4 What stays and what changes

Nothing in the sim spec, the content schema, the manifest, the greybox workflow or
the balance plan named a language; all of it stands. `SIMULATION_SPEC.md` §2 and
§17 are written in language-neutral integer terms and port to C# `long` and
`Math.imul`-free 32-bit code directly; the mulberry32 listing becomes `uint`
arithmetic. `ARCHITECTURE.md` §2, §7, §8 and §9 are rewritten once the choice is
signed off: the package graph becomes .NET projects (`CompanyWars.Sim`,
`CompanyWars.Content`, `CompanyWars.Manifest`, `CompanyWars.Build`, the Godot
project, `CompanyWars.Harness`), the import-direction lint becomes project-reference
rules, TypeBox and Playwright become `System.Text.Json` with schema validation and
xvfb screenshot capture, and the `Platform` interface stays as designed with a
GodotSteam implementation behind it.

### 9.5 The spike that confirms it

One day, before any package depends on the engine:

1. Export a Godot 4 C# hello-world with the Compatibility renderer and
   `display/display_server/driver.linuxbsd = x11` as a real app-ID build; launch it
   from the Steam client on Windows, macOS and a Deck in **gaming mode**; confirm the
   overlay, an achievement toast and Cloud sync. Repeat with Forward+ and with Wayland
   to document which combinations break.
2. Run the same Linux build inside the steamrt4 container and confirm GodotSteam's
   shared library loads.
3. Reference the sim class library from both a `dotnet test` project and the Godot
   project; run the mirror fixture in both and assert identical hashes; time ten
   thousand matches standalone.
4. Capture a greybox screenshot under `xvfb-run` on an Ubuntu runner twice and on
   two runner images; assert byte identity.
5. Sign and notarise the macOS export from a Linux runner with `rcodesign`; confirm
   Gatekeeper launch and the overlay under Metal.
6. Confirm the .NET version Godot pins matches the intended server runtime.
