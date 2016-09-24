using System;
using UnityEngine;

namespace GravBoots
{
    [RequireComponent(typeof (GravityThirdPersonCharacter))]
    [RequireComponent(typeof (CustomGravity))]
    [RequireComponent(typeof (CharacterHealth))]
    [RequireComponent(typeof (CharacterHeadLook))]
    [RequireComponent(typeof (AudioSource))]
    [RequireComponent(typeof (Animator))]
    public class GravityAIController : MonoBehaviour
    {
        public GravityThirdPersonCharacter character { get; private set; } // the character we are controlling
        public Transform target;    // target to aim for
        [SerializeField] AudioClip m_hurtSound;
        [SerializeField] AudioClip m_diedSound;
        [SerializeField] AudioClip m_killedSound;
        [SerializeField] AudioClip m_targetAcquiredSound;
        [SerializeField] AudioClip m_targeLostSound;
        [SerializeField] float CloseDistanceRange = 8f;
        [SerializeField] float HowFarCanISee = 100f;
        [SerializeField] float FieldOfView = 120f;
        [SerializeField] float ampedUpSeconds = 5f;

        [SerializeField] GameObject m_firstAidKit;

        public Pause pause;


        public float stopDistance;


        private CustomGravity m_grav;
        private CharacterHealth m_health;
        private bool m_previouslyGrounded = false;

        private float m_lastJumpTime;
        private float m_lastLandTime;
        private float m_lastShootTime;

        private bool wasAware = false;

        private bool doHop;

        CharacterHeadLook m_headLook;

        private AudioSource m_AudioSource;
        private Animator m_Animator;

        float m_lastSawTargetTime = -100;
        Vector3 m_lastTargetPosition = new Vector3();


        CapsuleCollider m_mainCollider = null;

        GravBootsFirstPersonController m_playerController;

        bool justWokeUp = true;

        private void Start()
        {
            // get the components on the object we need ( should not be null due to require component so no need to check )
            //agent = GetComponentInChildren<NavMeshAgent>();
            character = GetComponent<GravityThirdPersonCharacter>();
            m_grav = GetComponent<CustomGravity> ();

            m_AudioSource = GetComponent<AudioSource> ();

            m_health = GetComponent<CharacterHealth> ();

            m_headLook = GetComponent<CharacterHeadLook> ();

            m_Animator = GetComponent<Animator> ();

            m_headLook.lookAtTarget = target;

            m_mainCollider = GetComponent<CapsuleCollider> ();
            m_health.OnHurt += OnHurt;
            m_health.OnDie += OnDead;

            m_lastTargetPosition = transform.position-2f*(transform.up+transform.forward);

            SetTarget (target);
        }

        public bool isDead {
            get {
                if(m_health)
                    return m_health.isDead ();
                return false;
            }
        }





        private bool isAwareOfTarget() {
            if (m_playerController != null && m_playerController.isDead) {
                return false;
            }


            return (Time.time - m_lastSawTargetTime < ampedUpSeconds);
        }

        //from [url]http://answers.unity3d.com/questions/8453/field-of-view-using-raycasting[/url]
        private bool CanSeeTarget() {

            if (m_playerController == null || m_playerController.isDead) {
                //Debug.Log ("AI - No Player!");
                return false;
            }
            bool seesTarget = false;
            RaycastHit hit;
            Vector3 rayDirection = target.transform.position - transform.position;

            Debug.DrawRay (transform.position, 5f*rayDirection, Color.blue);
            Debug.DrawRay (transform.position,  5f*m_headLook.headLookVector, Color.yellow);


            float angleToTarget = Vector3.Angle (rayDirection, m_headLook.headLookVector);
            float distanceToTarget = Vector3.Distance (transform.position, target.transform.position);


                // If the ObjectToSee is close to this object and is in front of it, then return true
            if( distanceToTarget<= (CloseDistanceRange/2f)) {
                //Debug.Log ("AI target is close " + distanceToTarget);
                seesTarget = true;
            }
            else if (angleToTarget < 150 && distanceToTarget <= CloseDistanceRange) {
                //Debug.Log ("AI hears target " + distanceToTarget);
                seesTarget = true;
            } else if (angleToTarget < (FieldOfView / 2)) { // Detect if player is within the field of view
                //Debug.Log("within field of view");

                if (Physics.Raycast (transform.position, rayDirection, out hit, HowFarCanISee)) {

                    if (hit.collider.gameObject == target) {
                        seesTarget = true;
                        //Debug.Log ("AI Sees target");
                    } else {
                        seesTarget = false;
                        //Debug.Log ("AI target is behind wall");
                    }
                } else {
                   // Debug.Log ("AI target is too far away: "+distanceToTarget );
                }
            } else {
                //Debug.Log("AI target is outside of FOV angle: " +angleToTarget +" distance: "+ distanceToTarget);

            }

            if (seesTarget) {
                m_lastSawTargetTime = Time.time;
                m_lastTargetPosition = target.transform.position;
            }
            return seesTarget;

        }


        private void OnHurt() {
            m_AudioSource.PlayOneShot (m_diedSound);
        }

        private void OnDead() {

            if (UnityEngine.Random.value > .5f) {
                //25% of the time, drop first aid kit
                Instantiate (m_firstAidKit, transform.position+ Vector3.right*.5f, transform.rotation);
            }

            m_AudioSource.PlayOneShot (m_diedSound);
            m_mainCollider.height = 1f;
            m_mainCollider.radius = 0.2f;


        }




        private void Update()
        {
            if (target == null) {
                return;
            }
           

            if (m_health.isDead()) {
                if(!m_grav.isGrounded) character.Move (Vector3.zero, false, false , false, false);
                return;
            }



            checkForGrounded ();

            

            bool canSeeTarget = CanSeeTarget ();
            bool isAware = isAwareOfTarget ();


            checkAwarenessChange (isAware);

           
            Vector3 moveDirection = m_lastTargetPosition - transform.position;
            float moveDistance = moveDirection.magnitude;


            bool shoot = canSeeTarget && (Time.time - m_lastShootTime > 3) && !m_playerController.isDead;

            if(shoot) {
                character.doShoot(target.transform.position - transform.position);
                m_lastShootTime = Time.time+UnityEngine.Random.value;
            }

            bool stillShooting = Time.time - m_lastShootTime < 0.5f;



            Vector3 localDirection = transform.InverseTransformDirection (moveDirection);
            Quaternion q = Quaternion.FromToRotation(Vector3.forward, localDirection);
            Vector3 v3Euler = q.eulerAngles;

            float upAngle = Mathf.Abs (360 - v3Euler.x);

            bool doJump = m_grav.isGrounded && upAngle > 60 && (Time.time - m_lastLandTime > 2);

            if (isAware) {
                m_headLook.lookAtPosition = m_lastTargetPosition;
            } else {
                m_headLook.lookAtPosition = transform.position + transform.forward;
            }

            float aiStopDistance = stopDistance;
            if (isAware && !canSeeTarget) {
                aiStopDistance = 1f;
            }

            if (justWokeUp) {
                moveDirection = transform.forward - transform.up;
            }
              
            if ((m_playerController != null && moveDistance > aiStopDistance && !stillShooting && !m_playerController.isDead) ||justWokeUp) {
                Vector3 beforePos = transform.position;
                character.Move (moveDirection, false, doJump, doHop, false);
                float distanceMoved = Vector3.Distance (transform.position, beforePos);
                doHop = (distanceMoved < 0.01) && (moveDistance - stopDistance > 1);
                if (justWokeUp) {
                    m_lastTargetPosition = transform.position + 1f * transform.forward;
                    justWokeUp = !m_grav.isGrounded;
                }

            } else if (!m_grav.isGrounded) {
                
                character.Move (moveDirection, false, false, false, justWokeUp);


            } else {
                //Debug.Log ("grounded");
                if (justWokeUp) {
                    m_lastTargetPosition = transform.position + 3f * transform.forward;
                    justWokeUp = false;
                }
                character.Turn (moveDirection);
                doHop = false;
                justWokeUp = false;

            }


        }


        public void SetTarget(Transform target)
        {
            this.target = target;

            if (target == null) {
                return;
            }

            GravBootsFirstPersonController fps = target.gameObject.GetComponent<GravBootsFirstPersonController> ();
            if (fps != null) {
                m_playerController = fps;
            }
        }


        private void checkAwarenessChange(bool isAware) {
            if (!wasAware && isAware) {
                PlayerSighted ();
            }
            else if(wasAware && !isAware) {
                PlayerLost();
            }

            wasAware = isAware;
            m_Animator.SetBool ("Agro", isAware);

        }

        private void PlayerSighted() {
            if (m_playerController == null || m_playerController.isDead)
                return;
            m_AudioSource.PlayOneShot (m_targetAcquiredSound);

        }


        private void PlayerLost() {
            if (m_playerController == null || m_playerController.isDead)
                return;
            m_AudioSource.PlayOneShot (m_targeLostSound);
        }

        public void PlayerKilled() {
            m_AudioSource.PlayOneShot (m_killedSound);
        }


        private void checkForGrounded() {

            if (m_previouslyGrounded && !m_grav.isGrounded) {
                m_lastJumpTime = Time.time;
            }
            if (!m_previouslyGrounded && m_grav.isGrounded) {
                m_lastLandTime = Time.time;
            }


            m_previouslyGrounded = m_grav.isGrounded;
        }
    }
}


