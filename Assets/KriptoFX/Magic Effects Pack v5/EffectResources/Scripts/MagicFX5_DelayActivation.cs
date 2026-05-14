using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MagicFX5
{
    public class MagicFX5_DelayActivation : MonoBehaviour
    {
        public GameObject GameObject;
        public float      Delay    = 1;
        public float      LifeTime = -1;

        
        void OnEnable()
        {
            if (GameObject == null) return;
            GameObject.SetActive(false);
            Invoke("DelayActivate",               Delay);
            if (LifeTime > 0) Invoke("DelayDeactivate", Delay + LifeTime);
        }

        void OnDisable()
        {
            if (GameObject == null) return;
            GameObject.SetActive(false);
            CancelInvoke("DelayActivate");
            CancelInvoke("DelayDeactivate");
        }

        void DelayActivate()
        {
            GameObject.SetActive(true);
        }

        void DelayDeactivate()
        {
            GameObject.SetActive(false);
        }
    }
}
