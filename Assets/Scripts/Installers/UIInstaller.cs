using Zenject;

namespace Installers
{
    public class UIInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<UIMenu>().ToSelf().FromComponentsInHierarchy().AsSingle().NonLazy();
        }
    }
}
