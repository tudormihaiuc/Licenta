using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
public class Pickup : MonoBehaviourPunCallbacks
{
    public Gun gun;
    public Weapon weapon;
    public float cooldown;
    private float wait;
    private bool isDisabled;
    public List<GameObject> targets;
    public GameObject gunDisplay;

    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody == null)
        {
            return;
        }
        if (other.attachedRigidbody.gameObject.tag.Equals("Player"))
        {
            Debug.Log("Player entered collider");
            Weapon weaponController = other.attachedRigidbody.gameObject.GetComponent<Weapon>();
            weaponController.photonView.RPC("PickupWeapon", RpcTarget.All, gun.name);
            weapon.GetAmmo();
            photonView.RPC("Disable", RpcTarget.All);
        }
    }
    private void Start()
    {
        foreach (Transform t in gunDisplay.transform)
        {
            Destroy(t.gameObject);
        }
        GameObject newDisplay = Instantiate(gun.display, gunDisplay.transform.position, gunDisplay.transform.rotation) as GameObject;
        newDisplay.transform.SetParent(gunDisplay.transform);
    }
    private void Update()
    {
        if (isDisabled)
        {
            if (wait > 0)
            {
                wait = -Time.deltaTime;
            }
            else
            {
                //reenable 
                Enable();
            }
        }
    }
    [PunRPC]
    public void Disable()
    {
        Debug.Log("disable");
        isDisabled = true;
        wait = cooldown;
        foreach (GameObject target in targets)
        {
            target.SetActive(false);
        }
    }
    private void Enable()
    {
        isDisabled = false;
        wait = 0;
        foreach (GameObject target in targets)
        {
            target.SetActive(true);
        }
    }
}
