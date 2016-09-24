using System;
using UnityStandardAssets;
using UnityEngine;

namespace GravBoots
{
    public class GravGun : MonoBehaviour
    {

        [SerializeField] GravProjectile m_projectile;
        [SerializeField] Rigidbody m_rigidBody;
        [SerializeField] float m_angleRandomness;
        [SerializeField] float m_kick;
        [SerializeField] Vector3 m_offset;
        [SerializeField] AudioSource m_gunSoundPlayer;

        [SerializeField] Vector3 m_initiaRotation;

        public GravGun ()
        {
        }


        private float getShootAngle() {
            float value = UnityEngine.Random.value * UnityEngine.Random.value;

            return value * 2f * m_angleRandomness - m_angleRandomness;
        }

        public void fire() {
            fire (transform.forward);
        }

        public void fire(Vector3 fireTarget) {
            //Debug.Log ("Fire!");
            Vector3 projectilePosition = transform.position
                +(m_offset.x*transform.right)
                +(m_offset.y*transform.up)
                +(m_offset.z*transform.forward);
            Quaternion projRot = Quaternion.LookRotation (fireTarget);
            GravProjectile projectile = Instantiate (m_projectile, projectilePosition, projRot) as GravProjectile;
            projectile.transform.Rotate (m_initiaRotation);
            projectile.gameObject.SetActive (true);
            projectile.enabled = true;
            float xdeg = getShootAngle (), ydeg = getShootAngle ();
            projectile.transform.Rotate (xdeg, 0, ydeg);

            projectile.firingEntity = gameObject.transform.root.gameObject;

            if(m_gunSoundPlayer != null && m_projectile.fireSound) {
                m_gunSoundPlayer.PlayOneShot( m_projectile.fireSound);
            }
            m_rigidBody.AddForce (m_kick * -transform.forward);
        }


    }
}

