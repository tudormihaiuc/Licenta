using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//enum keeping the game modes
public enum GameMode{
        F4A=0,
        SOLO=1
    }
public class GameSettings : MonoBehaviour
{
    //sets the current gameMode to free for all mode
    public static GameMode GameMode=GameMode.F4A;
}
