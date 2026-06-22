using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    private PlayerClass player;
    public static UIManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI atkText;
    public TextMeshProUGUI defText;
    public TextMeshProUGUI moneyText;

    [Header("Sign Elements")]
    public GameObject signTextBox;
    public GameObject bountyBoardPanel;
    public TextMeshProUGUI messageText; // Replace with TMP_Text if using TextMeshPro

    [Header("Game Over Elements")]
    public GameObject gameOverPanel;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        player = Object.FindFirstObjectByType<PlayerClass>();

        UpdateUI();
    }

    private void Update()
    {
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (player != null)
        {
            atkText.text = player.attack.ToString();
            defText.text = player.defense.ToString();
            moneyText.text = player.money.ToString();
        }
    }

    public void ShowMessage(string msg)
    {
        signTextBox.SetActive(true);
        messageText.text = msg;
    }

    public void HideMessage()
    {
        signTextBox.SetActive(false);
    }

    public void ShowBountyBoard()
    {
        bountyBoardPanel.SetActive(true);
    }

    public void HideBountyBoard()
    {
        bountyBoardPanel.SetActive(false);
    }

    public void ShowGameOver()
    {
        gameOverPanel.SetActive(true);
    }
}
