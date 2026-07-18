using System.Linq;
using UnityEngine;

namespace UralGameJam.Ecs.Restart
{
    public interface IRestart
    {
        void Restart();
    }
    
    public class RestartMono : MonoBehaviour
    {
        public static void Restart()
        {
            var restartObjects = FindObjectsOfType<MonoBehaviour>(false)
                .OfType<IRestart>().ToArray();
            
            foreach (IRestart obj in restartObjects)
            {
                if (((MonoBehaviour)obj).enabled) // Дополнительная проверка
                {
                    //Debug.Log("Restart " + ((MonoBehaviour)obj).name);
                    obj.Restart();
                }
            }
        }
    }
}
