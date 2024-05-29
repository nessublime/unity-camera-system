using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

//SUMMARY//
//Este MonoBehaviour esta en todas las camaras y se encarga de setear al player como target en el Start
//MUY PROVISIONAL cambiara...
public class DollyCam : MonoBehaviour
{
    void Start()
    {
        GetComponent<CinemachineVirtualCamera>().Follow = GameObject.Find("Player").transform;
        GetComponent<CinemachineVirtualCamera>().LookAt = GameObject.Find("Player").transform.GetChild(1);
    }
}
