using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Characters.FirstPerson;

namespace GravBoots
{
    [Serializable]
    public class GravityMouseLook
    {
        public float XSensitivity = 2f;
        public float YSensitivity = 2f;
        public bool clampVerticalRotation = true;
        public float MinimumX = -90F;
        public float MaximumX = 90F;
        public bool smooth;
        public float smoothTime = 5f;
        public bool lockCursor = true;


        private Quaternion m_CharacterTargetRot;
        private Quaternion m_CameraTargetRot;
        private bool m_cursorIsLocked = true;

        private CustomGravity m_grav;

        public void Init(Transform character, Transform camera, CustomGravity grav)
        {
            m_CharacterTargetRot = character.localRotation;
            m_CameraTargetRot = camera.localRotation;
            m_grav = grav;
        }


        public void LookRotation(Transform character, Transform camera, bool deadClamp)
        {
            float yRot = CrossPlatformInputManager.GetAxis("Mouse X") * XSensitivity;
            float xRot = CrossPlatformInputManager.GetAxis("Mouse Y") * YSensitivity;

            m_CharacterTargetRot = character.localRotation;
           
            //Change character and camera rotation in all circumstances
            m_CharacterTargetRot *= Quaternion.Euler (0f, yRot, 0f);
            m_CameraTargetRot *= Quaternion.Euler (-xRot, 0f, 0f);



            if((clampVerticalRotation && m_grav.isGrounded) || deadClamp)
                m_CameraTargetRot = ClampRotationAroundXAxis (m_CameraTargetRot, deadClamp);

            if (deadClamp) {
                m_CharacterTargetRot = ClampRotationAroundYAxis (m_CharacterTargetRot, deadClamp);
            }


            if(smooth)
            {
                character.localRotation = Quaternion.Slerp (character.localRotation, m_CharacterTargetRot,
                    smoothTime * Time.deltaTime);
                camera.localRotation = Quaternion.Slerp (camera.localRotation, m_CameraTargetRot,
                    smoothTime * Time.deltaTime);
            }
            else
            {
                character.localRotation = m_CharacterTargetRot;
                camera.localRotation = m_CameraTargetRot;
            }

            UpdateCursorLock();
        }

        public void SetCursorLock(bool value)
        {
            lockCursor = value;
            if(!lockCursor)
            {//we force unlock the cursor if the user disable the cursor locking helper
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void UpdateCursorLock()
        {
            //if the user set "lockCursor" we check & properly lock the cursos
            if (lockCursor)
                InternalLockUpdate();
        }

        private void InternalLockUpdate()
        {
            if(Input.GetKeyUp(KeyCode.Escape))
            {
                m_cursorIsLocked = false;
            }
            else if(Input.GetMouseButtonUp(0))
            {
                m_cursorIsLocked = true;
            }

            if (m_cursorIsLocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (!m_cursorIsLocked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        Quaternion ClampRotationAroundXAxis(Quaternion q, bool dead)
        {
            q.x /= q.w;
            q.y /= q.w;
            q.z /= q.w;
            q.w = 1.0f;

            float angle = 2.0f * Mathf.Rad2Deg * Mathf.Atan (q.x);

            float min = dead ? MinimumX / 4 : MinimumX;
            float max = dead ? MaximumX / 4 : MaximumX;


            angle = Mathf.Clamp (angle, min, max);

            q.x = Mathf.Tan (0.5f * Mathf.Deg2Rad * angle);

            return q;
        }

        Quaternion ClampRotationAroundYAxis(Quaternion q, bool dead)
        {
            q.x /= q.w;
            q.y /= q.w;
            q.z /= q.w;
            q.w = 1.0f;

            float angle = 2.0f * Mathf.Rad2Deg * Mathf.Atan (q.y);

            float min = dead ? MinimumX / 4 : MinimumX;
            float max = dead ? MaximumX / 4 : MaximumX;


            angle = Mathf.Clamp (angle, min, max);

            q.x = Mathf.Tan (0.5f * Mathf.Deg2Rad * angle);

            return q;
        }

    }
}
