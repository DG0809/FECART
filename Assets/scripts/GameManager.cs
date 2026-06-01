using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("UI de Munição")]
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI reserveText;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;
    }

    public void UpdateAmmo(int balasNoPente, int tamanhoDoPente, int balasNoBolso)
    {
        if (ammoText != null)
        {
            ammoText.text = "Pente: " + balasNoPente + " / " + tamanhoDoPente;
        }

        if (reserveText != null)
        {
            reserveText.text = "Bolso: " + balasNoBolso;
        }
    }
}