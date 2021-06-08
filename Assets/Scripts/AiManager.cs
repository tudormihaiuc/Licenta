using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//class that takes care of spawning AI Enemies
public class AiManager : MonoBehaviour
{
    public GameObject[] aiSpawnLocations;
    public GameObject ai_prefab;
    void Start()
    {
        if(GameSettings.GameMode==GameMode.SOLO)
            SpawnAi();
    }
    //gets a random spawn position from the enemy locations array and spawns the ai_prefab
    public void SpawnAi(){
        int spawn = Random.Range(0, aiSpawnLocations.Length);
        GameObject newPlayer=Instantiate(ai_prefab, aiSpawnLocations[spawn].transform.position, aiSpawnLocations[spawn].transform.rotation) as GameObject;
    }
}
