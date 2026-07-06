namespace lychee_game;

/// <summary>
/// Exception thrown when a required plugin dependency is missing.
/// </summary>
public sealed class PluginRequirementException(string pluginName) : Exception($"{pluginName} is required");
