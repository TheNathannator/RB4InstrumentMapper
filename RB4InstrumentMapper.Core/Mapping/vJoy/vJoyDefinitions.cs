namespace RB4InstrumentMapper.Core.Mapping
{
    /// <summary>
    /// vJoy button flag constants.
    /// </summary>
    public enum vJoyButton : uint
    {
        None = 0,
        One = 1 << 0,
        Two = 1 << 1,
        Three = 1 << 2,
        Four = 1 << 3,
        Five = 1 << 4,
        Six = 1 << 5,
        Seven = 1 << 6,
        Eight = 1 << 7,
        Nine = 1 << 8,
        Ten = 1 << 9,
        Eleven = 1 << 10,
        Twelve = 1 << 11,
        Thirteen = 1 << 12,
        Fourteen = 1 << 13,
        Fifteen = 1 << 14,
        Sixteen = 1 << 15
    }

    /// <summary>
    /// vJoy PoV hat constants.
    /// </summary>
    public enum vJoyHat : uint
    {
        // vJoy continuous PoV hat values range from 0 to 35999 (measured in 1/100 of a degree).
        // The value is measured clockwise, with up being 0.
        Neutral = 0xFFFFFFFF,
        Up = 0,
        UpRight = 4500,
        Right = 9000,
        DownRight = 13500,
        Down = 18000,
        DownLeft = 22500,
        Left = 27000,
        UpLeft = 31500
    }
}