using System;
using UnityEngine;

namespace GravBoots 
{
    public class CharacterHealth : MonoBehaviour
    {


        public delegate void HurtEvent();
        public event HurtEvent OnHurt;
        public delegate void DieEvent();
        public event DieEvent OnDie;
        public delegate void HealEvent();
        public event HealEvent OnHeal;




        [SerializeField] float m_maxHealth = 100f;
        [SerializeField] Animator m_animator;

        private float m_health;


        private void Start()
        {
            m_health = m_maxHealth;

        }



        public float Health {
            get {
                return m_health;
            }
        }


        public void Hurt(float amount) {
            if (isDead ()) {
                return;
            }

            m_health -= amount;

            if (m_health <= 0) {
                m_health = 0;
                if (m_animator != null)
                    m_animator.SetTrigger ("Die");
                if (OnDie != null)
                    OnDie ();

            } else {
                if(m_animator != null) m_animator.SetTrigger ("Hit");
                if (OnHurt != null)
                    OnHurt ();
            }

        }

        public void Heal(float amount) {
            m_health += amount;
            if (m_health > m_maxHealth) {
                m_health = m_maxHealth;
            }
            if (OnHeal != null)
                OnHeal ();
        }

        public bool isDead() {
            return m_health == 0;
        }

        public bool fullHealth() {
            return m_health == m_maxHealth;
        }

        public float LifeRatio() {
            return m_health / m_maxHealth;
        }

        public void InstaKill() {
            Hurt (m_maxHealth);
        }


    }
}

