using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.Characters.FirstPerson;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

/// <summary>
/// C# translation from http://answers.unity3d.com/questions/155907/basic-movement-walking-on-walls.html
/// Author: UA @aldonaletto 
/// </summary>

// Prequisites: create an empty GameObject, attach to it a Rigidbody w/ UseGravity unchecked
// To empty GO also add BoxCollider and this script. Makes this the parent of the Player
// Size BoxCollider to fit around Player model.
namespace GravBoots {

    public class GravityCharacterController : MonoBehaviour {

        [SerializeField] private float moveSpeed = 6; // move speed
        [SerializeField] private float turnSpeed = 90; // turning speed (degrees/second)
        [SerializeField] private float lerpSpeed = 10; // smoothing speed
        [SerializeField] private float gravity = 10; // gravity acceleration
        [SerializeField] private float deltaGround = 0.2f; // character is grounded up to this distance
        [SerializeField] private float jumpSpeed = 10; // vertical jump initial speed
        [SerializeField] private float jumpRange = 1.0f; // range to detect target wall
        [SerializeField] private MouseLook m_MouseLook;
        [SerializeField] private bool m_UseFovKick;
        [SerializeField] private FOVKick m_FovKick = new FOVKick();

        //[SerializeField] private Vector3 surfaceNormal; // current surface normal

        private Vector3 myNormal; // character normal
        private float distGround; // distance from character position to ground
        private bool jumping = false; // flag &quot;I'm jumping to wall&quot;
        private float vertSpeed = 0; // vertical jump current speed

        public bool IsGrounded;

        private Camera m_Camera;


        private Transform myTransform;
        private Rigidbody m_rigidBody;
        private CustomGravity m_customGravity;

        private void Start(){
            myNormal = transform.up; // normal starts as character up direction
            myTransform = transform;
            m_rigidBody = GetComponent<Rigidbody> ();
            m_rigidBody.freezeRotation = true; // disable physics rotation
            m_customGravity = GetComponent<CustomGravity>();
            m_Camera = Camera.main;

        }

        private void FixedUpdate(){
            // apply constant weight force according to character normal:
            m_rigidBody.AddForce(-gravity*GetComponent<Rigidbody>().mass*myNormal);
        }

        private void Update(){
            // jump code - jump to wall or simple jump
            if (jumping) return; // abort Update while jumping to a wall

            Ray ray;
            RaycastHit hit;
            /*
            if (Input.GetButtonDown("Jump")){ // jump pressed:
                ray = new Ray(myTransform.position, myTransform.forward);
                if (Physics.Raycast(ray, out hit, jumpRange)){ // wall ahead?
                    JumpToWall(hit.point, hit.normal); // yes: jump to the wall
                }
                else if (isGrounded){ // no: if grounded, jump up
                    GetComponent<Rigidbody>().velocity += jumpSpeed * myNormal;
                }
            }
            */

            // movement code - turn left/right with Horizontal axis:
            myTransform.Rotate(0, Input.GetAxis("Horizontal")*turnSpeed*Time.deltaTime, 0);
            // update surface normal and isGrounded:
            /*ray = new Ray(myTransform.position, -myNormal); // cast ray downwards
            if (Physics.Raycast(ray, out hit)){ // use it to update myNormal and isGrounded
                IsGrounded = hit.distance <= distGround + deltaGround;
                surfaceNormal = hit.normal;
            }
            else {
                IsGrounded = false;
                // assume usual ground normal to avoid "falling forever"
                surfaceNormal = Vector3.up;
            }*/



            myNormal = Vector3.Lerp(myNormal, m_customGravity.surfaceNormal, lerpSpeed*Time.deltaTime);
            // find forward direction with new myNormal:
            Vector3 myForward = Vector3.Cross(myTransform.right, myNormal);
            // align character to the new myNormal while keeping the forward direction:
            Quaternion targetRot = Quaternion.LookRotation(myForward, myNormal);
            myTransform.rotation = Quaternion.Lerp(myTransform.rotation, targetRot, lerpSpeed*Time.deltaTime);
            // move the character forth/back with Vertical axis:
            myTransform.Translate(0, 0, Input.GetAxis("Vertical")*moveSpeed*Time.deltaTime);
        }

        private void JumpToWall(Vector3 point, Vector3 normal){
            // jump to wall
            jumping = true; // signal it's jumping to wall
            GetComponent<Rigidbody>().isKinematic = true; // disable physics while jumping
            Vector3 orgPos = myTransform.position;
            Quaternion orgRot = myTransform.rotation;
            Vector3 dstPos = point + normal * (distGround + 0.5f); // will jump to 0.5 above wall
            Vector3 myForward = Vector3.Cross(myTransform.right, normal);
            Quaternion dstRot = Quaternion.LookRotation(myForward, normal);

            StartCoroutine (jumpTime (orgPos, orgRot, dstPos, dstRot, normal));
            //jumptime
        }

        private IEnumerator jumpTime(Vector3 orgPos, Quaternion orgRot, Vector3 dstPos, Quaternion dstRot, Vector3 normal) {
            for (float t = 0.0f; t < 1.0f; ){
                t += Time.deltaTime;
                myTransform.position = Vector3.Lerp(orgPos, dstPos, t);
                myTransform.rotation = Quaternion.Slerp(orgRot, dstRot, t);
                yield return null; // return here next frame
            }
            myNormal = normal; // update myNormal
            GetComponent<Rigidbody>().isKinematic = false; // enable physics
            jumping = false; // jumping to wall finished

        }


    }
}