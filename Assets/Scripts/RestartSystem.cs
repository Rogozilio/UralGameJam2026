using System.Linq;
using UnityEngine;

namespace Scripts
{
    public interface IRestart
    {
        public void Restart();
    }
    
    public class RestartSystem : MonoBehaviour
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