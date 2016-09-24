using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace GravBoots
{
    public class GravBootsGameController : MonoBehaviour
    {
        [SerializeField] GameObject m_crossHairs;
        [SerializeField] Text m_healthText;
        [SerializeField] Text m_statusText;
        [SerializeField] GravBootsFirstPersonController m_player;
        [SerializeField] Camera m_buildCamera;


        public int StationSize = 4;
        public int Seed = 0;
        public int Difficulty = 1;

        public GravBootsFirstPersonController Player {
            get {
                return m_player;
            }
            set {
                m_player = value;
                if (m_player) {
                    m_player.healthText = m_healthText;
                    if (m_buildCamera)
                        m_buildCamera.gameObject.SetActive (false);
                } else {
                    if (m_buildCamera)
                        m_buildCamera.gameObject.SetActive (true);
                }
            }
        }

        private List<GravityAIController> m_enemies = new List<GravityAIController>();


        bool gameOver = false;


        void Start() {
            GameObject[] entities = GameObject.FindGameObjectsWithTag ("Entity");

            foreach (GameObject entity in entities) {
                addEnemy (entity);
            }


        }


        void Reset() {
            m_enemies = new List<GravityAIController>();
            m_player = null;
            if (m_buildCamera)
                m_buildCamera.gameObject.SetActive (true);
        }



        public void addEnemy(GameObject enemy) {
            GravityAIController ai = enemy.GetComponent<GravityAIController> ();
            if (ai != null && !m_enemies.Contains(ai)) {
                m_enemies.Add (ai);
            }
        }

        void Update() {
        
            if(m_player != null  && m_player.isActiveAndEnabled && !m_player.isDead) {
                m_crossHairs.SetActive (true);
                m_healthText.gameObject.SetActive (true);
            }

            if(!gameOver && m_player != null  && m_player.isActiveAndEnabled && m_player.isDead) {
                m_crossHairs.SetActive (false);
                m_healthText.gameObject.SetActive (false);
                m_statusText.text = "All Is Lost";
                m_statusText.gameObject.SetActive (true);
                gameOver = true;
            }


            if (!gameOver &&m_enemies.Count > 0 && allEnemiesDead ()) {
                m_crossHairs.SetActive (false);
                m_statusText.text = "Station Secure";
                m_statusText.gameObject.SetActive (true);
                gameOver = true;
            }

        }

        private bool allEnemiesDead() {
            bool dead = true;
            for(int i = 0; i< m_enemies.Count; i++) {
                GravityAIController ai = m_enemies [i];
                if (!ai.isDead) {
                    dead = false;
                    break;
                }
            }
            return dead;
        }
       

    }
}

