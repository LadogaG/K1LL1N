using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Finish : MonoBehaviour
{
    [SerializeField] float triggerDistance = 5;
    [SerializeField] bool instant = false;
    GameObject player;
    bool panel = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        if (Manager.Instance.win) return;
        float distance = Vector3.Distance(transform.position, player.transform.position);
        if (distance <= triggerDistance)
        {
            if (instant) Manager.Instance.Win();
            else if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F))
            {
                Manager.Instance.Panel(Manager.Instance.toWinPanel, 0.1f);
                Manager.Instance.Win();
            }

            if (!panel && !instant)
            {
                panel = true;
                Manager.Instance.Panel(Manager.Instance.toWinPanel, 0.1f);
            }
        }
        else if (panel)
        {
            panel = false;
            Manager.Instance.Panel(Manager.Instance.toWinPanel, 0.1f);
        }
    }
}
