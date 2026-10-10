using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DadsHunting;

internal sealed class HuntingMenu : MonoBehaviour
{
    private static readonly Color Gold = new(1f, 0.72f, 0.36f, 1f);
    private static readonly Color PanelColor = new(0.075f, 0.085f, 0.085f, 0.97f);
    private InventoryGui _gui = null!;
    private Button _button = null!;
    private GameObject _panel = null!;
    private RectTransform _content = null!;
    private RectTransform _panelRect = null!;
    private RectTransform _emblemRect = null!;
    private RectTransform _overlayRoot = null!;
    private Sprite? _targetSprite;
    private Texture2D? _targetTexture;

    private void Awake()
    {
        _gui = GetComponent<InventoryGui>();
        if (_gui == null || _gui.m_pvp == null || _gui.m_recipeName == null) return;
        RectTransform? host = _gui.m_pvp.transform.parent as RectTransform;
        RectTransform? overlayRoot = _gui.m_inventoryRoot as RectTransform;
        if (host == null || overlayRoot == null) return;
        _overlayRoot = overlayRoot;
        BuildButton(host);
        BuildPanel(_overlayRoot);
    }

    private void BuildButton(RectTransform host)
    {
        RectTransform rect = Rect("DadsHunting_Emblem", host);
        _emblemRect = rect;
        RectTransform pvp = (RectTransform)_gui.m_pvp.transform;
        rect.anchorMin = pvp.anchorMin;
        rect.anchorMax = pvp.anchorMax;
        rect.pivot = pvp.pivot;
        rect.anchoredPosition = pvp.anchoredPosition + new Vector2(pvp.rect.width - 10f, 0f);
        rect.sizeDelta = pvp.sizeDelta;
        rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        _button = rect.gameObject.AddComponent<Button>();
        _button.onClick.AddListener(Toggle);
        _targetTexture = TargetTexture();
        _targetSprite = Sprite.Create(_targetTexture, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f));
        RectTransform icon = Rect("Target", rect);
        Stretch(icon, 14f);
        Image image = icon.gameObject.AddComponent<Image>();
        image.sprite = _targetSprite;
        image.color = Gold;
        image.raycastTarget = true;
        _button.targetGraphic = image;
    }

    private void BuildPanel(RectTransform host)
    {
        RectTransform panel = Rect("DadsHunting_QuarryMenu", host);
        _panelRect = panel;
        _panel = panel.gameObject;
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(1f, 1f);
        panel.sizeDelta = new Vector2(340f, 460f);
        Image background = panel.gameObject.AddComponent<Image>();
        background.color = PanelColor;
        TextMeshProUGUI title = Label(panel, "Title", "Select your Quarry", 21f, Gold);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -12f);
        titleRect.sizeDelta = new Vector2(-42f, 34f);
        title.alignment = TextAlignmentOptions.Center;

        Button close = Button(panel, "Close", "×", 22f);
        RectTransform closeRect = (RectTransform)close.transform;
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-8f, -8f);
        closeRect.sizeDelta = new Vector2(30f, 30f);
        close.onClick.AddListener(() => _panel.SetActive(false));

        RectTransform viewport = Rect("Viewport", panel);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(12f, 12f);
        viewport.offsetMax = new Vector2(-12f, -54f);
        viewport.gameObject.AddComponent<RectMask2D>();
        _content = Rect("Quarries", viewport);
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(1f, 1f);
        _content.pivot = new Vector2(0.5f, 1f);
        _content.anchoredPosition = Vector2.zero;
        ScrollRect scroll = panel.gameObject.AddComponent<ScrollRect>();
        scroll.content = _content;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        _panel.SetActive(false);
    }

    private void Toggle()
    {
        if (_panel == null) return;
        bool open = !_panel.activeSelf;
        _panel.SetActive(open);
        if (open)
        {
            _panel.transform.SetAsLastSibling();
            PositionPanel();
            Refresh();
        }
    }

    private void PositionPanel()
    {
        if (_panelRect == null || _emblemRect == null || _overlayRoot == null) return;
        Vector3[] corners = new Vector3[4];
        _emblemRect.GetWorldCorners(corners);
        Vector3 at = _overlayRoot.InverseTransformPoint(corners[3]);
        _panelRect.localPosition = new Vector3(at.x, at.y - 45f, 0f);
    }

    private void Refresh()
    {
        for (int i = _content.childCount - 1; i >= 0; i--)
            Destroy(_content.GetChild(i).gameObject);
        DadsHuntingPlugin? plugin = DadsHuntingPlugin.Instance;
        if (plugin == null) return;
        var groups = new Dictionary<string, List<Quarry>>();
        var order = new List<string>();
        foreach (Quarry quarry in QuarryCatalog.All)
        {
            if (!QuarryCatalog.Unlocked(quarry)) continue;
            if (!groups.TryGetValue(quarry.Biome, out List<Quarry>? entries))
            {
                entries = new List<Quarry>();
                groups.Add(quarry.Biome, entries);
                order.Add(quarry.Biome);
            }
            entries.Add(quarry);
        }
        float y = 0f;
        if (order.Count == 0)
        {
            AddText("No quarry recorded yet", 0f, 34f, Gold);
            y = 34f;
        }
        else
        {
            Button none = AddOption("Clear selection", y, 32f, string.IsNullOrEmpty(plugin.Selection));
            none.onClick.AddListener(() => { plugin.Select(null); Refresh(); });
            y += 40f;
            foreach (string biome in order)
            {
                AddText(biome, y, 27f, Gold);
                y += 29f;
                foreach (Quarry quarry in groups[biome])
                {
                    Button option = AddOption(quarry.Name, y, 32f, plugin.Selection == quarry.Prefab);
                    option.onClick.AddListener(() => { plugin.Select(quarry); Refresh(); });
                    y += 36f;
                }
                y += 6f;
            }
        }
        _content.sizeDelta = new Vector2(0f, y);
    }

    private Button AddOption(string label, float top, float height, bool selected)
    {
        Button option = Button(_content, label, selected ? "◎  " + label : "○  " + label, 16f);
        RectTransform rect = (RectTransform)option.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -top);
        rect.sizeDelta = new Vector2(-3f, height);
        option.GetComponent<Image>().color = selected ? new Color(0.38f, 0.16f, 0.08f, 0.98f) : new Color(0.13f, 0.15f, 0.15f, 0.92f);
        return option;
    }

    private void AddText(string value, float top, float height, Color color)
    {
        TextMeshProUGUI text = Label(_content, value, value, 16f, color);
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -top);
        rect.sizeDelta = new Vector2(-8f, height);
    }

    private Button Button(Transform parent, string name, string caption, float size)
    {
        RectTransform rect = Rect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.13f, 0.15f, 0.15f, 0.92f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        TextMeshProUGUI text = Label(rect, "Caption", caption, size, Gold);
        Stretch(text.rectTransform, 6f);
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return button;
    }

    private TextMeshProUGUI Label(Transform parent, string name, string caption, float size, Color color)
    {
        RectTransform rect = Rect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = _gui.m_recipeName.font;
        text.fontSize = size;
        text.color = color;
        text.text = caption;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        GameObject child = new(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)child.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private static Texture2D TargetTexture()
    {
        var texture = new Texture2D(64, 64, TextureFormat.ARGB32, false);
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
        {
            float dx = x - 31.5f, dy = y - 31.5f;
            float radius = Mathf.Sqrt(dx * dx + dy * dy);
            bool mark = (radius >= 23f && radius <= 26f) || (radius >= 10f && radius <= 13f) ||
                (Mathf.Abs(dx) <= 1.5f && Mathf.Abs(dy) <= 29f) ||
                (Mathf.Abs(dy) <= 1.5f && Mathf.Abs(dx) <= 29f);
            texture.SetPixel(x, y, mark ? Color.white : Color.clear);
        }
        texture.Apply();
        return texture;
    }

    private void Update()
    {
        if (_panel == null || !_panel.activeSelf) return;
        if (!InventoryGui.IsVisible() || Input.GetKeyDown(KeyCode.Escape)) _panel.SetActive(false);
        else PositionPanel();
    }

    private void OnDestroy()
    {
        if (_targetSprite != null) Destroy(_targetSprite);
        if (_targetTexture != null) Destroy(_targetTexture);
    }
}
