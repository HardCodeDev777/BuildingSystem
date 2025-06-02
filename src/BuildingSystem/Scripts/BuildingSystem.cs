using AYellowpaper.SerializedCollections;
using HardCodeDev.Attributes;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HardCodeDev.BuildingSystem
{
    public class BuildingSystem : MonoBehaviour
    {
        [System.Serializable]
        private struct SavedBuilding
        {
            public string prefabName;
            public Vector3 position;
            public Quaternion rotation;

            public SavedBuilding(string prefabName, Vector3 postiion, Quaternion rotation)
            {
                this.prefabName = prefabName;
                this.position = postiion;
                this.rotation = rotation;
            }
        }

        [System.Serializable]
        private struct SavedBuildingsList
        {
            public List<SavedBuilding> savedBuildings;

            public SavedBuildingsList(List<SavedBuilding> buildings)
            {
                this.savedBuildings = buildings;
            }
        }

        [SerializeField] private Material _selectedMaterial, _deleteMaterial;
        [SerializeField] private GameObject _playerCamera;
        [SerializeField] private float _buildingToCameraDistance;
        [SerializeField, Tooltip("Path to JSON file. For example: Assets/BuildingSystem/SavedData.json")] private string _jsonPath;

        [SerializedDictionary("Key to build", "Building prefab")]
        public SerializedDictionary<KeyCode, GameObject> buildingsPrefabs;

        [SerializeField, NotInteractable] private Material _currentDefaultMat;

        private GameObject _currentBuilding;
        private bool _isHoldingByCamera, _isDeleting, _isDefaultMatDeleting;
        private SavedBuildingsList _buildingsList = new(new());
        private List<SavedBuilding> _runtimeSavedBuildings = new();

        public static BuildingSystem Instance { get; private set; }
        public bool InEditMode { get; private set; }

        private void Awake() => Instance = this;

        private void Start() => LoadFromJSON();

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab)) InEditMode = !InEditMode;

            if (InEditMode)
            {
                Build();
                Delete();
            }
        }

        private void Build()
        {
            foreach (var building in buildingsPrefabs)
            {
                if (Input.GetKeyDown(building.Key))
                {
                    if (_currentBuilding is null)
                    {
                        _currentBuilding = Instantiate(building.Value);

                        _currentDefaultMat = GetRendererMaterial();
                        ApplyMaterial(_selectedMaterial);

                        _isHoldingByCamera = true;
                    }
                }

                if (Input.GetMouseButtonDown(0))
                {
                    if (_currentBuilding is not null)
                    {
                        _isHoldingByCamera = false;
                        ApplyMaterial(_currentDefaultMat);

                        SaveToJSON(new(_currentBuilding.name, _currentBuilding.transform.position, _currentBuilding.transform.rotation));

                        _currentBuilding = null;
                    }
                }
            }

            if (_isHoldingByCamera)
            {
                if (_currentBuilding is not null)
                {
                    var screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, _buildingToCameraDistance);
                    _currentBuilding.transform.position = _playerCamera.GetComponent<Camera>().ScreenToWorldPoint(screenCenter);
                    _currentBuilding.transform.rotation = _playerCamera.transform.rotation;
                }
            }
        }

        private void SaveToJSON(SavedBuilding savedBuilding)
        {
            _buildingsList.savedBuildings.Add(savedBuilding);
            _runtimeSavedBuildings.Add(savedBuilding);
            var json = JsonUtility.ToJson(_buildingsList);
            File.WriteAllText(_jsonPath, json);
            AssetDatabase.Refresh();
        }

        private void LoadFromJSON()
        {
            if (File.Exists(_jsonPath))
            {
                var json = File.ReadAllText(_jsonPath);
                var data = JsonUtility.FromJson<SavedBuildingsList>(json);
                foreach (var building in data.savedBuildings)
                {
                    var newPrefabName = building.prefabName.Replace("(Clone)", "");
                    foreach (var build in buildingsPrefabs.Values)
                    {
                        if (build.name == newPrefabName) Instantiate(build, building.position, building.rotation);
                    }
                }
            }
        }

        private void Delete()
        {
            var ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 4f))
            {
                if (hit.collider.gameObject.CompareTag("Building") && !_isHoldingByCamera)
                {
                    _currentBuilding = hit.collider.gameObject;
                    if (!_isDefaultMatDeleting)
                    {
                        _currentDefaultMat = GetRendererMaterial();
                        _isDefaultMatDeleting = true;
                    }
                    ;

                    _isDeleting = true;
                    if (Input.GetKeyDown(KeyCode.Q))
                    {
                        foreach (var saved in _runtimeSavedBuildings)
                        {
                            if (saved.position == _currentBuilding.transform.position && saved.rotation == _currentBuilding.transform.rotation && saved.prefabName == _currentBuilding.name)
                            {
                                _buildingsList.savedBuildings.Remove(saved);
                                _runtimeSavedBuildings.Remove(saved);
                                var json = JsonUtility.ToJson(_buildingsList);
                                File.WriteAllText(_jsonPath, json);
                                AssetDatabase.Refresh();
                                break;
                            }
                        }
                        Destroy(_currentBuilding);
                        _currentBuilding = null;
                    }
                }
                else _isDeleting = false;
            }

            if (_isDeleting)
            {
                // Fix it later
               if (_currentBuilding is not null && !_isHoldingByCamera) ApplyMaterial(_deleteMaterial);
            }
            else
            {
                if (_currentBuilding is not null && !_isHoldingByCamera)
                {
                    ApplyMaterial(_currentDefaultMat);
                    _isDefaultMatDeleting = false;
                }
                else if (_currentBuilding is not null && _isHoldingByCamera)
                {
                    _isDefaultMatDeleting = false;
                }
            }
        }

        private Material GetRendererMaterial()
        {
            if (_currentBuilding.TryGetComponent<MeshRenderer>(out _)) return _currentBuilding.GetComponent<MeshRenderer>().material;

            else return _currentBuilding.GetComponentInChildren<MeshRenderer>().material;
        }

        private void ApplyMaterial(Material mat)
        {
            if (_currentBuilding.TryGetComponent<MeshRenderer>(out _)) _currentBuilding.GetComponent<MeshRenderer>().material = mat;

            else _currentBuilding.GetComponentInChildren<MeshRenderer>().material = mat;
        }
    }
}