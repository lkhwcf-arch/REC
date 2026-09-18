public interface IAnomalyTarget
{
    int TargetId { get; }
    bool IsVisible { get; }
    void SetVisible(bool visible);
}