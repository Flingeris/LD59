public class RunState
{
    public int Faith;
    public int Gold;
    public int CemeteryState;
    public int CurrentDay;
    public int CurrentNight;
    public GamePhase CurrentPhase;

    public static RunState CreateInitial()
    {
        return new RunState
        {
            Faith = 0,
            Gold = 0,
            CemeteryState = 100,
            CurrentDay = 1,
            CurrentNight = 0,
            CurrentPhase = GamePhase.Transition
        };
    }
}
