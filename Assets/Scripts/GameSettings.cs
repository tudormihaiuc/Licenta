using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameMode{
        F4A=0,
        SOLO=1
    }
public class GameSettings : MonoBehaviour
{
    public static GameMode GameMode=GameMode.F4A;
}
