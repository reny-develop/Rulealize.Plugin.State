// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using Rulealize.Abstraction.Building;
using Rulealize.Abstraction.Evaluation;
using Rulealize.Abstraction.Node;
using Rulealize.Abstraction.Value;

namespace Rulealize.Plugin.State
{
    /// <summary>Writes a value to a state field.</summary>
    /// <remarks>
    /// <para>
    /// An effect node, so it can only appear in an input's <c>effects</c> array.
    /// </para>
    /// <para>
    /// The value is evaluated against the snapshot and the write accumulates in the draft,
    /// to be committed once every effect has run. Two writes to the same field: the last
    /// one wins.
    /// </para>
    /// <para>
    /// Othello's placement effects end with <c>{ "path": "turn", "value": "#opponent" }</c>,
    /// where the opponent is derived from whose turn it is. Snapshot semantics are what make
    /// that mean the mover's opponent regardless of what any earlier effect did.
    /// </para>
    /// </remarks>
    internal sealed class StateSetNode(StatePath path, ExpressionNode value) : EffectNode
    {
        public static EffectNode Build(INodeBuildContext context)
        {
            string text = context.RequireString("path");
            if (!context.State.TryResolve(text, out StatePath? path))
            {
                throw context.Error("path", $"'{text}' is not a field of the state schema.");
            }

            return new StateSetNode(path, context.RequireExpression("value"));
        }

        public override void Apply(IEvaluationContext context, IStateDraft draft)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(draft);

            draft.Set(path, value.Evaluate(context));
        }
    }

    /// <summary>Writes a state field in terms of its current value.</summary>
    /// <remarks>
    /// <para>
    /// An effect node. The field is read from the snapshot, bound to the name given by
    /// <c>as</c>, and the new value is evaluated under that binding.
    /// </para>
    /// <para>
    /// Nothing here cannot be said with <c>state.set</c> and <c>state.get</c> together; the
    /// gain is only that a long path is not written twice.
    /// </para>
    /// </remarks>
    internal sealed class StateUpdateNode(StatePath path, LocalSlot current, ExpressionNode value) : EffectNode
    {
        public static EffectNode Build(INodeBuildContext context)
        {
            string text = context.RequireString("path");
            if (!context.State.TryResolve(text, out StatePath? path))
            {
                throw context.Error("path", $"'{text}' is not a field of the state schema.");
            }

            string name = context.RequireString("as");
            using (context.Scope.BeginScope())
            {
                LocalSlot current = context.Scope.Declare(name);
                return new StateUpdateNode(path, current, context.RequireExpression("value"));
            }
        }

        public override void Apply(IEvaluationContext context, IStateDraft draft)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(draft);

            IEvaluationContext scope = context.Bind(current, context.GetState(path));
            draft.Set(path, value.Evaluate(scope));
        }
    }
}
