using System;
using UnityEngine;
using UnityEngine.UI;

public class UIPanelWin : MonoBehaviour, IMenu
{
    [SerializeField] private Button btnOk;

    private UIMainManager m_uiMainManager;

    private void Awake()
    {
        if (btnOk != null)
        {
            btnOk.onClick.AddListener(OnClickOk);
        }
    }

    private void OnDestroy()
    {
        if (btnOk != null)
        {
            btnOk.onClick.RemoveAllListeners();
        }
    }

    public void Setup(UIMainManager uiMainManager)
    {
        m_uiMainManager = uiMainManager;
    }

    private void OnClickOk()
    {
        m_uiMainManager.ShowMainMenu();
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}