using System;
using MemoryPack;

namespace Game;

[MemoryPackable]
public partial class GameData
{
    // Add game-related save data here
    public int CurrentHealth { get; set; } = 200;
    // TODO add health bar


    // public int FinalKilled { get; set; } = 0;

    // [MemoryPackIgnore]
    // public Action? FinalThingy { get; set; }
}
