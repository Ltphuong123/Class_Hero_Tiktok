using UnityEngine;

public class UICanvas : MonoBehaviour
{
    [SerializeField] bool isDestroyOnclose = false;

    private void Awake()
    {
        RectTransform rect = GetComponent<RectTransform>();
        float ratio = (float)Screen.width / (float)Screen.height;
        if (ratio > 2.1f)
        {
            Vector2 LeftBottom = rect.offsetMin;
            Vector2 RightTop = rect.offsetMax;

            LeftBottom.y = 0f;
            RightTop.y = -100f;

            rect.offsetMin = LeftBottom;
            rect.offsetMax = RightTop;
        }
    }
    public virtual void Setup()
    {

    }
    public virtual void Open()
    {
        gameObject.SetActive(true);
    }
    public virtual void Close(float time)
    {
        Invoke(nameof(CloseDirectly), time);
    }
    public virtual void CloseDirectly()
    {
        if (isDestroyOnclose)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
