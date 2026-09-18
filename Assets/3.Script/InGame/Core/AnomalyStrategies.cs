using System;
using System.Collections.Generic;

namespace REC.Core
{
    public abstract class AnomalyAction
    {
        public string Name { get; }
        public string ResolveName { get; }
        protected AnomalyAction(string name, string resolveName) { Name = name; ResolveName = resolveName; }
        public abstract void Apply(IAnomalyBody body, MovementSpec movement);
    }

    public class VisibilityAction : AnomalyAction
    {
        private readonly bool visible;
        public VisibilityAction(bool visible) : base(visible ? "ObjectAddition" : "ObjectDeletion", visible ? "ObjectDeletion" : "ObjectAddition") { this.visible = visible; }
        public override void Apply(IAnomalyBody body, MovementSpec movement) => body.SetVisible(visible);
    }

    public class MovementAction : AnomalyAction
    {
        public MovementAction(bool group = false) : base(group ? "ObjectGroupMovement" : "ObjectMovement", group ? "RestoreObjectGroupState" : "RestoreObjectState") { }
        public override void Apply(IAnomalyBody body, MovementSpec movement) => body.Move(movement);
    }

    public class ShadowAction : AnomalyAction
    {
        public ShadowAction() : base("ShadowAddition", "ShadowRemoval") { }
        public override void Apply(IAnomalyBody body, MovementSpec movement) => body.SetShadow(true);
    }

    public class ReplacementAction : AnomalyAction
    {
        public ReplacementAction() : base("ObjectReplacement", "RestoreObjectState") { }
        public override void Apply(IAnomalyBody body, MovementSpec movement) => body.SetReplacement(true);
    }

    public class ActionCatalog
    {
        private readonly Dictionary<string, AnomalyAction> actions = new(StringComparer.Ordinal);
        public ActionCatalog()
        {
            Register(new VisibilityAction(true)); Register(new VisibilityAction(false)); Register(new MovementAction());
            Register(new MovementAction(true)); Register(new ShadowAction()); Register(new ReplacementAction());
        }
        public void Register(AnomalyAction action)
        {
            if (action is null || !actions.TryAdd(action.Name, action)) throw new ArgumentException("행동 이름이 비었거나 중복되었습니다.");
        }
        public AnomalyAction Get(string name) => actions.TryGetValue(name ?? "", out var action) ? action : throw new InvalidOperationException($"지원하지 않는 발생 행동: {name}");
    }

    // 동일 사물의 미해결 사건은 기준 상태 위에 발생 순서대로 다시 적용합니다.
    // 하나를 해결해도 다른 시간대 사건의 효과와 해결 가능 상태는 유지됩니다.
    public class AnomalyRuntime
    {
        private readonly IReadOnlyDictionary<int, IAnomalyBody> bodies;
        private readonly ActionCatalog catalog;
        private readonly List<MissionRuntime> active = new();
        public AnomalyRuntime(IReadOnlyDictionary<int, IAnomalyBody> bodies, ActionCatalog catalog) { this.bodies = bodies; this.catalog = catalog; }
        public void Validate(MissionRuntime mission)
        {
            if (!bodies.TryGetValue(mission.TargetId, out var body)) throw new InvalidOperationException($"Quest={mission.QuestId}: TargetID={mission.TargetId}의 맵 연결이 없습니다.");
            var action = catalog.Get(mission.BeginAction);
            if (action.ResolveName != mission.ResolveAction) throw new InvalidOperationException($"Quest={mission.QuestId}: 발생/해결 행동 쌍이 올바르지 않습니다.");
            if (!body.Supports(action.Name, mission.Movement, out string error)) throw new InvalidOperationException($"TargetID={mission.TargetId}: {error}");
        }
        public void Capture()
        {
            foreach (var body in bodies.Values) { body.CaptureBaseline(); body.SetActionable(false); }
        }
        public void Activate(MissionRuntime mission)
        {
            if (mission.Status != MissionStatus.Locked) return;
            Validate(mission);
            active.Add(mission);
            try { Rebuild(mission.TargetId); mission.Status = MissionStatus.Active; }
            catch { active.Remove(mission); Rebuild(mission.TargetId); throw; }
        }
        public void Resolve(MissionRuntime mission)
        {
            if (mission.Status != MissionStatus.Active) return;
            int index = active.IndexOf(mission);
            if (index < 0) throw new InvalidOperationException("활성 사건 상태가 일치하지 않습니다.");
            active.RemoveAt(index);
            try { Rebuild(mission.TargetId); mission.Status = MissionStatus.Resolved; }
            catch { active.Insert(index, mission); Rebuild(mission.TargetId); throw; }
        }
        private void Rebuild(int targetId)
        {
            IAnomalyBody body = bodies[targetId]; body.RestoreBaseline();
            bool actionable = false;
            foreach (var mission in active)
                if (mission.TargetId == targetId) { catalog.Get(mission.BeginAction).Apply(body, mission.Movement); actionable = true; }
            body.SetActionable(actionable);
        }
        public void Freeze() { foreach (var body in bodies.Values) body.SetActionable(false); }
        public void Reset()
        {
            active.Clear();
            foreach (var body in bodies.Values) { body.RestoreBaseline(); body.SetActionable(false); }
        }
    }
}
