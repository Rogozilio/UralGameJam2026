using Unity.Entities;

namespace UralGameJam.Ecs.Game
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public sealed partial class InputSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var sourceEntity = SystemAPI.GetSingletonEntity<InputComponent>();
            var source = EntityManager.GetComponentObject<InputSource>(sourceEntity).View;

            if (source == null)
                return;

            var input = EntityManager.GetComponentData<InputComponent>(sourceEntity);
            input.Move = source.playerMove;
            input.Look = source.playerLook;
            input.JumpPressed = source.isJump;
            input.JumpHeld = source.isJumpHeld;
            input.IsGamepad = source.isGamepad;
            input.EscapePressed = source.isEscape;
            EntityManager.SetComponentData(sourceEntity, input);
        }
    }
}
