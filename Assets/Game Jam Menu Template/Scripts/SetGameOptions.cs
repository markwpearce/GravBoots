using UnityEngine;
using System.Collections;
using UnityEngine.Audio;
using UnityEngine.UI;
using GravBoots;

using UnityEngine.SceneManagement;

public class SetGameOptions : MonoBehaviour {

    public GravBootsGameController gameController; 

    //Call this function and pass in the float parameter musicLvl to set the volume of the AudioMixerGroup Music in mainMixer
    public void SetStationSize(float stationSize)
    {
        if (stationSize == 0)
            stationSize = 4;
        gameController.StationSize = Mathf.CeilToInt(stationSize)+1;
    }


    public void SetDifficulty(float difficulty)
    {
        if (difficulty == 0)
            difficulty = 1;
        gameController.Difficulty = Mathf.CeilToInt(difficulty);
    }


    public void QuitToMain() {
        //Load the selected scene, by scene index number in build settings
        SceneManager.LoadScene (0);
    }

}
