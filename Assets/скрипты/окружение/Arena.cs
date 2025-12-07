using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Arena : MonoBehaviour
{
    [SerializeField] List<Door> doorsToLock;
    [SerializeField] List<GameObject> enemies;

    bool arenaActive = false;

    void Awake()
    {
        if (enemies.Count == 0)
        {
            enemies = transform.GetComponentsInChildren<Enemy>()
                .Where(t => t != transform)
                .Select(child => child.gameObject)
                .Where(active => active.activeSelf)
                .ToList();
        }

        foreach (var enemy in enemies)
        {
            var enemyComponent = enemy.GetComponent<Enemy>();
            if (enemyComponent != null)
            {
                enemyComponent.enabled = false;
            }
        }
    }

    void Start()
    {
        foreach (var enemy in enemies)
        {
            enemy.tag = "Untagged";
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !arenaActive)
        {
            arenaActive = true;
            foreach (var door in doorsToLock)
            {
                door.LockDoor();
            }
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {                    
                    var enemyComponent = enemy.GetComponent<Enemy>();
                    if (enemyComponent != null)
                    {
                        enemyComponent.enabled = true;
                        enemy.tag = "Enemy";
                    }
                }
            }
        }
    }

    void Update()
    {
        if (arenaActive)
        {
            bool allEnemiesDead = true;
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {                    
                    if (enemy.GetComponent<Enemy>() != null)
                    {
                        allEnemiesDead = false;
                        break;
                    }
                }
            }

            if (allEnemiesDead)
            {
                foreach (var door in doorsToLock)
                {
                    door.UnlockDoor();
                }
                arenaActive = false;
                Manager.Instance.Popup(Manager.Instance.killAim, 1);
                Manager.Instance.Flash();
                Manager.Instance.Pause(0.2f);

                Destroy(this);
            }
        }
    }
}