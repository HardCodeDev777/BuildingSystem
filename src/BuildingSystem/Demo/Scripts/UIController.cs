using UnityEngine;
using TMPro;

namespace HardCodeDev.BuildingSystem
{
    public class UIController : MonoBehaviour
    {
        [SerializeField] private TMP_Text _modeText, _buildingText;
        private bool _wasInitialized;
        private void Update()
        {
            _modeText.text = $"{BuildingSystem.Instance.InEditMode}";
            if (BuildingSystem.Instance.InEditMode)
            {
                _buildingText.gameObject.SetActive(true);
                if (!_wasInitialized) foreach (var building in BuildingSystem.Instance.buildingsPrefabs) _buildingText.text += $"{building.Key} - {building.Value.name}, ";

                _wasInitialized = true;
            }
            else _buildingText.gameObject.SetActive(false);
        }
    }

}