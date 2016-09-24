using System;
using UnityEngine;
using UnityStandardAssets.Characters;

namespace GravBoots
{
    
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(GravityCharacter))]
    [RequireComponent(typeof(CustomGravity))]
    [RequireComponent(typeof (GravGun))]
    [RequireComponent(typeof (AudioSource))]
    public class GravityThirdPersonCharacter : MonoBehaviour
    {
        [SerializeField] float m_MovingTurnSpeed = 360;
        [SerializeField] float m_StationaryTurnSpeed = 180;
        [SerializeField] float m_JumpPower = 12f;
        [Range(1f, 4f)][SerializeField] float m_GravityMultiplier = 2f;
        [SerializeField] float m_RunCycleLegOffset = 0.2f; //specific to the character in sample assets, will need to be modified to work with others
        [SerializeField] float m_MoveSpeedMultiplier = 1f;
        [SerializeField] float m_AnimSpeedMultiplier = 1f;
        [SerializeField] float m_GroundCheckDistance = 1f;
        [SerializeField] AudioClipCycler m_footStepSounds;
        [SerializeField] AudioClip m_jumpSound;
        [SerializeField] AudioClip m_landSound;

        [SerializeField] ParticleSystem m_sparks;

        public GameObject lastHitPosition;


        Rigidbody m_Rigidbody;
        Animator m_Animator;
        bool m_IsGrounded;
        float m_OrigGroundCheckDistance;
        const float k_Half = 0.5f;
        float m_TurnAmount;
        float m_ForwardAmount;
        Vector3 m_GroundNormal;
        float m_CapsuleHeight;
        Vector3 m_CapsuleCenter;
        Vector3 m_worldMoveDir = Vector3.zero;
        CapsuleCollider m_Capsule;
        bool m_Crouching;

        bool m_previouslyGrounded = false;

        bool m_hasEverBeenGrounded = false;

        private float lerpSpeed = 5;
        Quaternion gravityRotation;
        Vector3 myNormal;

        public Vector3 m_currentMotion;
        public Vector3 m_currentJumpWorldMotion;

        private GravGun m_gun;

        AudioSource m_audioSource;

        GravityCharacter m_character;
        CustomGravity m_grav;
        void Start()
        {
            m_Animator = GetComponent<Animator>();
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Capsule = GetComponent<CapsuleCollider>();
            m_CapsuleHeight = m_Capsule.height;
            m_CapsuleCenter = m_Capsule.center;
            m_audioSource = GetComponent<AudioSource> ();

            m_grav = GetComponent<CustomGravity>();
            m_character = GetComponent<GravityCharacter> ();

            m_Rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

            myNormal = transform.up;

            m_gun = GetComponent<GravGun> ();

            m_currentJumpWorldMotion = -transform.up;

        }


        public void Move(Vector3 move, bool crouch, bool jump, bool hop, bool fall)
        {

            // convert the world relative moveInput vector into a local-relative
            // turn amount and forward amount required to head in the desired
            // direction.
            //Debug.Log ("Robot move 1: " + move);



            if (move.magnitude > 1f) move.Normalize();

            Vector3 localMove = transform.InverseTransformDirection(move);

            //Vector3 desiredMove = Vector3.forward * localMove.z + Vector3.right * localMove.x;;

            CheckGroundStatus();
           
            m_TurnAmount = Mathf.Atan2 (localMove.x, localMove.z);//desiredMove.x, desiredMove.z);
            m_ForwardAmount = localMove.z;

            ApplyExtraTurnRotation();

            //m_currentMotion = Vector3.zero;

            // control and velocity handling is different when grounded and airborne:
            if (m_IsGrounded || fall)
            {
                HandleGroundedMovement(crouch, jump, hop, fall);
            }
            else
            {
                m_currentMotion = transform.InverseTransformDirection(m_currentJumpWorldMotion);
                localMove = m_currentMotion;
                HandleAirborneMovement();
            }

            //ScaleCapsuleForCrouching(crouch);
            //PreventStandingInLowHeadroom();

            // send input and other state parameters to the animator
            //Debug.Log ("Robot move 2: " + move);

            if (m_IsGrounded) {
                m_currentMotion.x = localMove.x;//desiredMove.x;
                m_currentMotion.z = localMove.z;//desiredMove.z;
                //m_currentMotion.y = localMove.y;
            }
            m_worldMoveDir = transform.TransformDirection(m_currentMotion);

            m_currentJumpWorldMotion = m_worldMoveDir;// += m_currentJumpWorldMotion;

            if (m_IsGrounded && !jump && !hop) {
                m_currentJumpWorldMotion = Vector3.zero;
            }

            UpdateAnimator(localMove);
            m_character.MoveFixedUpdate (m_worldMoveDir* Time.fixedDeltaTime);


        }

        public void Turn(Vector3 move)
        {
            Vector3 localMove = transform.InverseTransformDirection(move);
            m_TurnAmount = Mathf.Atan2 (localMove.x, localMove.z);//desiredMove.x, desiredMove.z);
            m_ForwardAmount = 0f;
            ApplyExtraTurnRotation();
            UpdateAnimator(localMove);

        }
            


        void ScaleCapsuleForCrouching(bool crouch)
        {
            if (m_IsGrounded && crouch)
            {
                if (m_Crouching) return;
                m_Capsule.height = m_Capsule.height / 2f;
                m_Capsule.center = m_Capsule.center / 2f;
                m_Crouching = true;
            }
            else
            {
                Ray crouchRay = new Ray(m_Rigidbody.position + Vector3.up * m_Capsule.radius * k_Half, Vector3.up);
                float crouchRayLength = m_CapsuleHeight - m_Capsule.radius * k_Half;
                if (Physics.SphereCast(crouchRay, m_Capsule.radius * k_Half, crouchRayLength, ~0, QueryTriggerInteraction.Ignore))
                {
                    m_Crouching = true;
                    return;
                }
                m_Capsule.height = m_CapsuleHeight;
                m_Capsule.center = m_CapsuleCenter;
                m_Crouching = false;
            }
        }

        void PreventStandingInLowHeadroom()
        {
            // prevent standing up in crouch-only zones
            if (!m_Crouching)
            {
                Ray crouchRay = new Ray(m_Rigidbody.position + Vector3.up * m_Capsule.radius * k_Half, Vector3.up);
                float crouchRayLength = m_CapsuleHeight - m_Capsule.radius * k_Half;
                if (Physics.SphereCast(crouchRay, m_Capsule.radius * k_Half, crouchRayLength, ~0, QueryTriggerInteraction.Ignore))
                {
                    m_Crouching = true;
                }
            }
        }


        void UpdateAnimator(Vector3 move)
        {
            // update the animator parameters
            m_Animator.SetFloat("Forward", m_ForwardAmount, 0.1f, Time.deltaTime);
            m_Animator.SetFloat("Turn", m_TurnAmount, 0.1f, Time.deltaTime);
            m_Animator.SetBool("Crouch", m_Crouching);
            m_Animator.SetBool("OnGround", m_IsGrounded);
            if (!m_IsGrounded)
            {
                m_Animator.SetFloat("Jump", m_Rigidbody.velocity.y);
            }

            // calculate which leg is behind, so as to leave that leg trailing in the jump animation
            // (This code is reliant on the specific run cycle offset in our animations,
            // and assumes one leg passes the other at the normalized clip times of 0.0 and 0.5)
            float runCycle =
                Mathf.Repeat(
                    m_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime + m_RunCycleLegOffset, 1);
            float jumpLeg = (runCycle < k_Half ? 1 : -1) * m_ForwardAmount;
            if (m_IsGrounded)
            {
                m_Animator.SetFloat("JumpLeg", jumpLeg);
            }

            // the anim speed multiplier allows the overall speed of walking/running to be tweaked in the inspector,
            // which affects the movement speed because of the root motion.
            if (m_IsGrounded && move.magnitude > 0)
            {
                m_Animator.speed = m_AnimSpeedMultiplier;
            }
            else
            {
                // don't use that while airborne
                m_Animator.speed = 1;
            }
        }


        void HandleAirborneMovement()
        {
            // apply extra gravity from multiplier:

           // Vector3 extraGravityForce = (Physics.gravity * m_GravityMultiplier) - Physics.gravity;
            //m_Rigidbody.AddForce(extraGravityForce);

            m_GroundCheckDistance = m_Rigidbody.velocity.y < 0 ? m_OrigGroundCheckDistance : 0.01f;

            //if (m_IsGrounded) {
             //   m_currentJumpWorldMotion = Vector3.zero;
            //}

        }


        void HandleGroundedMovement(bool crouch, bool jump, bool hop, bool fall)
        {

            //m_Animator.applyRootMotion = false;

            // check whether conditions are right to allow a jump:

            float jumpPower = m_JumpPower;
            if (!jump && hop) {

                //Debug.Log ("Hopping!");

                jumpPower = m_JumpPower /2;
                jump = true;
            }
            if (fall) {
                jumpPower = -m_JumpPower /2;
                Debug.Log ("Falling!");
            }

           
            if ((jump && !crouch && m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Grounded")) || fall)
            {
                // jump!
                //m_Rigidbody.velocity = m_Rigidbody.velocity+m_JumpPower*m_grav.surfaceNormal;//new Vector3(m_Rigidbody.velocity.x, m_JumpPower, m_Rigidbody.velocity.z);
                m_IsGrounded = false;
                m_Animator.applyRootMotion = false;
                m_GroundCheckDistance = 0.1f;
                m_currentMotion.y = jumpPower;
                m_character.JumpFixedUpdate (jumpPower);
                RobotJumpSound (Mathf.Pow(jumpPower/m_JumpPower, 4));
            }

        }

        void ApplyExtraTurnRotation()
        {
            // help the character turn faster (this is in addition to root rotation in the animation)
            float turnSpeed = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, m_ForwardAmount);
            transform.Rotate(0, m_TurnAmount * turnSpeed * Time.deltaTime, 0);




        }


        public void OnAnimatorMove()
        {
            // we implement this function to override the default root motion.
            // this allows us to modify the positional speed before it's applied.
            if (m_IsGrounded && Time.deltaTime > 0)
            {
                Vector3 v = (m_Animator.deltaPosition * m_MoveSpeedMultiplier) / Time.deltaTime;

                // we preserve the existing y part of the current velocity.
               // v.y = m_Rigidbody.velocity.y;

                m_Rigidbody.velocity = v;

                //m_character.MoveFixedUpdate (transform.TransformDirection(v));
               //m_currentJumpWorldMotion = Vector3.zero;
            }
        }


        void CheckGroundStatus()
        {
            m_IsGrounded = m_grav.isGrounded;
            m_GroundNormal = m_grav.surfaceNormal;

            m_hasEverBeenGrounded = m_hasEverBeenGrounded || m_IsGrounded;

            m_Animator.applyRootMotion = false;//m_IsGrounded;


            if (m_IsGrounded && !m_previouslyGrounded) {
                m_currentJumpWorldMotion = Vector3.zero;
                RobotLandSound (1);
            }

            m_previouslyGrounded = m_IsGrounded;

            if(m_grav.hasGravity)
            {
                 myNormal = Vector3.Slerp(myNormal, -m_grav.GravityDirection, m_grav.gravAmount*lerpSpeed*Time.deltaTime);
                // find forward direction with new myNormal:
                Vector3 myForward = Vector3.Cross(transform.right, myNormal);

                Debug.DrawRay (transform.position, 2f*myForward, Color.red);

                // align character to the new myNormal while keeping the forward direction:
                //if (m_gravCharacter.velocity.sqrMagnitude > 0f) {

                if (myForward != Vector3.zero) {
                    gravityRotation = Quaternion.LookRotation (myForward, myNormal);
                } else {
                    gravityRotation = transform.rotation;
                }

                //} else {
                //    gravityRotation = transform.rotation;
                // }
            }

            Quaternion gravRot = Quaternion.Slerp (transform.rotation, gravityRotation, lerpSpeed * Time.deltaTime);
            transform.rotation = gravRot;


            /* RaycastHit hitInfo;
            #if UNITY_EDITOR
            // helper to visualise the ground check ray in the scene view
            Debug.DrawLine(transform.position + (Vector3.up * 0.1f), transform.position + (Vector3.up * 0.1f) + (Vector3.down * m_GroundCheckDistance));
            #endif
            // 0.1f is a small offset to start the ray from inside the character
            // it is also good to note that the transform position in the sample assets is at the base of the character
            if (Physics.Raycast(transform.position + (Vector3.up * 0.1f), Vector3.down, out hitInfo, m_GroundCheckDistance))
            {
                m_GroundNormal = hitInfo.normal;
                m_IsGrounded = true;
                m_Animator.applyRootMotion = true;
            }
            else
            {
                m_IsGrounded = false;
                m_GroundNormal = Vector3.up;
                m_Animator.applyRootMotion = false;
            }
            */
        }
    
        public void doShoot(Vector3 pos) {

            if(m_gun) {
                m_Animator.SetTrigger("Shooting");
                m_gun.fire(pos);
                m_Animator.SetTrigger("StopShooting");

            }
        }


        public void RobotStep() {
            if(m_audioSource) {
                m_footStepSounds.PlayNext (m_audioSource, Mathf.Clamp(m_Rigidbody.velocity.magnitude, 0.1f, 1));
            }
        }

        public void RobotJumpSound(float volume) {
            if(m_audioSource) {

                m_audioSource.PlayOneShot (m_jumpSound, volume);
            }
        }

        public void RobotLandSound(float volume) {
            if(m_audioSource) {

                m_audioSource.PlayOneShot (m_landSound, volume);
            }
        }

        public void DoHit() {
            
            m_Animator.ResetTrigger ("Hit");
            if (m_sparks) {
                if (lastHitPosition) {
                    m_sparks.transform.position = lastHitPosition.transform.position;
                } else {
                    m_sparks.transform.position = transform.position;
                }
                m_sparks.Play ();
            }
        }

        public void DoRagdoll() {
            //DIE!
           
            foreach(Rigidbody rb in gameObject.GetComponentsInChildren<Rigidbody>()) {
                rb.isKinematic = true;
                rb.drag = 0.1f;
            }
            foreach(HingeJoint hj in gameObject.GetComponentsInChildren<HingeJoint>()) {
                hj.useSpring = true;
            }

            CharacterHeadLook headLook = GetComponent<CharacterHeadLook> ();
            if (headLook != null)
                headLook.enabled = false;
        }
    
    }
}

