using System.Globalization;
using TMPro;
using UnityEngine;

namespace EmpireAtWar
{
    public class FpsCounter : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI fpsText;


        private void Update()
        {
            fpsText.text = Mathf.Ceil(1.0f / Time.deltaTime).ToString(CultureInfo.InvariantCulture);
        }
    }
}
