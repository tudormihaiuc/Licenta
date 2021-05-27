using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiManager : MonoBehaviour
{
    public GameObject[] aiSpawnLocations;
    public GameObject ai_prefab;
    // Start is called before the first frame update
    void Start()
    {
        if(GameSettings.GameMode==GameMode.SOLO)
            SpawnAi();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SpawnAi(){
        int spawn = Random.Range(0, aiSpawnLocations.Length);
        GameObject newPlayer=Instantiate(ai_prefab, aiSpawnLocations[spawn].transform.position, aiSpawnLocations[spawn].transform.rotation) as GameObject;
    }
}
