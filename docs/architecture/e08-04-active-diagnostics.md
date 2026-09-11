# E08-04 — Active observation and diagnostics

`OperationalDiagnosticService` is a read-only projection over authoritative
state. It never issues commands, advances time, writes ledger entries, changes
orders, or alters customer decisions.

The player-facing Notebook translates facts into four readable signals:

- Queue: manageable, building, or dominant pressure.
- Price: current offer and observed price-related losses.
- Stock: prepared coverage and sell-out risk.
- Product fit: repeated preference mismatch.

It also exposes the latest lost-customer reasons, a prior-day comparison, and the
currently saturated operational resource. Exact utility values and softmax
formulas remain hidden. These tools explain evidence and severity so a player
can diagnose the dominant problem before choosing an intervention.

Customer context provides **Pin customer** and **Watch queue**. These only alter
presentation focus; simulation outcomes remain untouched.
