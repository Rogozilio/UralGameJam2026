using System;
using UnityEngine;

namespace Scripts
{
    public class KillBox : MonoBehaviour
    {
        private void OnTriggerStay(Collider other)
        {
            if(other.CompareTag("Player"))
            {
                other.GetComponent<Player>().RestartNow();
            }
        }
    }
}