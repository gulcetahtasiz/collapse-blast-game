using UnityEngine;
using TMPro; 

public class UI : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField inputM;
    public TMP_InputField inputN;
    public TMP_InputField inputK;
    public TMP_InputField inputA;
    public TMP_InputField inputB;
    public TMP_InputField inputC;

    [Header("References")]
    public GameManager gameManager;
    public GameObject panel; 

    public void OnStartButtonPressed(){
        int m = int.Parse(inputM.text);
        int n = int.Parse(inputN.text);
        int k = int.Parse(inputK.text);
        int a = int.Parse(inputA.text);
        int b = int.Parse(inputB.text);
        int c = int.Parse(inputC.text);
        
        if (m < 2 || m > 10 ||
            n < 2 || n > 10 ||
            k < 1 || k > 6 ||
            a <= 0 || b <= a || c <= b) {
            Debug.LogWarning("Invalid input");
            return;
        }


        gameManager.StartGame(m, n, k, a, b, c);

        // close ui
        panel.SetActive(false);
    }
}
