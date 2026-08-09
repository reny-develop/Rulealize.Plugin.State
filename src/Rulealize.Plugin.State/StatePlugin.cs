// Copyright (c) 2026 Reny
// Licensed under the Apache License, Version 2.0.

using Rulealize.Abstraction.Plugin;

namespace Rulealize.Plugin.State
{
    /// <summary>
    /// Reading and writing the state, over the <c>state</c> namespace, and the <c>$</c>
    /// shorthand.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reads and writes live together because they share one path syntax. Splitting them
    /// would put the specification of what a path means in two places, and neither half
    /// would be much use alone.
    /// </para>
    /// <para>
    /// This plugin knows nothing about the structure of what it moves. That a field holds a
    /// board, and that a board is a sparse map of coordinates, is the grid plugin's affair.
    /// Keeping that out of here is what makes the grid replaceable.
    /// </para>
    /// </remarks>
    public sealed class StatePlugin : IRulealizePlugin
    {
        /// <inheritdoc />
        public PluginManifest Manifest { get; } =
            new("Rulealize.Plugin.State", new Version(1, 0, 0), "state", '$');

        /// <inheritdoc />
        public void Register(IPluginRegistry registry)
        {
            ArgumentNullException.ThrowIfNull(registry);

            registry.AddExpression("get", StateGetNode.Build);
            registry.AddEffect("set", StateSetNode.Build);
            registry.AddEffect("update", StateUpdateNode.Build);
            registry.AddSugar(new StateGetSugarExpander());
        }
    }
}
