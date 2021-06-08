using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//manages the menu tabs from the array of menus
public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;
    [SerializeField] Menu[] menus;

    private void Start() {
        Pause.paused=false;
        Cursor.lockState=CursorLockMode.None;
        Cursor.visible=true;
    }
    void Awake(){
        Instance=this;
    }
    public void OpenMenu(string menuName){
        for(int i=0;i<menus.Length;i++){
            if(menus[i].menuName==menuName){
                OpenMenu(menus[i]);
            }
            else if(menus[i].open){
                CloseMenu(menus[i]);
            }
        }
    }

    public void OpenMenu(Menu menu){
        for(int i=0;i<menus.Length;i++){
            if(menus[i].open){
                CloseMenu(menus[i]);
            }
        }
        menu.Open();
    }

    public void CloseMenu(Menu menu){
        menu.Close();
    }
}
