# UI-12 / UI-02 / UI-03 / UI-04 / UI-08 — World-first live interface

## Interaction model

The live day is one world scene with two deliberately separate UI layers:

1. **Live overlay layer** — persistent corner essentials, contextual object
   panels, compact hotbar, and time controls. It never replaces the park.
2. **Management layer** — Notebook and Neighborhood Map surfaces opened only on
   demand. These can carry dense information because they are explicitly outside
   moment-to-moment live operation.

There are no global Business, Neighborhood, Observe, or Track Guest tabs. The
stand and visible customers are the primary interaction targets. Pin Customer is
an action inside the selected customer's observation card.

## Persistent HUD

- Top-left: day, clock, weather, temperature.
- Top-right: cash, today's cash delta, local reputation.
- Each persistent line pairs native-resolution type with a compact code-native
  pixel glyph; icons support scanning but never replace the text label.
- Bottom-left: Pause / 1× / 2× / 4× and a small ten-hour day timeline.
- Bottom-center: Stand / Customers / Notebook / Map hotbar.

The hotbar is navigation, not a web tab strip. Its pressed state identifies the
currently open destination and clears when the player returns to the world.
Mouse and keyboard stay equivalent: `1` opens Stand, `2` opens Customers, `N`
opens Notebook, `M` opens Map, and `Escape` returns to the unobstructed world.

Queue, stock, and sales are not global KPIs. Queue pressure is physical bodies
plus a small world-space queue sign. Classic availability is shown on the stand
menu board and repeated in the Stand context panel.

Urgency remains contextual: sold-out/paused state changes the world stock sign
and Stand panel; intervention outcomes use the transient top-center toast. The
corner HUD never grows into an alert dashboard.

## Context contracts

- Stand click opens a compact operations panel with product swatches,
  quantity/freshness, sold-out status, and live interventions.
- Customer click opens an observation card with mood, queue interpretation,
  purchase intent, contextual pinning, and a read-only queue watch action.
- Clicking empty world closes the current object context.
- Notebook and Map open a centered management overlay; returning reveals the live world
  without changing simulation state.

## Product status contract

Inventory is not a permanent dashboard. Selecting the stand opens a narrow
context panel with recognizable lemon/berry pixel glyphs, explicit quantity
(`12 LEFT` / `0 LEFT`), and a separate freshness or selling cue (`FRESH`,
`PAUSED`, `SOLD OUT`). The same authoritative Classic availability drives the
context row and the stand's world-space menu board, so a pause or sell-out is
visible in both places during the same refresh.

Operational details remain one level deeper behind **Inventory details**. This
secondary management overlay shows the opening batch, extra prepared servings,
and unavailable products, then returns directly to the Stand context.

## Time contract

Time buttons call the authoritative `SimulationClock.SetSpeed` API. Render delta
is accumulated by the deterministic fixed-step clock; UI never writes
`GameState.SimTime` directly. The large debug-style “Advance 2 hours” action is
absent. “Close the stand” appears only during the final hour of the ten-hour live
day. The compact overlay marks the operating window (`OPEN 08:00` →
`CLOSE 18:00`), shows the active clock state, and exposes Pause/1×/2×/4× as
mutually exclusive controls. `Space` pauses and resumes the last active speed.

At 17:00 the timeline and frame shift to the closing palette, the state label
becomes `CLOSING`, and the contextual **Close the stand** action appears. Before
that hour, the action occupies no persistent HUD space.

## Acceptance mapping

- UI-12: full-width KPI/navigation chrome removed; world fills the frame.
- UI-02: only continuously necessary corner information remains.
- UI-03: object selection and compact hotbar replace web-style tabs.
- UI-04: product state is contextual and mirrored on the world menu board.
- UI-08: compact clock overlay is synchronized to Pause/1×/2×/4× SimClock.
