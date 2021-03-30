using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
public class Footsteps : MonoBehaviourPunCallbacks
{
    PlayerController player;
    public AudioSource sfx;
    // Start is called before the first frame update
    void Start()
    {
        player=GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        sfx.Play();
        photonView.RPC("PlayFootstep", RpcTarget.All);
    }
    [PunRPC]
    public void PlayFootstep(){
        sfx.Play();
    }
}
