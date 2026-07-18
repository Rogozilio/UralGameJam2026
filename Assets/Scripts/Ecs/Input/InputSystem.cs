using Unity.Entities;

namespace UralGameJam.Ecs.Game
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public sealed partial class InputSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var sourceEntity = SystemAPI.GetSingletonEntity<InputComponent>();
            var source = EntityManager.GetComponentObject<InputSource>(sourceEntity).view;

            var input = EntityManager.GetComponentData<InputComponent>(sourceEntity);
            input.move = source.playerMove;
            input.look = source.playerLook;
            input.jumpPressed = source.isJump;
            input.jumpHeld = source.isJumpHeld;
            input.isGamepad = source.isGamepad;
            input.escapePressed = source.isEscape;
            EntityManager.SetComponentData(sourceEntity, input);
        }
    }
}
