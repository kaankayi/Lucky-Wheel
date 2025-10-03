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
    [SerializeField] private TextMeshProUGUI doubleRewardText; // Buraya inspector’dan bağlayacağız
    private void Start()
    {
         // Başlangıçta 2x durumunu göster
        UpdateDoubleRewardText();

        // Spin butonuna basıldığında
        uiSpinButton.onClick.AddListener(() =>
        {
            uiSpinButton.interactable = false;   // butonu devre dışı bırak
            uiSpinButtonText.text = "Dönüyor";

            // Spin start event
            cark.OnSpinStart(() => Debug.Log("Döndürülüyor"));

            // Spin end event
            cark.OnSpinEnd(carkParca =>
            {
                Debug.Log("Döndürüldü, Kazanılan: " + carkParca.Label + ", Miktar: " + carkParca.Amount);

                uiSpinButton.interactable = true;   // butonu tekrar aktif et
                uiSpinButtonText.text = "Döndür";
            });

            // Çarkı döndür
            cark.Spin();
        });
    }

    private void Update()
    {
        // 2x durumu değişirse text otomatik güncellensin
        UpdateDoubleRewardText();
    }

    // 2x durumuna göre Text güncelle
    private void UpdateDoubleRewardText()
    {
        if (doubleRewardText != null)
            doubleRewardText.text = cark.DoubleReward ? "2x = ON" : "2x = OFF";
    }
}