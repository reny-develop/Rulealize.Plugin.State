// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using Rulealize.Abstraction.Building;
using Rulealize.Abstraction.Evaluation;
using Rulealize.Abstraction.Node;
using Rulealize.Abstraction.Plugin;
using Rulealize.Abstraction.Value;

namespace Rulealize.Plugin.State
{
    /// <summary>Reads a state field from the snapshot.</summary>
    /// <remarks>
    /// <para>
    /// Always the snapshot — the state as it stood when the input was applied. Inside an
    /// input's effects this does not see what earlier effects in the same array wrote,
    /// which is what lets Reversi's pass counter be written as
    /// <c>{ "op": "math.add", "of": ["$passes", 1] }</c> without caring where in the array
    /// it sits.
    /// </para>
    /// <para>
    /// The node implements <see cref="IStateLocation"/>. That is how an effect belonging to
    /// another plugin — <c>grid.set</c>, handed <c>"$board"</c> as its target — discovers
    /// which field to write back to, without either plugin referencing the other.
    /// </para>
    /// <para>
    /// Paths are literal and are checked against the schema when the rule set is built. A
    /// path cannot be computed, and the point of that restriction is to know from the
    /// document alone which fields an input touches.
    /// </para>
    /// </remarks>
    internal sealed class StateGetNode(StatePath path) : ExpressionNode, IStateLocation
    {
        public StatePath Path => path;

        public static ExpressionNode Build(INodeBuildContext context) =>
            Resolve(context, context.RequireString("path"), "path");

        /// <summary>Resolves a state path against the schema.</summary>
        /// <param name="context">The surrounding build state.</param>
        /// <param name="path">The path, without any shorthand prefix.</param>
        /// <param name="propertyName">The property to blame, or null to blame the node.</param>
        /// <returns>The node.</returns>
        public static ExpressionNode Resolve(IBuildContext context, string path, string? propertyName)
        {
            if (!context.State.TryResolve(path, out StatePath? resolved))
            {
                string message = $"'{path}' is not a field of the state schema.";
                throw propertyName is null ? context.Error(message) : context.Error(propertyName, message);
            }

            return new StateGetNode(resolved);
        }

        public override RuleValue Evaluate(IEvaluationContext context) => context.GetState(path);
    }

    /// <summary>Expands <c>"$path"</c> into the node <c>state.get</c> would have built.</summary>
    internal sealed class StateGetSugarExpander : ISugarExpander
    {
        public ExpressionNode Expand(IBuildContext context, string text)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(text);

            string path = text[1..];
            if (path.Length == 0)
            {
                throw context.Error("'$' on its own does not name a state field.");
            }

            return StateGetNode.Resolve(context, path, propertyName: null);
        }
    }
}
