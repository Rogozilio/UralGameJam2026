using Zenject;
using Input = Scripts.Input;

namespace Installers
{
    public class InputInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<Input>().ToSelf().FromComponentsInHierarchy().AsSingle().NonLazy();
        }
    }
}