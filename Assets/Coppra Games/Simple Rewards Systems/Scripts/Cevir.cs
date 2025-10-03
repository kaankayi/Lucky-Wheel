using UnityEngine;
using UnityEngine.UI;
using EasyUI.CarkUI;
using TMPro;
using System.Collections;

public class Cevir : MonoBehaviour
{
    [Header("Çark ve Spin Butonları")]
    [SerializeField] private Cark cark;
    [SerializeField] private Button uiSpinButton;
    [SerializeField] private TextMeshProUGUI uiSpinButtonText;

    [Header("Skip Animation Butonu")]
    [SerializeField] private Button skipButton;

    [Header("2x Durumu için Text ve Butonlar")]
    [SerializeField] private TextMeshProUGUI doubleRewardText;
    [SerializeField] private Button doubleRewardOnButton;
    [SerializeField] private Button doubleRewardOffButton;

    [Header("2x Buton Renkleri")]
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color deselectedColor = Color.white;

    private void Start()
    {
        UpdateDoubleRewardText();
        UpdateDoubleRewardButtonColors();

        if (skipButton != null) skipButton.gameObject.SetActive(false);

        uiSpinButton.onClick.AddListener(() =>
        {
            uiSpinButton.interactable = false;
            uiSpinButtonText.text = "Dönüyor";

            if (skipButton != null) skipButton.gameObject.SetActive(true);

            // 2x UI'yı gizle
            SetDoubleRewardUIActive(false);

            cark.OnSpinStart(() => { });

            cark.OnSpinEnd(carkParca =>
            {
                Debug.Log("Döndürüldü, Kazanılan: " + carkParca.Label + ", Miktar: " + carkParca.Amount);

                uiSpinButton.interactable = true;
                uiSpinButtonText.text = "Döndür";

                if (skipButton != null) skipButton.gameObject.SetActive(false);

                // 0.6 saniye sonra 2x UI'yı göster
                StartCoroutine(ShowDoubleRewardUIAfterDelay(0.32f));
            });

            cark.Spin();
        });

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(() =>
            {
                cark.SkipSpin();
                skipButton.gameObject.SetActive(false);
            });
        }

        if (doubleRewardOnButton != null)
            doubleRewardOnButton.onClick.AddListener(() =>
            {
                cark.SetDoubleReward(true);
                UpdateDoubleRewardButtonColors();
            });

        if (doubleRewardOffButton != null)
            doubleRewardOffButton.onClick.AddListener(() =>
            {
                cark.SetDoubleReward(false);
                UpdateDoubleRewardButtonColors();
            });
    }

    private void UpdateDoubleRewardText()
    {
        if (doubleRewardText != null)
            doubleRewardText.text = "2x =";
    }

    private void UpdateDoubleRewardButtonColors()
    {
        if (doubleRewardOnButton != null)
            doubleRewardOnButton.GetComponent<Image>().color = cark.DoubleReward ? selectedColor : deselectedColor;

        if (doubleRewardOffButton != null)
            doubleRewardOffButton.GetComponent<Image>().color = !cark.DoubleReward ? selectedColor : deselectedColor;
    }

    private void SetDoubleRewardUIActive(bool active)
    {
        if (doubleRewardText != null) doubleRewardText.gameObject.SetActive(active);
        if (doubleRewardOnButton != null) doubleRewardOnButton.gameObject.SetActive(active);
        if (doubleRewardOffButton != null) doubleRewardOffButton.gameObject.SetActive(active);
    }

    private IEnumerator ShowDoubleRewardUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetDoubleRewardUIActive(true);
    }
}
