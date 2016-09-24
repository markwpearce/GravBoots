using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace GravBoots
{
    public class StationBuilder : MonoBehaviour
    {

        [SerializeField] private GameObject[] m_sectionPrefabs; // Has multiple ports
        [SerializeField] private GameObject[] m_endSectionPrefabs;  //Has one port
        [SerializeField] private string m_portTag = "Port"; //transforms tagged with this are ports
        [SerializeField] private string m_spaceStationColliderTag = "SpaceStationSection"; //transforms tagged with this are sections
        [SerializeField] private string m_entityLocationTag = "EntityLocation"; //transforms tagged with this are sections
        [SerializeField] public int stationSize = 4;
        [SerializeField] public int seed = 0;
        private int m_maxFailures = 5;
        [SerializeField] public int enemiesPerSection = 1;
        [SerializeField] private GameObject m_enemyPrefab;
        [SerializeField] private GameObject m_playerPrefab;
        [SerializeField] public GravBootsGameController gameController; // Has multiple ports


        [SerializeField] private float m_creationSpeed = 5; //number of sections or entities to add per second

        private System.Random rand;

        private bool m_generated = false;
        private bool m_entitiesPlaced = false;
        private int m_levelSize = 0;
        private int m_enemiesCount = 0;
        private int m_enemiesMax = 0;

        private bool m_playerAdded;

        private int m_failureCount = 0;

        private float m_lastBuildActionTime = 0;

        GameObject m_stationParent;
        GameObject m_mainSection;
        List<GameObject> m_stationPorts;

        List<GameObject> m_entityLocations;


        List<GameObject> m_enemies = new List<GameObject>();
        GameObject m_playerGameObj;

        void Awake () {

            if (stationSize == 0) {
                stationSize = 4;
            }

            if (enemiesPerSection == 0) {
                enemiesPerSection = 1;
            }

            Debug.Log("Staion Builder - size: "+stationSize+", enemiesPerSection: "+enemiesPerSection+", seed: "+seed);

            if (seed == 0) {
                rand = new System.Random ((int)Time.time);
            } else {
                rand = new System.Random (seed);
            }
            GenerateSpaceStationMainSection ();
            m_enemiesMax =stationSize * enemiesPerSection;
            m_maxFailures = stationSize * 5;

         
        }


        //Gets a random section
        GameObject getSection() {
            int randomIndex = rand.Next(m_sectionPrefabs.Length);
            return m_sectionPrefabs[randomIndex];   
        }

        //Gets a random end
        GameObject getEnd() {
            int randomIndex = rand.Next(m_endSectionPrefabs.Length);
            return m_endSectionPrefabs[randomIndex];   
        }


        GameObject getRandomFrom(List<GameObject> gameObjects) {
            int randomIndex = rand.Next(gameObjects.Count);
            return gameObjects [randomIndex];
        }

        List<GameObject> getChildGameObjectsTaggedWith(GameObject section, string tag) {
            List<GameObject> gameObjs = new List<GameObject> ();

            foreach (Transform child in section.transform) {
                if(child.CompareTag(tag)) {
                    gameObjs.Add(child.gameObject);
                }
                gameObjs.AddRange (getChildGameObjectsTaggedWith (child.gameObject, tag));
            }
            return gameObjs;          
        }

        //Recursive function to find all child transforms tagged with m_portTag
        List<GameObject> getPorts(GameObject section) {
            return getChildGameObjectsTaggedWith (section, m_portTag);
        }

        //Recursive function to find all child transforms tagged with m_entityLocationTag
        List<GameObject> getEntityPlaces(GameObject section) {
            return getChildGameObjectsTaggedWith (section, m_entityLocationTag);
        }

        //Recursive function to find all child box colliders tagged with m_spaceStationColliderTag
        List<BoxCollider> getStationColliders(GameObject section) {
            List<BoxCollider> colliders = new List<BoxCollider> ();

            foreach (Transform child in section.transform) {
                if(child.CompareTag(m_spaceStationColliderTag)) {
                    colliders.Add(child.gameObject.GetComponent<BoxCollider>());
                }
                colliders.AddRange (getStationColliders (child.gameObject));
            }
            return colliders;          
        }

   
        void GenerateSpaceStationMainSection() {
            m_stationParent = new GameObject("SpaceStation");

            m_mainSection = Instantiate (getSection (), Vector3.zero, Quaternion.identity) as GameObject;
            m_mainSection.transform.parent = m_stationParent.transform;
            m_levelSize = 1;
            m_stationPorts = getPorts (m_stationParent);
         }



        void Update() {

            if (m_generated && m_entitiesPlaced) {
                return;
            }

            float speedFactor = !m_generated ? 1.0f : 0.5f; //more fun to watch station being built

            if ( (Time.time - m_lastBuildActionTime )< (speedFactor / m_creationSpeed)) {
                return;
            }
            m_lastBuildActionTime = Time.time;


            bool finishEnds = false;

            if( !m_generated && m_levelSize < stationSize && m_failureCount < m_maxFailures) {
                AddRandomSection ();

                if(m_failureCount >= m_maxFailures) {
                    Debug.Log ("Failed out. level size: "+m_levelSize);
                    finishEnds = true;
                }
                if (m_levelSize >= stationSize) {
                    Debug.Log ("Level complete! level size: "+m_levelSize);
                    finishEnds = true;
                }
            }

           

            if (!m_generated && finishEnds) {
                Debug.Log ("Adding ends...");
                int portsLeft = m_stationPorts.Count;
                for (int i =0; i < portsLeft; i++) {
                    AddEndPort ();
                }

                Debug.Log ("Disabling colliders...");
                foreach (BoxCollider stationCollider in getStationColliders(m_stationParent)) {
                    stationCollider.enabled = false;
                    
                }
                //we've generated the geometry
                m_generated = true;

                //get the possible enemy locations
                m_entityLocations = getEntityPlaces (m_stationParent);

                //Set the maximum number of enemies to the min of recommended enemies or one less than possible locations
                //we need one space for the player
                Debug.Log ("Possible enemy locations: "+m_entityLocations.Count);
                m_enemiesMax = Math.Min (m_enemiesMax, m_entityLocations.Count - 2);
                Debug.Log ("Adding enemies: "+m_enemiesMax);

            }

            bool addPlayer = false;


            if (m_generated && (m_enemiesCount < (m_enemiesMax))) {
                Debug.Log ("Adding enemy: "+(m_enemiesCount+1));
                AddEnemy ();
                if (m_enemiesCount >= m_enemiesMax) {
                    Debug.Log ("Done adding enemies: "+m_enemiesCount);
                    addPlayer = true;
                }
            }

            if (m_generated && addPlayer) {
                AddPlayer ();
                Debug.Log ("Added player!");

                //Disable the view of entity locations
                for (int i =0; i < m_entityLocations.Count; i++) {
                    m_entityLocations[i].SetActive (false);
                }

                m_playerGameObj.SetActive (true);
                Debug.Log ("Targeting player...");
                foreach (GameObject enemy in m_enemies) {
                    GravityAIController ai = enemy.GetComponent<GravityAIController> ();
                    ai.SetTarget( m_playerGameObj.transform);
                }

                Debug.Log ("Done generating Space Station!");


                m_entitiesPlaced = true;
            }
        }


        void AddRandomSection() {
            AddSection (getSection (), true);
        }

        void AddEndPort() {
            AddSection (getEnd (), false);
        }



        void AddSection(GameObject sectionToAdd, bool checkCollisions) {
            //This is the existing port to connect to
            GameObject existingPort = getRandomFrom (m_stationPorts);

            //This is the section that will be added
            GameObject newSection = Instantiate (sectionToAdd, Vector3.zero, Quaternion.identity)  as GameObject;
            newSection.transform.parent = m_stationParent.transform;

            newSection.SetActive (true);

            List<GameObject> newPorts = getPorts(newSection);

            //This is the port in the new section that will be connected to existingPort
            GameObject newPort = getRandomFrom (newPorts);
            Vector3 newPortPositionOffset = newPort.transform.position;

            //move new section to where existing port is
            newSection.transform.position = existingPort.transform.position-newSection.transform.TransformDirection(newPortPositionOffset);


            //rotate newSection so newPort has oppposite direction to existingPort
            Quaternion newSectionRot = Quaternion.FromToRotation (
                newPort.transform.up,
                -existingPort.transform.up);

            newSection.transform.rotation = newSectionRot;

            //try different rotations around the port to deal with collisions
            int initialRotationAtPort =  rand.Next (0, 6);

            newSection.transform.RotateAround (newPort.transform.position, newPort.transform.up, initialRotationAtPort*60);

            bool placed = !checkCollisions;
            for(int i = 0; i<6; i++) {
                
                //rotate a random multiple of 60degrees around hexagonal port.
                newSection.transform.RotateAround (newPort.transform.position, newPort.transform.up, 60);

                //move new section to where existing port is
                newSection.transform.position = existingPort.transform.position-newSection.transform.TransformDirection(newPortPositionOffset);

                if (placed) {
                    break;
                }


                //get the box collider of the section and see if it hits anything
                BoxCollider sectionCollider = newSection.GetComponent<BoxCollider>();


                Vector3 newSectionColliderPos = newSection.transform.position - newSection.transform.TransformDirection(sectionCollider.center);

                Debug.Log ("Section collider position: "+ newSectionColliderPos +" rotation: " + newSection.transform.rotation);

                //something wrong with this... :(
                Collider[] hitColliders = Physics.OverlapBox(
                    newSectionColliderPos,
                    sectionCollider.size/2,
                    newSection.transform.rotation);
                //Debug.Log ("Collided with "+ hitColliders.Length);
                bool okToPlace = true;

                //loop through collisons and see if any are "SpaceStationSections" that aren't this section
                for(int c=0; c<hitColliders.Length; c++){// (Collider hitCollider in hitColliders) {
                    Collider hitCollider = hitColliders[c];
                    if (hitCollider.tag == m_spaceStationColliderTag && hitCollider.gameObject.transform != newSection.gameObject.transform) {
                        Debug.Log ("Collision with "+ hitCollider.name);

                        //draw the collision cube, so we can debug.
                        /*GameObject collisionCube = GameObject.CreatePrimitive (PrimitiveType.Cube);
                        collisionCube.transform.position =  newSectionColliderPos;
                        collisionCube.transform.localScale = sectionCollider.size;
                        collisionCube.transform.rotation = newSection.transform.rotation;
                        */
                        okToPlace = false;
                        break;
                    }
                }

                if (okToPlace) {
                    Debug.Log ("Placed section! "+(m_levelSize+1));

                    placed = true;
                    break;
                }

            }

            if (!placed) {
                //try again!
                Debug.Log ("Unable to place section!");
                m_failureCount++;
                newSection.SetActive (false);
                Destroy (newSection);
                return;
            }

            //Remove these ports as they have formed a bond
            m_stationPorts.Remove (existingPort);
            newPorts.Remove(newPort);

            //Disbale view of port transforms
            existingPort.SetActive(false);
            newPort.SetActive(false);


            //Add the other ports from the new section to the list of existing ports
            m_stationPorts.AddRange(newPorts);
            m_levelSize++;
        }

        void AddEnemy() {
            GameObject enemy = AddEntity (m_enemyPrefab);
            m_enemies.Add (enemy);
            gameController.addEnemy (enemy);
            m_enemiesCount+=1;
            Debug.Log ("Added enemy: "+(m_enemiesCount));

        }

        void AddPlayer() {
            if (m_playerAdded)
                return;

            m_playerGameObj = AddEntity (m_playerPrefab);
            gameController.Player = m_playerGameObj.GetComponent<GravBootsFirstPersonController>();

            m_playerAdded = true;
        }

        GameObject AddEntity (GameObject entityPrefab) {
            GameObject location = getRandomFrom (m_entityLocations);
            Debug.Log ("Adding entity "+(entityPrefab.name) + " at "+location.transform.position);
            m_entityLocations.Remove (location);
            location.SetActive (false);
            return Instantiate (entityPrefab, location.transform.position, location.transform.rotation) as GameObject;
        }


    }
}

