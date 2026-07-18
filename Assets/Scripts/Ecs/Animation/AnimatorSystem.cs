using Unity.Entities;

namespace UralGameJam.Ecs.Animation
{
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    public sealed partial class AnimatorSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            new ApplyAnimatorCommandsJob().Run();
        }

        public partial struct ApplyAnimatorCommandsJob : IJobEntity
        {
            public void Execute(AnimatorViewComponent view, DynamicBuffer<AnimatorCommand> commands)
            {
                var animator = view.animator;

                foreach (var command in commands)
                {
                    switch (command.type)
                    {
                        case AnimatorCommandType.SetBool:
                            animator.SetBool(command.nameHash, command.integerValue != 0);
                            break;
                        case AnimatorCommandType.SetInteger:
                            animator.SetInteger(command.nameHash, command.integerValue);
                            break;
                        case AnimatorCommandType.SetFloat:
                            animator.SetFloat(command.nameHash, command.floatValue);
                            break;
                        case AnimatorCommandType.SetTrigger:
                            animator.SetTrigger(command.nameHash);
                            break;
                        case AnimatorCommandType.CrossFade:
                            animator.CrossFade(command.nameHash, command.transitionDuration, command.layer);
                            break;
                    }
                }

                commands.Clear();
            }
        }
    }
}
