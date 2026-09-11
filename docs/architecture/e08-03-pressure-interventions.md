# E08-03 — Operational pressure interventions

All three interventions cross the same authoritative `DayCommandProcessor` and
`LiveInterventionService` boundary used by headless simulation and Godot. The UI
only issues commands and reads resulting state.

## Trade-offs

- **Emergency restock** is limited to two calls per game day, costs more than
  normal supply, charges cash immediately, and adds sellable servings only when
  the delivery ETA elapses.
- **Call extra help** commits labor cost immediately. Staff capacity increases
  only after the arrival delay and returns to baseline when the temporary shift
  ends; existing work is never teleported or deleted.
- **Mid-day price change** changes the product price while preserving segment
  reference prices. Future customer choices therefore pass through the existing
  price-sensitivity/softmax model: higher prices can improve unit margin while
  reducing conversion and widening the expectation gap.

Global intervention friction and per-kind cooldowns remain active. Each choice
therefore exchanges cash, time, coverage, conversion, or waste risk; none is a
universally dominant answer to operational pressure.

## Trace and economy contracts

Every action writes Trigger, Action, and Consequence trace stages. Emergency
supply is an append-only `Supply` ledger expense and extra help is a `Labor`
expense. Both affect cash reconciliation and daily profit.
