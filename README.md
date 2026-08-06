# Rulealize.Plugin.State

Reading and writing the state of a [Rulealize](https://github.com/reny-develop/Rulealize)
rule set.

| | |
| --- | --- |
| Plugin id | `Rulealize.Plugin.State` |
| Namespace | `state` |
| Reserved prefix | `$` |
| Depends on | `Rulealize.Abstraction` |

Reads and writes live together because they share one path syntax. Split across two
plugins, the specification of what a path means would live in two places, and neither half
would be much use alone.

## Operations

```jsonc
{ "op": "state.get", "path": "<path>" }                             // shorthand: "$<path>"

{ "op": "state.set", "path": "<path>", "value": <expr> }            // effect
{ "op": "state.update", "path": "<path>", "as": "<name>", "value": <expr> }   // effect
```

`state.get` is an expression; the other two are effects and can only appear in an input's
`effects` array.

## Paths are literal

Dot-separated field names, resolved against `state.schema` when the rule set is built. A
path cannot be computed from an expression.

Three things follow, and they are the reason for the restriction:

- Every path can be checked to exist before anything runs.
- Which fields an input writes to is readable from the document.
- Schema validation does not have to be deferred to evaluation time.

Nesting is reserved rather than usable: `state.schema` is currently a flat map of field
names to schema nodes, so Othello's valid paths are exactly `board`, `turn` and `passes`.

### The inside of a board is not reachable by path

There is no `"board.d3"`. A board is one opaque value as far as this plugin is concerned;
squares are read with `grid.at` and written with `grid.set`.

This is the seam that matters. If paths could reach inside a board, this plugin would have
to know whether boards are sparse or dense and what notation their coordinates use — and
the grid would stop being replaceable.

## Snapshot semantics

`state.get` always reads the state as it stood when the input was applied.

Inside `effects` that means it does **not** see what earlier effects in the same array
wrote. Othello's pass counter is written

```jsonc
{ "op": "state.set", "path": "passes", "value": { "op": "math.add", "of": ["$passes", 1] } }
```

and `$passes` is the value from before the transition, wherever in the array this effect
happens to sit. The same goes for `"#opponent"` in the effect that flips the turn: it is
derived from whose turn it was, not from whatever the array has done so far.

Writes accumulate in a draft and are committed together. Two writes to one field: the last
one wins.

## How another plugin writes through this one

`state.get` implements `IStateLocation`, the contract in `Rulealize.Abstraction` that
carries a resolved `StatePath`.

That is how `grid.set`, handed `"$board"` as its target, finds out which field to write
back to. It cannot inspect the node — it does not reference this assembly — but it can ask
for the interface:

```csharp
ExpressionNode target = context.RequireExpression("target");
if (target is not IStateLocation location)
{
    throw context.Error("target", "must denote a state field.");
}
```

Neither plugin learns anything about the other.

## State documents

```jsonc
{
  "$schema": "rulealize/state/v1",
  "ruleSet": "othello@1.0.0",
  "data": { "board": { "d4": "white", … }, "turn": "black", "passes": 0 }
}
```

The keys under `data` are the fields of `state.schema`. How each field's value becomes JSON
is decided by the schema node that declared it — the sparse board object above is
`grid.board`'s doing, not this plugin's. What this plugin fixes is only the outer frame:
`$schema`, `ruleSet`, `data`.

## Building

`Rulealize.Abstraction` is not on nuget.org yet, so `NuGet.config` points at a folder
feed. Produce it from the abstraction repository first:

```
dotnet pack path\to\Rulealize.Abstraction\src\Rulealize.Abstraction -c Release -o path\to\LocalNuGet
```

with `LocalNuGet` a sibling of this repository. Then `dotnet build`.

## License

Apache-2.0.
