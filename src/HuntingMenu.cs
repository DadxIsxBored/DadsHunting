using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DadsHunting;

internal sealed class HuntingMenu : MonoBehaviour
{
    private InventoryGui _gui = null!;
    private Button _emblem = null!;
    private GameObject _panel = null!;
    private SkillsDialog _nativeDialog = null!;
    private RectTransform _listRoot = null!;
    private ScrollRect _scroll = null!;
    private TMP_Text _nativeRowText = null!;
    private Image? _nativeRowBackground;
    private TMP_Text? _title;
    private Sprite? _arrowSprite;
    private Texture2D? _arrowTexture;
    private float _baseListHeight;

    internal bool IsOpen => _panel != null && _panel.activeSelf;

    private void Awake()
    {
        _gui = GetComponent<InventoryGui>();
        if (_gui == null || _gui.m_pvp == null || _gui.m_skillsDialog == null) return;
        RectTransform? iconHost = _gui.m_pvp.transform.parent as RectTransform;
        if (iconHost == null) return;
        ExpandHeader();
        CreateEmblem(iconHost);
        CreateNativePanel();
    }

    private void ExpandHeader()
    {
        if (_gui.m_info == null || !_gui.m_pvp.transform.IsChildOf(_gui.m_info)) return;
        RectTransform frame = _gui.m_info;
        frame.offsetMax = new Vector2(frame.offsetMax.x + 20f, frame.offsetMax.y);
        string[] names = { "Texts", "Skills", "Trophies", "Achievements", "PVP" };
        float[] positions = { -200f, -108f, -16f, 76f, 168f };
        Transform nav = _gui.m_pvp.transform.parent;
        for (int index = 0; index < names.Length; index++)
        {
            RectTransform? icon = nav.Find(names[index]) as RectTransform;
            if (icon == null) continue;
            icon.anchoredPosition = new Vector2(positions[index], icon.anchoredPosition.y);
        }
    }

    private void CreateEmblem(RectTransform host)
    {
        RectTransform pvp = (RectTransform)_gui.m_pvp.transform;
        RectTransform rect = NewRect("DadsHunting_Emblem", host);
        rect.anchorMin = pvp.anchorMin;
        rect.anchorMax = pvp.anchorMax;
        rect.pivot = pvp.pivot;
        rect.anchoredPosition = pvp.anchoredPosition + new Vector2(92f, 0f);
        rect.sizeDelta = pvp.sizeDelta;
        rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        _emblem = rect.gameObject.AddComponent<Button>();
        _emblem.navigation = new Navigation { mode = Navigation.Mode.None };
        _emblem.onClick.AddListener(Toggle);

        _arrowTexture = CreateArrowTexture();
        _arrowSprite = Sprite.Create(_arrowTexture, new Rect(0f, 0f, 128f, 128f), new Vector2(0.5f, 0.5f));
        RectTransform imageRect = NewRect("HuntingArrow", rect);
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = new Vector2(17f, 17f);
        imageRect.offsetMax = new Vector2(-17f, -17f);
        Image image = imageRect.gameObject.AddComponent<Image>();
        image.sprite = _arrowSprite;
        image.color = new Color(0.95f, 0.69f, 0.32f, 1f);
        image.raycastTarget = true;
        _emblem.targetGraphic = image;
    }

    private void CreateNativePanel()
    {
        SkillsDialog source = _gui.m_skillsDialog;
        _panel = Instantiate(source.gameObject, source.transform.parent, false);
        _panel.name = "DadsHunting_QuarryDialog";
        _panel.SetActive(false);
        _nativeDialog = _panel.GetComponent<SkillsDialog>();
        _nativeDialog.enabled = false;
        _listRoot = _nativeDialog.m_listRoot;
        _baseListHeight = _listRoot.rect.height;
        _scroll = _panel.GetComponentInChildren<ScrollRect>(true);

        foreach (Transform child in _listRoot)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        _nativeRowText = FindRowText(source.m_elementPrefab);
        _nativeRowBackground = source.m_elementPrefab.transform.Find("bkg")?.GetComponent<Image>();
        if (_nativeDialog.m_totalSkillText != null) _nativeDialog.m_totalSkillText.gameObject.SetActive(false);

        _title = _panel.transform.Find("SkillsFrame/topic")?.GetComponent<TMP_Text>();
        if (_title == null)
        {
            float highestTitleY = float.NegativeInfinity;
            foreach (TMP_Text text in _panel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.transform.IsChildOf(_listRoot) || text == _nativeDialog.m_totalSkillText ||
                    text.GetComponentInParent<Button>() != null) continue;
                if (text.transform.position.y <= highestTitleY) continue;
                highestTitleY = text.transform.position.y;
                _title = text;
            }
        }
        if (_title != null)
        {
            _title.text = "Select your Quarry";
            _title.enableAutoSizing = true;
            _title.fontSizeMin = 15f;
        }

        foreach (Button button in _panel.GetComponentsInChildren<Button>(true))
        {
            if (button.transform.IsChildOf(_listRoot)) continue;
            if (button.GetComponent<RectTransform>().rect.width < 100f) continue;
            TMP_Text? caption = button.GetComponentInChildren<TMP_Text>(true);
            if (button.name.IndexOf("close", StringComparison.OrdinalIgnoreCase) < 0 &&
                (caption == null || caption.text.IndexOf("close", StringComparison.OrdinalIgnoreCase) < 0)) continue;
            button.onClick.AddListener(Close);
            break;
        }
    }

    private static TMP_Text FindRowText(GameObject element)
    {
        foreach (TMP_Text text in element.GetComponentsInChildren<TMP_Text>(true))
            if (text.name.Equals("name", StringComparison.OrdinalIgnoreCase)) return text;
        return element.GetComponentInChildren<TMP_Text>(true);
    }

    private void Toggle()
    {
        if (IsOpen) { Close(); return; }
        if (_panel == null) return;
        _gui.m_skillsDialog.gameObject.SetActive(false);
        _gui.m_trophiesPanel.SetActive(false);
        _gui.m_achievementsPanel.gameObject.SetActive(false);
        _gui.m_textsDialog.gameObject.SetActive(false);
        _panel.transform.SetAsLastSibling();
        _panel.SetActive(true);
        if (_title != null) _title.text = "Select your Quarry";
        Refresh();
    }

    internal void Close()
    {
        if (_panel != null) _panel.SetActive(false);
    }

    private void Refresh()
    {
        foreach (Transform child in _listRoot)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        DadsHuntingPlugin? plugin = DadsHuntingPlugin.Instance;
        if (plugin == null) return;
        var groups = new Dictionary<string, List<Quarry>>();
        var order = new List<string>();
        foreach (Quarry quarry in QuarryCatalog.All)
        {
            if (!QuarryCatalog.Unlocked(quarry)) continue;
            if (!groups.TryGetValue(quarry.Biome, out List<Quarry>? animals))
            {
                animals = new List<Quarry>();
                groups.Add(quarry.Biome, animals);
                order.Add(quarry.Biome);
            }
            animals.Add(quarry);
        }

        float top = 0f;
        if (order.Count == 0)
        {
            AddHeading("No quarry recorded yet", top);
            top += 48f;
        }
        else
        {
            AddChoice("Clear selection", top, string.IsNullOrEmpty(plugin.Selection), null);
            top += 52f;
            foreach (string biome in order)
            {
                AddHeading(biome, top);
                top += 36f;
                foreach (Quarry quarry in groups[biome])
                {
                    AddChoice(quarry.Name, top, plugin.Selection == quarry.Prefab, quarry);
                    top += 48f;
                }
                top += 8f;
            }
        }
        _listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(_baseListHeight, top));
        _scroll.verticalNormalizedPosition = 1f;
    }

    private void AddHeading(string label, float top)
    {
        RectTransform row = NewRow("Biome_" + label, top, 32f);
        TMP_Text text = NewNativeText(row, label);
        text.fontSize = 22f;
        text.color = new Color(1f, 0.72f, 0.36f, 1f);
    }

    private void AddChoice(string label, float top, bool selected, Quarry? quarry)
    {
        RectTransform row = NewRow("Quarry_" + label, top, 44f);
        Image background = row.gameObject.AddComponent<Image>();
        if (_nativeRowBackground != null)
        {
            background.sprite = _nativeRowBackground.sprite;
            background.type = _nativeRowBackground.type;
        }
        background.color = selected ? new Color(0.80f, 0.42f, 0.08f, 0.85f) :
            _nativeRowBackground != null ? _nativeRowBackground.color : new Color(0f, 0f, 0f, 0.40f);
        Button button = row.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(() =>
        {
            DadsHuntingPlugin.Instance?.Select(quarry);
            Refresh();
        });
        TMP_Text text = NewNativeText(row, label);
        text.fontSize = 22f;
        text.color = selected ? Color.black : Color.white;
        text.raycastTarget = false;
    }

    private RectTransform NewRow(string name, float top, float height)
    {
        RectTransform rect = NewRect(name, _listRoot);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -top);
        rect.sizeDelta = new Vector2(-14f, height);
        return rect;
    }

    private TMP_Text NewNativeText(RectTransform parent, string value)
    {
        TMP_Text text = Instantiate(_nativeRowText, parent, false);
        text.gameObject.SetActive(true);
        text.text = value;
        text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(18f, 0f);
        rect.offsetMax = new Vector2(-18f, 0f);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        return text;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        GameObject child = new(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)child.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static bool InsideTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float s1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
        float s2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
        float s3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
        return (s1 >= 0f && s2 >= 0f && s3 >= 0f) || (s1 <= 0f && s2 <= 0f && s3 <= 0f);
    }

    private static Texture2D CreateArrowTexture()
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.filterMode = FilterMode.Bilinear;
        Vector2 tail = new(19f, 19f), shaftEnd = new(88f, 88f);
        Vector2 shaft = shaftEnd - tail;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 point = new(x + 0.5f, y + 0.5f);
            float t = Mathf.Clamp01(Vector2.Dot(point - tail, shaft) / shaft.sqrMagnitude);
            bool mark = Vector2.Distance(point, tail + shaft * t) <= 6f ||
                InsideTriangle(point, new Vector2(69f, 105f), new Vector2(109f, 109f), new Vector2(105f, 69f)) ||
                InsideTriangle(point, new Vector2(17f, 17f), new Vector2(12f, 47f), new Vector2(39f, 25f)) ||
                InsideTriangle(point, new Vector2(17f, 17f), new Vector2(47f, 12f), new Vector2(25f, 39f));
            texture.SetPixel(x, y, mark ? Color.white : Color.clear);
        }
        texture.Apply();
        return texture;
    }

    private void Update()
    {
        if (IsOpen && !InventoryGui.IsVisible()) Close();
    }

    private void OnDestroy()
    {
        if (_arrowSprite != null) Destroy(_arrowSprite);
        if (_arrowTexture != null) Destroy(_arrowTexture);
    }
}
