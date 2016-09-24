using System;
using UnityStandardAssets;
using UnityEngine;
using System.Collections.Generic;

namespace GravBoots
{
    public class GravProjectile : MonoBehaviour
    {

        //keep track of decals...
        static Queue<GameObject> decals = new Queue<GameObject>();
        const int MAX_DECALS = 256;

        [SerializeField] float m_speed;
        [SerializeField] float m_lifeTime;
        [SerializeField] float m_maxDamage;
        public AudioClip fireSound;
        public GameObject decalHitWall;
        float floatInFrontOfWall = 0.001f;

        public GameObject firingEntity;


        private float m_bornTime;
       
        private void Start ()
        {
            m_bornTime = Time.time;
        }


        private float getDamageFactor(string colliderName) {
            if(colliderName == "Head") return 1f;
            if(colliderName == "Spine" || colliderName == "Torso") return 0.7f;
            if (colliderName.IndexOf ("Shoulder") >= 0)
                return 0.6f;
            if (colliderName.IndexOf ("Arm") >= 0)
                return 0.6f;
            if (colliderName.IndexOf ("Leg") >= 0)
                return 0.5f;
            return 0.4f;
        }


        private void Update()
        {
            bool doDestroy = (Time.time - m_bornTime) > m_lifeTime;

            Vector3 prevPos = transform.position;
            Vector3 projDirection = Vector3.up * m_speed * Time.deltaTime;
            Ray shotRay = new Ray (transform.position + transform.up, transform.up);
            bool hitHandled = false;
            bool wallHit = false;
            RaycastHit decalHit = new RaycastHit();
            RaycastHit[] hits = Physics.RaycastAll (transform.position + transform.up, transform.up, projDirection.magnitude * 2f);
            foreach (RaycastHit hit in hits)
            {
               
                if (decalHitWall &&
                    hit.collider.tag != "Entity" &&
                    hit.collider.tag != "Player" &&
                    hit.collider.tag != "RobotBody" &&
                    hit.collider.tag != "PlayerBody" && !hitHandled) {

                    decalHit = hit;
                    wallHit = true;
                }

                if ((hit.collider.tag == "RobotBody" || hit.collider.tag=="PlayerBody") && !hitHandled) {
                    hitHandled = true;
                    GameObject entity = hit.collider.transform.root.gameObject;
                    CharacterHealth health = entity.GetComponent<CharacterHealth>();
                    if (health != null) {
                        float damage = getDamageFactor (hit.collider.name) * m_maxDamage;
                        Debug.Log ("Hurting! " + damage);
                        GravityThirdPersonCharacter robotCharacter = entity.GetComponent<GravityThirdPersonCharacter> ();
                        if(robotCharacter != null) 
                            robotCharacter.lastHitPosition = hit.collider.gameObject;
                        health.Hurt(damage);
                        if (firingEntity!= null && health.isDead()) {
                            GravityAIController ai = firingEntity.GetComponent<GravityAIController> ();
                            if (ai != null) {
                                ai.PlayerKilled ();
                            }

                        }
                    }

                    doDestroy = true;
                }
            }

            if (!hitHandled && wallHit) {
                while (decals.Count > MAX_DECALS) {
                    GameObject decalToDestroy = decals.Dequeue ();
                    decalToDestroy.SetActive (false);
                    Destroy (decalToDestroy);
                }

                GameObject decal = Instantiate (decalHitWall, decalHit.point + (decalHit.normal * floatInFrontOfWall), Quaternion.LookRotation (decalHit.normal)) as GameObject;
                decal.SetActive (true);
                doDestroy = true;
                decals.Enqueue (decal);
            }



            transform.Translate(projDirection);


            if (doDestroy) {
                gameObject.SetActive (false);
                Destroy (gameObject);
                return;
            }
        }
    }
}

