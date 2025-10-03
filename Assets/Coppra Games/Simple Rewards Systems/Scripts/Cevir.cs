using UnityEngine;
using EasyUI.CarkUI;
using UnityEngine.UI;
using TMPro;

public class Cevir : MonoBehaviour
{
    [SerializeField] private Cark cark;
    [SerializeField] private Button uiSpinButton;
    [SerializeField] private TextMeshProUGUI uiSpinButtonText;

    [Header("2x Durumu için Text")]
    [SerializeField] private TextMeshProUGUI doubleRewardText;

    [Header("Skip Animation Butonu")]
    [SerializeField] private Button skipButton; // Inspector’dan bağla

    private void Start()
    {
        UpdateDoubleRewardText();

        uiSpinButton.onClick.AddListener(() =>
        {
            uiSpinButton.interactable = false;
            uiSpinButtonText.text = "Dönüyor";

            // Skip butonu görünür olsun
            skipButton.gameObject.SetActive(true);

            cark.OnSpinStart(() => Debug.Log("Döndürülüyor"));

            cark.OnSpinEnd(carkParca =>
            {
                Debug.Log("Döndürüldü, Kazanılan: " + carkParca.Label + ", Miktar: " + carkParca.Amount);

                uiSpinButton.interactable = true;
                uiSpinButtonText.text = "Döndür";

                // Spin bitince skip butonu gizle
                skipButton.gameObject.SetActive(false);
            });

            cark.Spin();
        });

        // Skip butonuna basınca çark hızlansın
        skipButton.onClick.AddListener(() => {
            cark.SkipSpin();
            skipButton.gameObject.SetActive(false);
        });

        // Başlangıçta gizli olsun
        skipButton.gameObject.SetActive(false);
    }

    private void Update()
    {
        UpdateDoubleRewardText();
    }

    private void UpdateDoubleRewardText()
    {
        if (doubleRewardText != null)
            doubleRewardText.text = cark.DoubleReward ? "2x = ON" : "2x = OFF";
    }
}
