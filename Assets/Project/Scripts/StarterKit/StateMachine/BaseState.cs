namespace StarterKit.StateMachine
{
    /// <summary>Lớp cha tiện dụng cho state, mặc định không làm gì.</summary>
    public abstract class BaseState : IState
    {
        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void Update() { }
    }
}
