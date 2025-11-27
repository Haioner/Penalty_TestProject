using UnityEngine;
using Fusion;
using TMPro;

public class PingDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI pingText;

    [Header("Display Settings")]
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private string prefix = "Ping: ";
    [SerializeField] private string suffix = "ms";

    [Header("Color Coding")]
    [SerializeField] private bool useColorCoding = true;
    [SerializeField] private Color goodPingColor = Color.green;
    [SerializeField] private Color mediumPingColor = Color.yellow;
    [SerializeField] private Color badPingColor = Color.red;
    [SerializeField] private int goodPingThreshold = 50;
    [SerializeField] private int mediumPingThreshold = 100;

    private NetworkRunner runner;
    private float updateTimer;

    private void Start()
    {
        updateTimer = updateInterval;
        FindRunner();
    }

    private void Update()
    {
        if (runner == null)
        {
            FindRunner();
            return;
        }

        updateTimer -= Time.deltaTime;
        if (updateTimer <= 0f)
        {
            UpdatePingDisplay();
            updateTimer = updateInterval;
        }
    }

    private void FindRunner()
    {
        if (runner == null)
        {
            runner = FindFirstObjectByType<NetworkRunner>();
        }
    }

    private void UpdatePingDisplay()
    {
        if (pingText == null)
            return;

        if (runner == null || !runner.IsRunning)
        {
            pingText.text = $"{prefix}--{suffix}";
            return;
        }

        double rtt = runner.GetPlayerRtt(runner.LocalPlayer);
        int pingMs = Mathf.RoundToInt((float)(rtt * 1000.0));

        pingText.text = $"{prefix}{pingMs}{suffix}";

        if (useColorCoding)
        {
            if (pingMs <= goodPingThreshold)
            {
                pingText.color = goodPingColor;
            }
            else if (pingMs <= mediumPingThreshold)
            {
                pingText.color = mediumPingColor;
            }
            else
            {
                pingText.color = badPingColor;
            }
        }
    }

    public void SetPingText(TextMeshProUGUI textComponent)
    {
        pingText = textComponent;
    }
}
