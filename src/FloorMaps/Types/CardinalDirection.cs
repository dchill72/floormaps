namespace FloorMaps
{
    /// <summary>
    /// The four cardinal directions in tile space.
    /// Y increases downward (screen/tile convention).
    /// </summary>
    public enum CardinalDirection
    {
        North,  // -Y (upward on screen)
        South,  // +Y (downward on screen)
        East,   // +X (rightward)
        West    // -X (leftward)
    }
}
