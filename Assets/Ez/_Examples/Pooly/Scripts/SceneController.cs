// Copyright (c) 2016 - 2017 Ez Entertainment SRL. All Rights Reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Ez.Pooly.Examples
{
    public class SceneController : MonoBehaviour
    {
        public GameObject mainCanvas;
        public GameObject[] zoneUI;
        public GameObject[] zoneElements;
        public Vector3[] zoneCameraPosition;
        public Vector3[] zoneCameraRotation;

        public PoolySpawner Zone0Spawner;
        public PoolySpawner[] Zone1Spawners;
        public PoolySpawner Zone2Spawner;
        public PoolySpawner Zone3Spawner;

        int currentZoneIndex = 0;

        bool zone3Loaded = false;

        void Start()
        {
            ChangeZone();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow)) { PreviousZone(); }
            if (Input.GetKeyDown(KeyCode.RightArrow)) { NextZone(); }
            if (Input.GetKeyDown(KeyCode.DownArrow)) { mainCanvas.SetActive(!mainCanvas.activeInHierarchy); }
            switch (currentZoneIndex)
            {
                case 0:
                    if (Input.GetKeyDown(KeyCode.Space)) { if (Zone0Spawner.isSpawning) { Zone0Spawner.StopSpawn(); } else { Zone0Spawner.StartSpawn(); } }
                    break;

                case 1:
                    if (Input.GetKeyDown(KeyCode.Alpha1)) { Zone1Spawners[0].SpawnNext(); }
                    if (Input.GetKeyDown(KeyCode.Alpha2)) { Zone1Spawners[1].SpawnNext(); }
                    if (Input.GetKeyDown(KeyCode.Alpha3)) { Zone1Spawners[2].StartSpawn(); }
                    if (Input.GetKeyDown(KeyCode.Space)) { Zone1StressTest(); }
                    break;

                case 2:
                    if (Input.GetKeyDown(KeyCode.Space)) { if (Zone2Spawner.isSpawning) { Zone2Spawner.StopSpawn(); } else { Zone2Spawner.StartSpawn(); } }
                    break;

                case 3:
                    if (Input.GetKeyDown(KeyCode.Space)) { if (Zone3Spawner.isSpawning) { Zone3Spawner.StopSpawn(); } else { Zone3Spawner.StartSpawn(); } }
                    break;
            }
        }

        public void Zone1StressTest()
        {
            if (Zone1Spawners[0].isSpawning || Zone1Spawners[1].isSpawning || Zone1Spawners[2].isSpawning)
            {
                Zone1Spawners[2].spawnForever = false;
                foreach (var spawner in Zone1Spawners) { spawner.StopSpawn(); }
            }
            else
            {
                Zone1Spawners[2].spawnForever = true;
                foreach (var spawner in Zone1Spawners) { spawner.StartSpawn(); }
            }
        }

        public void NextZone()
        {
            if (currentZoneIndex == zoneUI.Length - 1) { currentZoneIndex = 0; } else { currentZoneIndex++; }
            ChangeZone();
        }

        public void PreviousZone()
        {
            if (currentZoneIndex <= 0) { currentZoneIndex = zoneUI.Length - 1; } else { currentZoneIndex--; }
            ChangeZone();
        }

        private void ChangeZone()
        {
            foreach (var go in zoneUI) { go.SetActive(false); }
            zoneUI[currentZoneIndex].SetActive(true);

            foreach (var go in zoneElements) { if (go == null) { continue; } go.SetActive(false); }
            if (zoneElements[currentZoneIndex] != null) { zoneElements[currentZoneIndex].SetActive(true); }
            if (currentZoneIndex == 3 && !zone3Loaded) { SceneManager.LoadSceneAsync("MainSceneExtension", LoadSceneMode.Additive); zone3Loaded = true; }
            else { if(Zone3Spawner != null && Zone3Spawner.isSpawning) { Zone3Spawner.StopSpawn(); } }
            StopAllCoroutines();
            StartCoroutine(MoveToPosition(Camera.main.transform, zoneCameraPosition[currentZoneIndex], 1f));
            StartCoroutine(RotateToRotation(Camera.main.transform, Quaternion.Euler(zoneCameraRotation[currentZoneIndex]), 1f));
        }

        IEnumerator MoveToPosition(Transform transform, Vector3 position, float timeToMove)
        {
            var currentPosition = transform.position;
            var t = 0f;
            while (t < 1)
            {
                t += Time.deltaTime / timeToMove;
                transform.position = Vector3.Lerp(currentPosition, position, t);
                yield return null;
            }
        }

        IEnumerator RotateToRotation(Transform transform, Quaternion rotation, float timeToMove)
        {
            var currentRotation = transform.rotation;
            var t = 0f;
            while (t < 1)
            {
                t += Time.deltaTime / timeToMove;
                transform.rotation = Quaternion.Lerp(currentRotation, rotation, t);
                yield return null;
            }
        }
    }
}
