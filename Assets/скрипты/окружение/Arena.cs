using System.Collections.Generic;
using UnityEngine;

public class Arena : MonoBehaviour
{
    [SerializeField] private List<Door> doorsToLock;
    [SerializeField] private List<GameObject> enemies;

    private bool arenaActive = false;

    void Awake()
    {
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
            var enemyComponent = enemy.GetComponent<Enemy>();
            if (enemyComponent != null)
            {
                enemy.tag = "Untagged";
            }
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