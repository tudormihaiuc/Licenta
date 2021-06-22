using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

//handles the picking up of weapons 
public class Pickup : MonoBehaviourPunCallbacks
{
    public Gun gun;
    public Weapon weapon;
    public float cooldown;
    public List<GameObject> targets;
    public GameObject gunDisplay;
    private float wait;
    private bool isDisabled;
    
    //on collision, triggers an action
    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody == null)
        {
            return;
        }
        //if the obj that collided is the player
        if (other.attachedRigidbody.gameObject.tag.Equals("Player"))
        {
            Debug.Log("Player entered collider");
            Weapon weaponController = other.attachedRigidbody.gameObject.GetComponent<Weapon>();
            //call the pickupWeapon function over the network
            weaponController.photonView.RPC("PickupWeapon", RpcTarget.All, gun.name);
            //call the GetAmmo function on pickup of a new weapon
            weapon.GetAmmo();
            //photonView.RPC("Disable", RpcTarget.All);
        }
    }
    private void Start()
    {
        foreach (Transform t in gunDisplay.transform)
        {
            Destroy(t.gameObject);
        }
        //uppon start, display the coresponding weapon display prefab
        GameObject newDisplay = Instantiate(gun.display, gunDisplay.transform.position, gunDisplay.transform.rotation) as GameObject;
        newDisplay.transform.SetParent(gunDisplay.transform);
    }

}
