using UnityEngine;

public class AshSpawner : MonoBehaviour
{
    [Header("Settings")]
    public GameObject prefab;
    public Transform spawnPoint;
    public KeyCode spawnKey = KeyCode.Space;

    public void Spawn()
    {
        if (prefab == null)
        {
            Debug.LogWarning("PrefabSpawner: prefab не назначен!");
            return;
        }

        var pos = spawnPoint != null ? spawnPoint.position : transform.position;
        var rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        Instantiate(prefab, pos, rot);
    }
}
