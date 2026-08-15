# Rulealize.Plugin.State

Reading and writing the state of a [Rulealize](https://github.com/reny-develop/Rulealize)
rule set.

| | |
| --- | --- |
| Plugin id | `Rulealize.Plugin.State` |
| Namespace | `state` |
| Reserved prefix | `$` |
| Depends on | `Rulealize.Abstraction` |
| Specification | [doc/specification.md](doc/specification.md) |

`state.get` is an expression, with `"$path"` as its shorthand; `state.set` and
`state.update` are effects. Reads and writes live together because they share one path
syntax — split across two plugins, the specification of what a path means would live in two
places, and neither half would be much use alone.

**Paths are literal**, dot-separated field names resolved against `state.schema` when the
rule set is built, and that is the decision everything else here follows from. Every path
can be checked to exist before anything runs, which fields an input writes is readable from
the document, and schema validation never has to be deferred to evaluation time.

The seam that matters: there is no `"board.d3"`. A board is one opaque value as far as this
plugin is concerned, and its squares are read with `grid.at` and written with `grid.set`. If
paths could reach inside a board, this plugin would have to know whether boards are sparse
or dense and what notation their coordinates use — and the grid would stop being
replaceable.

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

## Building

`dotnet build`. `Rulealize.Abstraction` restores from nuget.org like any other package, so
this repository builds on its own.

[`NuGet.config`](NuGet.config) also adds a folder feed named `LocalNuGet` beside the
repositories — added to nuget.org rather than replacing it — which is how a change to the
abstraction is tried out before it is published. Pack it when you have changed it:

```sh
dotnet pack path\to\Rulealize.Abstraction\src\Rulealize.Abstraction -c Release -o path\to\LocalNuGet
```

## License

Apache-2.0.
