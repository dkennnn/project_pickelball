namespace StarterKit.StateMachine
{
    /// <summary>Hợp đồng tối thiểu của một state trong FSM.</summary>
    public interface IState
    {
        void Enter();
        void Exit();
        void Update();
    }
}
