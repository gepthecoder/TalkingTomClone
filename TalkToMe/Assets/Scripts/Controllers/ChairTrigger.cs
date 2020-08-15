using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChairTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.transform.name);
        if(other.gameObject.tag == "Player") 
        {
            Debug.Log("TRIGGER CHAIR");
            GameController.Instance.TRIGGER_STATE((int)PlayerState.Chilling);
        }
    }
}
