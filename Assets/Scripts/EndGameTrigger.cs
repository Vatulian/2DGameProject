using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.UIElements;

public class EndGameTrigger : MonoBehaviour
{
    [SerializeField] private GameObject FadePanel;
    private bool hasTriggered = false;
    private void Awake()
    {
        FadePanel.SetActive(false);
      
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            FadePanel.SetActive(true);
        }
    }
}
