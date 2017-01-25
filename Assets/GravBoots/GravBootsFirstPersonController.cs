using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;
using UnityEngine.UI;

namespace GravBoots
{
    [RequireComponent(typeof (GravityCharacter))]
    [RequireComponent(typeof (AudioSource))]
    [RequireComponent(typeof (CustomGravity))]
    [RequireComponent(typeof (CharacterHealth))]
    [RequireComponent(typeof(Animator))]
    public class GravBootsFirstPersonController : MonoBehaviour
    {
        [SerializeField] private bool m_IsWalking;
        [SerializeField] private float m_WalkSpeed;
        [SerializeField] private float m_RunSpeed;
        [SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
        [SerializeField] private float m_JumpSpeed;
        [SerializeField] private float m_StickToGroundForce;
        [SerializeField] private float m_GravityMultiplier;
        [SerializeField] private GravityMouseLook m_MouseLook;
        [SerializeField] private bool m_UseFovKick;
        [SerializeField] private FOVKick m_FovKick = new FOVKick();
        [SerializeField] private bool m_UseHeadBob;
        [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
        [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
        [SerializeField] private float m_StepInterval;
        [SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
        [SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
        [SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.
        [SerializeField] private AudioClip m_DeadSound;           // the sound played when character dies
        [SerializeField] private AudioClip m_HealSound;           // the sound played when character dies
        [SerializeField] private float rotationLerpSpeed = 10; // smoothing speed of automatic rotation
        [SerializeField] private float angleRotationThreshold = 2; // smoothing speed
        [SerializeField] GravGun m_gun;
        [SerializeField] AudioClipCycler m_hurtSounds;


        [SerializeField] private Camera m_Camera;


        private bool m_Jump;
        private float m_YRotation;
        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;
        private Vector3 m_WorldMoveDir = new Vector3 (0, -10f);
        private CollisionFlags m_CollisionFlags;
        private bool m_PreviouslyGrounded;
        private Vector3 m_OriginalCameraPosition;
        private float m_StepCycle;
        private float m_NextStep;
        private bool m_Jumping;
        private AudioSource m_AudioSource;

        private CustomGravity m_grav;

        private Vector3 myNormal;
        private GravityCharacter m_gravCharacter;

        private Quaternion gravityRotation;

        private CharacterHealth m_health;

        public UnityEngine.UI.Text healthText;

        Animator m_Animator;

        bool neverGrounded = true;

        private float timeOfLastJump = 0;

        // Use this for initialization
        private void Start()
        {
            m_gravCharacter = GetComponent<GravityCharacter>();
            m_OriginalCameraPosition = m_Camera.transform.localPosition;
            m_FovKick.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_StepInterval);
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle/2f;
            m_Jumping = false;
            m_AudioSource = GetComponent<AudioSource>();
            m_grav = GetComponent<CustomGravity> ();
            m_MouseLook.Init(transform, m_Camera.transform, m_grav);
            myNormal = transform.up;
            m_health = GetComponent<CharacterHealth> ();
            m_Animator = GetComponent<Animator>();


            m_health.OnHurt += playerHurt;
            m_health.OnDie += playerDied;
            m_health.OnHeal += playerHeal;

        }

        public bool isDead {
            get {
                if (m_health == null)
                    return false;
                return  m_health.isDead ();
            }
        }


        // Update is called once per frame
        private void Update()
        {
            RotateView();
            // the jump state needs to read here to make sure it is not missed
            if (!m_Jump && m_grav.isGrounded)
            {
                m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
            }

            if (!m_PreviouslyGrounded && m_grav.isGrounded)//m_CharacterController.isGrounded)
            {
                StartCoroutine(m_JumpBob.DoBobCycle());
                PlayLandingSound();
                m_MoveDir.y = 0f;
                m_Jumping = false;
            }
            if (!m_grav.isGrounded && !m_Jumping && m_PreviouslyGrounded)
            {
               // m_MoveDir.y = 0f;
            }

            m_PreviouslyGrounded = m_grav.isGrounded;//m_CharacterController.isGrounded;


            bool fire = CrossPlatformInputManager.GetButtonDown("Fire1");

            if (!isDead && fire) {
                m_gun.fire ();
                m_Animator.SetTrigger ("Shoot");
            }

            UpdateHealthText ();

          
        }


        private void PlayLandingSound()
        {
            if (isDead)
                return;

            m_AudioSource.clip = m_LandSound;
            m_AudioSource.Play();
            m_NextStep = m_StepCycle + .5f;
        }


        private void FixedUpdate()
        {
            float speed;
            GetInput(out speed);

            //m_MoveDir = Vector3.zero;
            if (m_grav.isGrounded) {
                // always move along the camera forward as it is the direction that it being aimed at

                Vector3 desiredMove = Vector3.forward * m_Input.y + Vector3.right * m_Input.x;;

                m_MoveDir.x = desiredMove.x*speed;
                m_MoveDir.z = desiredMove.z*speed;


                neverGrounded = false;
                if (m_Jump && !isDead) {
                    m_MoveDir.y = m_JumpSpeed;
                    m_gravCharacter.JumpFixedUpdate(m_JumpSpeed);
                    PlayJumpSound ();
                    m_Jump = false;
                    m_Jumping = true;
                    timeOfLastJump = Time.time;
                    Debug.Log ("Jumping!");


                }
                m_WorldMoveDir = transform.TransformDirection (m_MoveDir);

            }
            else if (!m_Jumping && !isDead) {
                //TODO: WHen you loose groundedness, but it wasn't from a jump, slow down world momentum!
                m_WorldMoveDir *= 0.5f;
            }
            m_gravCharacter.MoveFixedUpdate (m_WorldMoveDir * Time.fixedDeltaTime);

            if(m_grav.hasGravity)
            {
                
                /*m_MoveDir += m_grav.GravityDirection*m_GravityMultiplier*Time.fixedDeltaTime;

                float angle = Vector3.Angle (transform.up, -m_grav.GravityDirection);

                Debug.Log ("Angle :" + angle);

                Quaternion targetRot = Quaternion.AngleAxis (angle, transform.forward);


                Debug.Log(String.Format("Lerping: {0}, {1}, {2} to {3}, {4}, {5}",
                    transform.rotation.x, transform.rotation.y, transform.rotation.z, 
                    targetRot.x, targetRot.y, targetRot.z));

                transform.rotation = Quaternion.Lerp (transform.rotation, targetRot, Time.fixedDeltaTime);*/
                float angleDifference = Vector3.Angle (myNormal, -m_grav.GravityDirection);
                myNormal = Vector3.Slerp(myNormal, -m_grav.GravityDirection, m_grav.gravAmount*rotationLerp(Time.deltaTime));
                
                    // find forward direction with new myNormal:
                Vector3 myForward = Vector3.Cross(transform.right, myNormal);

                Debug.DrawRay (transform.position, 2f*myForward, Color.red);
                Debug.DrawRay (transform.position, 2.5f*myNormal, Color.magenta);

                // align character to the new myNormal while keeping the forward direction:
                //if (m_gravCharacter.velocity.sqrMagnitude > 0f) {
                gravityRotation = Quaternion.LookRotation (myForward, myNormal);
                //} else {
                //    gravityRotation = transform.rotation;
               // }
            }
          
            ProgressStepCycle(speed);
            UpdateCameraPosition(speed);

            m_MouseLook.UpdateCursorLock();
        }


        private void PlayJumpSound()
        {
            m_AudioSource.clip = m_JumpSound;
            m_AudioSource.Play();
        }


        private void ProgressStepCycle(float speed)
        {
            if (isDead)
                return;

            if (m_gravCharacter.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
            {
                m_StepCycle += (m_gravCharacter.velocity.magnitude + (speed*(m_IsWalking ? 1f : m_RunstepLenghten)))*
                    Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep))
            {
                return;
            }

            m_NextStep = m_StepCycle + m_StepInterval;

            PlayFootStepAudio();
        }


        private void PlayFootStepAudio()
        {
            if (!m_grav.isGrounded || isDead)
            {
                return;
            }
            // pick & play a random footstep sound from the array,
            // excluding sound at index 0
            int n = Random.Range(1, m_FootstepSounds.Length);
            m_AudioSource.clip = m_FootstepSounds[n];
            m_AudioSource.PlayOneShot(m_AudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            m_FootstepSounds[n] = m_FootstepSounds[0];
            m_FootstepSounds[0] = m_AudioSource.clip;
        }


        private void UpdateCameraPosition(float speed)
        {
            Vector3 newCameraPosition;
            if (!m_UseHeadBob)
            {
                return;
            }
            if (m_gravCharacter.velocity.magnitude > 0 && m_grav.isGrounded)
            {
                m_Camera.transform.localPosition =
                    m_HeadBob.DoHeadBob(m_gravCharacter.velocity.magnitude +
                        (speed*(m_IsWalking ? 1f : m_RunstepLenghten)));
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
            }
            else
            {
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
            }
            m_Camera.transform.localPosition = newCameraPosition;
        }


        private void GetInput(out float speed)
        {
            // Read input
            float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
            float vertical = CrossPlatformInputManager.GetAxis("Vertical");

            bool waswalking = m_IsWalking;

            #if !MOBILE_INPUT
            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running
            m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
            #endif

            if (m_health.isDead ()) {
                horizontal = 0;
                vertical = 0;
            }

            // set the desired speed to be walking or running
            speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
            m_Input = new Vector2(horizontal, vertical);

            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }

            // handle speed change to give an fov kick
            // only if the player is going to a run, is running and the fovkick is to be used
            if (m_IsWalking != waswalking && m_UseFovKick && m_gravCharacter.velocity.sqrMagnitude > 0)
            {
                StopAllCoroutines();
                StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
            }





            //Check distance away from station
            if (transform.position.magnitude > 500) {
                
            }
        }


        private float rotationLerp(float time) {
            float lerpSpeed = rotationLerpSpeed * time ;
            return lerpSpeed;
        }



        private void RotateView()
        {

            float angleNeed = Quaternion.Angle (transform.rotation, gravityRotation);
            float speedFactor = 1;

            if (angleNeed < 15f) {
                speedFactor = m_gravCharacter.GetPlayerSpeed () *angleNeed/15f;
                speedFactor = Math.Max (speedFactor, 0.2f);
            }
            float maxAngleToRotate = angleNeed * m_grav.currentGravFraction()*speedFactor;
            maxAngleToRotate = Math.Max (4f, maxAngleToRotate);
            maxAngleToRotate = Math.Min (maxAngleToRotate, 15f);
         
            Debug.Log ("Speed: " + m_gravCharacter.GetPlayerSpeed () +" Max Rot Angle: "+maxAngleToRotate+" angle need: "+angleNeed);
           

            Quaternion gravRot = Quaternion.RotateTowards (transform.rotation, gravityRotation, maxAngleToRotate);
           
            Vector3 feet = transform.position- transform.up* ((m_gravCharacter.height - 0.05f) / 2f);;

            transform.rotation = gravRot;

            transform.position = feet + transform.up * ((m_gravCharacter.height - 0.05f) / 2f);
            m_MouseLook.LookRotation (transform, m_Camera.transform, m_health.isDead());
        }


        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            /*Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (m_CollisionFlags == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(m_gravCharacter.velocity*0.1f, hit.point, ForceMode.Impulse);*/
        }


        private void playerHeal() {

            Debug.Log ("Heal! " + m_health.Health);

            if(m_AudioSource) {
                m_AudioSource.PlayOneShot(m_HealSound);
            }

        }


        private void playerHurt() {

            Debug.Log ("Hurt! " + m_health.Health);

            if(m_AudioSource) {
                m_hurtSounds.PlayNext (m_AudioSource);
            }
        
        }

        private void playerDied() {
            Debug.Log ("Dead! " + m_health.Health);

            m_AudioSource.PlayOneShot(m_DeadSound);
        }


        private void UpdateHealthText() {

            if (healthText == null) {
                return;
            }

            if (m_health.isDead ()) {
                healthText.text = "Dead";
            } else {
                healthText.text = "" + Mathf.FloorToInt (m_health.Health);
            }
        }
    }
}

