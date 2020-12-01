﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sway : MonoBehaviour
{
   public float intesnsity;
   public float smooth;
   private Quaternion origin_rotation;
   public bool isMine;

    private void Start() {
        origin_rotation=transform.localRotation;
    }
   private void Update() {
       UpdateSway();
   }

   private void UpdateSway(){
       float t_x_mouse=Input.GetAxis("Mouse X");
       float t_y_mouse=Input.GetAxis("Mouse Y");
       if(!isMine){
           t_x_mouse=0;
           t_y_mouse=0;
                  }

       //calculate target rotation
       Quaternion t_x_adj=Quaternion.AngleAxis(-1*intesnsity*t_x_mouse,Vector3.up);
       Quaternion t_y_adj=Quaternion.AngleAxis(intesnsity*t_y_mouse,Vector3.right);
       Quaternion target_rotation=origin_rotation*t_x_adj*t_y_adj;
       //rotate towards target rotation
       transform.localRotation=Quaternion.Lerp(transform.localRotation,target_rotation,Time.deltaTime*smooth);
   }
}
