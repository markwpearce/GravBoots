using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Characters.FirstPerson;

namespace GravBoots
{

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Animator))]
    public class GravityCharacter: MonoBehaviour
    {
        private CapsuleCollider m_collider;
        private Rigidbody m_rigidbody;
        private CustomGravity m_customGrav;

        private float playerSpeed = 0;
        private float lastTimeSpeedUpdate = Time.time;


        void Start()
        {
            m_collider = GetComponent<CapsuleCollider>();
            m_rigidbody = GetComponent<Rigidbody>();
            m_customGrav = GetComponent<CustomGravity>();
        }


        public float height {
            get {
                return m_collider.height;
            }

        }

        public float radius {
            get {
                return m_collider.radius;
            }
          ///  set;
        }


        public Vector3 velocity {
            get {
                return m_rigidbody.velocity;
            }
          //  set;
        }


        public void MoveFixedUpdate(Vector3 desiredWorldDirection) {
            
            Debug.DrawRay (transform.position, desiredWorldDirection, Color.black);
             // get a normal for the surface that is being touched to move along it
            Vector3 actualDirection = transform.InverseTransformDirection(desiredWorldDirection);

           
            if (m_customGrav.isGrounded) {
                DrawPlane (m_customGrav.FeetPosition, -m_customGrav.GravityDirection);
                //actualDirection = Vector3.ProjectOnPlane (actualDirection, -m_customGrav.GravityDirection);
                //actualDirection.y -= 0.2f;
            }

            if (actualDirection.magnitude > 0.01f) {
                Debug.DrawLine (transform.position, transform.position + 20f*actualDirection, Color.green);
            }

            RaycastHit hitInfo;
            if (!m_customGrav.isGrounded && Physics.SphereCast(transform.position, m_collider.radius, actualDirection, out hitInfo, actualDirection.magnitude))
            {
                actualDirection = Vector3.ClampMagnitude (actualDirection, hitInfo.distance-m_collider.height);
            }

            Vector3 previousPosition = transform.position;


            if (actualDirection.magnitude > 0.01f) {
                    
                transform.Translate (actualDirection);
            }

            playerSpeed = (transform.position - previousPosition).sqrMagnitude / (Time.time - lastTimeSpeedUpdate);

            lastTimeSpeedUpdate = Time.time;
        }

        public void JumpFixedUpdate(float speed) {
            if (m_customGrav.isGrounded) {
                m_rigidbody.AddForce(speed*transform.up);
                //Debug.Log ("Jumping! " +speed);
            }
        }

        public float GetPlayerSpeed() {
            return playerSpeed;
        }

        private void FixedUpdate()
        {
            if (m_customGrav.hasGravity) {
               Vector3 gravityForce = m_customGrav.gravAmount * m_rigidbody.mass * m_customGrav.GravityDirection;
               m_rigidbody.AddForce(gravityForce);
            }
        }


        void DrawPlane(Vector3 position, Vector3 normal) {

             Vector3 v3;

            if (normal.normalized != Vector3.forward)
                v3 = Vector3.Cross(normal, Vector3.forward).normalized * normal.magnitude;
            else
                v3 = Vector3.Cross(normal, Vector3.up).normalized * normal.magnitude;;

            Vector3 corner0 = position + v3;
            Vector3 corner2 = position - v3;
            Quaternion q = Quaternion.AngleAxis(90.0f, normal);
            v3 = q * v3;
            Vector3 corner1 = position + v3;
            Vector3 corner3 = position - v3;

            Debug.DrawLine(corner0, corner2, Color.green);
            Debug.DrawLine(corner1, corner3, Color.green);
            Debug.DrawLine(corner0, corner1, Color.green);
            Debug.DrawLine(corner1, corner2, Color.green);
            Debug.DrawLine(corner2, corner3, Color.green);
            Debug.DrawLine(corner3, corner0, Color.green);
            Debug.DrawRay(position, normal, Color.red);
        }



    }
}

