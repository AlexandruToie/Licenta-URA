using UnityEngine;
using UnityEngine.UI;

public class TabSystem : MonoBehaviour
{
    [Header("Tab Buttons")]
    public Button tabOverview;
    public Button tabDepartments;
    public Button tabHR;

    [Header("Pages")]
    public GameObject pageOverview;
    public GameObject pageDepartments;
    public GameObject pageHR;

    [Header("Active Design")]
    public Color activeColor = Color.white;
    public Color normalColor = Color.gray;

    void Start()
    {
        tabOverview.onClick.AddListener(() => OpenPage(pageOverview, tabOverview));
        tabDepartments.onClick.AddListener(() => OpenPage(pageDepartments, tabDepartments));
        tabHR.onClick.AddListener(() => OpenPage(pageHR, tabHR));

        OpenPage(pageOverview, tabOverview);
    }

    void OpenPage(GameObject targetPage, Button targetButton)
    {
        pageOverview.SetActive(false);
        pageDepartments.SetActive(false);
        pageHR.SetActive(false);

        tabOverview.image.color = normalColor;
        tabDepartments.image.color = normalColor;
        tabHR.image.color = normalColor;

        targetPage.SetActive(true);
        targetButton.image.color = activeColor; 
    }
}