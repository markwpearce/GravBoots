using System;
using UnityEngine;

namespace GravBoots
{
    public class FirstAid : MonoBehaviour
    {

        [SerializeField] float healAmount = 25;
       
        void OnTriggerEnter(Collider other) {
            Debug.Log ("First Aid collision!");
            GravBootsFirstPersonController player = other.gameObject.GetComponent<GravBootsFirstPersonController> ();
            if(player != null){
                CharacterHealth health = other.gameObject.GetComponent<CharacterHealth> ();
                if (health != null && !health.fullHealth()) {
                    Debug.Log("first aid healing!");
                    health.Heal (healAmount);
                }
                else {
                    Debug.Log("first aid full health");

                }
                Destroy (gameObject);
            }
            else {
                Debug.Log ("first aid not a player");
            }
        }

    }
}

