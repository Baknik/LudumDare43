// Copyright (c) 2016 - 2017 Ez Entertainment SRL. All Rights Reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using System.Collections;

namespace Ez.Pooly.Examples
{
    [RequireComponent(typeof(Rigidbody))]
    public class PoolyProjectile : MonoBehaviour
    {
        public bool isEnabled = true;
        public float force = 5f;
        public bool randomForce = false;
        public float minForce = 5f;
        public float maxForce = 10f;
        public Vector3 direction = Vector3.forward;
        public enum LaunchOn { Start, OnSpawned }
        public LaunchOn launchOn = LaunchOn.OnSpawned;
        private Rigidbody rb;
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void Start()
        {
            if (launchOn == LaunchOn.Start) { Launch(); }
        }

        void OnSpawned()
        {
            if (launchOn == LaunchOn.OnSpawned) { Launch(); }
        }

        void Launch()
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForceAtPosition(randomForce ? direction * Random.Range(minForce, maxForce) : direction * force, rb.position, ForceMode.Impulse);
        }
    }
}
