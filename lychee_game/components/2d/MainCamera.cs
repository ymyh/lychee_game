using lychee.attributes;

namespace lychee_game.components._2d;

/// <summary>
/// Tag component marking the active main camera entity.
/// Only one entity should have this component at a time.
/// Multiple MainCamera entities is undefined behavior (last one wins).
/// </summary>
[Component]
public partial struct MainCamera;
