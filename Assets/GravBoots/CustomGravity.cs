using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace GravBoots
{

    [RequireComponent(typeof(CapsuleCollider))]
    public class CustomGravity : MonoBehaviour 
    {

        [SerializeField] private float m_gravityThreshold;
        [SerializeField] private float m_groundedThreshold;
        [SerializeField] private Color m_gravityLineColour = Color.red;
        [SerializeField] private Color m_downLineColour = Color.blue;
        [SerializeField] private Color m_feetSphereColour = Color.yellow;
        [SerializeField] private Color m_gravityRaysCast= Color.cyan;
        [SerializeField] private float maxAmount;
        private string[] m_ignoreTags = { "Entity", "SpaceStationSection", "RobotBody", "PlayerBody"};
       


        public Vector3 GravityDirection = Vector3.zero;
        public Vector3 LocalDown = Vector3.zero;
        public Vector3 FeetPosition = Vector3.zero;
        public bool isGrounded;
        public bool hasGravity;
        public Vector3 surfaceNormal = Vector3.zero;
        public float gravAmount;

        private Vector3 closestPoint;



        private Vector3 m_lastPosition = Vector3.zero;


        public GameObject objectCausingGravity;


        public GameObject lastObjectCausingGravity;

        private int m_gravRayCastMask;

        private CapsuleCollider m_collider;


        //private CharacterController m_CharacterController;
        //private GravBootsFirstPersonController m_gravBoots;


        // Use this for initialization
        private void Start()
        {
            m_collider = GetComponent<CapsuleCollider>();
            LocalDown = -transform.up;
            FeetPosition = transform.position+(-transform.up*(m_collider.height-0.05f) /2f );
        }


        private Color MakeDebugColour (Color c) {
            c.a = 0.5f*c.a;
            return c;
        }

        public static string GetGameObjectPath(GameObject com)
        {
            GameObject obj = com.gameObject;
            string path = "/" + obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path;
        }

        public static string GetGameObjectPath(Component com)
        {
            return GetGameObjectPath(com.gameObject);
        }

        bool shouldIgnoreGravitySource(Collider gravSource) {
            bool ignore = false;
            for (int i = 0; i < m_ignoreTags.Length; i++) {
                ignore = ignore || gravSource.tag == m_ignoreTags [i];
            };
            return ignore;
        
        }


        bool lookForGravityObject (out RaycastHit closestHitInfo) {

            float[] coords = { -1f, 0f, 1f};

            float ourClosestDistance = m_gravityThreshold;

            bool found = false;
            closestHitInfo = new RaycastHit();

            List<RaycastHit> surfaceHits = new List<RaycastHit> ();


            for(int i = 0; i< coords.Length; i++) {

                for(int j = 0;j< coords.Length; j++) {
                    for(int k = 0; k< coords.Length; k++) {
                        float rx = coords[i];
                        float ry = coords[j];
                        float rz = coords[k];

                        Vector3 rayDirection = new Vector3 (rx, ry, rz);
                        rayDirection = transform.TransformDirection (rayDirection);

                        Debug.DrawRay( FeetPosition, m_gravityThreshold*rayDirection, MakeDebugColour(m_gravityRaysCast));

                        RaycastHit hitInfo;

                        if (Physics.Raycast(FeetPosition, rayDirection, out hitInfo, m_gravityThreshold)) {
                            if (!shouldIgnoreGravitySource(hitInfo.collider)
                                && hitInfo.collider.gameObject != transform.gameObject) {

                                surfaceHits.Add (hitInfo);

                                if(hitInfo.distance < ourClosestDistance ) {
                                    closestHitInfo = hitInfo;
                                    ourClosestDistance = hitInfo.distance;
                                    found = true;
                                }
                            }
                        }

                    }
                }
            }

            if (found) {
                //find weighted average of surface normals
                //weight of each normal = 
                // (distance of closest /this distance)^2

                Vector3 normalsWeightedTotal = Vector3.zero;
                int surfaces = surfaceHits.Count;
               /* if (surfaceNormal != Vector3.zero) {
                    //use current to weight gravity direction.
                    normalsWeightedTotal += surfaceNormal;
                    surfaces+=1;
                }

                foreach (RaycastHit surfaceHit in surfaceHits) {
                    float weight = Mathf.Pow (ourClosestDistance / surfaceHit.distance, 2);
                    normalsWeightedTotal += weight * surfaceHit.normal;
                }
                */

                surfaceNormal = closestHitInfo.normal;//normalsWeightedTotal / surfaces;
            }


            return found;

        }





        void Update() {
            m_lastPosition = transform.position;
            LocalDown = -transform.up;

            //Debug.DrawLine(transform.position, transform.position + LocalDown*3, MakeDebugColour( m_downLineColour));

            FeetPosition = transform.position+(LocalDown*(m_collider.height-0.05f) /2f );

            RaycastHit hit;
            bool found = lookForGravityObject (out hit);
          
            //Vector3 closestPoint = Vector3.zero;

            if (found) {
                GravityDirection = -surfaceNormal;
                closestPoint = hit.point;
                Debug.DrawRay (transform.position, 10*GravityDirection, MakeDebugColour (m_gravityLineColour));
                lastObjectCausingGravity = objectCausingGravity;
                objectCausingGravity = hit.collider.gameObject;

                hasGravity = true;
                bool willBeGrounded = hit.distance < m_groundedThreshold;
                if (willBeGrounded && !isGrounded) {
                   // Debug.Log ("Grounded!");
                } else if (isGrounded && !willBeGrounded) {
                   // Debug.Log ("Lost grounded!");
                }
                isGrounded = willBeGrounded;
                gravAmount = maxAmount * (1f / (1f + hit.distance));
                if (objectCausingGravity != lastObjectCausingGravity) {
                   // Debug.Log ("Gravity Object: " + GetGameObjectPath(objectCausingGravity));
                   // Debug.Log ("Distance: " + hit.distance + ", isGrounded: "+isGrounded);
                   
                }
            } else {
                GravityDirection = Vector3.zero;
                surfaceNormal = Vector3.zero;
                hasGravity = false;
                isGrounded = false;
            }
            if(!hasGravity)
            {
                if (objectCausingGravity != null ) {
                    //Debug.Log ("Lost gravity!");
                }
                objectCausingGravity = null;
            }

        }

        void OnDrawGizmos() {
            Gizmos.color = MakeDebugColour(m_feetSphereColour);
            Gizmos.DrawSphere (FeetPosition, 0.1f);
            if (objectCausingGravity) {
                Gizmos.DrawCube (objectCausingGravity.transform.position, new Vector3(1, 1, 1));
            }
            if (objectCausingGravity) {
                Gizmos.DrawCube (objectCausingGravity.transform.position, new Vector3(1, 1, 1));
                Gizmos.DrawSphere (closestPoint, 0.1f);

            }

        }



    }
}

