namespace CompanyWars.Playback;

/// <summary>
/// A tick counter advanced by an accumulator — elapsed time times twenty ticks per second, never one tick per
/// frame — at 1x, 2x or 4x, or jumped to the end for skip (ARCHITECTURE.md §3). Integer microseconds so two runs
/// fed the same frame times land on the same ticks.
/// </summary>
public sealed class PlaybackClock
{
    private readonly long _ticksPerSecond;
    private long _accumulatorMicros;

    public PlaybackClock(long ticksPerSecond, long endTick)
    {
        _ticksPerSecond = ticksPerSecond;
        EndTick = endTick;
    }

    public long EndTick { get; }
    public long Tick { get; private set; }
    public int Speed { get; private set; } = 1;
    public bool Paused { get; set; }
    public bool Finished => Tick >= EndTick;

    public void SetSpeed(int speed)
    {
        Speed = speed is 1 or 2 or 4 ? speed : 1;
    }

    public void Skip()
    {
        Tick = EndTick;
        _accumulatorMicros = 0;
    }

    public void Seek(long tick)
    {
        Tick = Math.Clamp(tick, 0, EndTick);
        _accumulatorMicros = 0;
    }

    /// <summary>Advances by wall time; returns how many ticks elapsed.</summary>
    public long Advance(long elapsedMicros)
    {
        if (Paused || Finished || elapsedMicros <= 0) return 0;
        _accumulatorMicros += elapsedMicros * Speed;
        long microsPerTick = 1_000_000 / _ticksPerSecond;
        long ticks = _accumulatorMicros / microsPerTick;
        _accumulatorMicros -= ticks * microsPerTick;
        long before = Tick;
        Tick = Math.Min(EndTick, Tick + ticks);
        return Tick - before;
    }
}
