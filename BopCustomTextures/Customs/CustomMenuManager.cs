using BopCustomTextures.Config;
using BopCustomTextures.Scripts;
using BopCustomTextures.EventTemplates;
using BopCustomTextures.AccessExtensions;
using TMPro;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Display = BopCustomTextures.Config.Display;
using ILogger = BopCustomTextures.Logging.ILogger;

namespace BopCustomTextures.Customs;

public class CustomMenuManager : BaseCustomManager
{
    public static readonly string[] MenuCopyOptions = [
        "Copy Customs from File",
        "Copy Customs from Folder",
    ];
    public static readonly string[] MenuReloadOptions = [
        "Reload Custom Assets"
    ];

    private bool LastCopyActive = false;
    private bool LastReloadActive = false;
    private int lastTemplatesIndex = -1;

    public ConfigManager ConfigManager;
    public CustomManager Manager;

    public MixtapeEventTemplate EditorPropertiesTemplate;
    private MixtapeEventScript _editorPropertiesEvent;
    public MixtapeEventScript EditorPropertiesEvent 
    { 
        get => _editorPropertiesEvent; 
        set => Manager.EditorPropertiesEvent = _editorPropertiesEvent = value;
    }

    public MixtapeEventTemplate MixtapePropertiesTemplate;
    private MixtapeEventScript _mixtapePropertiesEvent;
    public MixtapeEventScript MixtapePropertiesEvent 
    { 
        get => _mixtapePropertiesEvent; 
        set => Manager.MixtapePropertiesEvent = _mixtapePropertiesEvent = value;
    }

    public Dictionary<string, List<MixtapeEventTemplate>> Entities;

    public int ModdedCategoryIndex = -1;
    private string LastCategorySelected;
    public BopCustomTexturesButton MixtapeCategoryButton = null;

    public CustomMenuManager(ILogger logger, ConfigManager configManager, CustomManager manager,
        MixtapeEventTemplate editorPropertiesTemplate,
        MixtapeEventTemplate mixtapePropertiesTemplate,
        Dictionary<string, List<MixtapeEventTemplate>> entities) : base(logger)
    {
        ConfigManager = configManager;
        Manager = manager;
        EditorPropertiesTemplate = editorPropertiesTemplate;
        MixtapePropertiesTemplate = mixtapePropertiesTemplate;
        Entities = entities;
        UpdateEventCategoryPosition();
    }

    public void UpdateEventCategoryPosition()
    {
        var index = FindEventCategoryIndex();
        if (index == lastTemplatesIndex && Entities.ContainsKey(MyPluginInfo.PLUGIN_GUID))
        {
            return;
        }
        Entities.Remove(MyPluginInfo.PLUGIN_GUID);
        var list = Entities.ToList();
        if (index > list.Count || index < 1)
        {
            index = list.Count;
        }
        list.Insert(index, new KeyValuePair<string, List<MixtapeEventTemplate>>(MyPluginInfo.PLUGIN_GUID, new List<MixtapeEventTemplate>(BopCustomTexturesEventTemplates.Templates)));
        Entities.Clear();
        foreach (var pair in list)
        {
            Entities[pair.Key] = pair.Value;
        }
        lastTemplatesIndex = index;
    }

    public int FindEventCategoryIndex()
    {
        var x = FindEventCategoryIndexInternal();
        if (Entities.ContainsKey(MyPluginInfo.PLUGIN_GUID) && x > Entities.Keys.ToList().IndexOf(MyPluginInfo.PLUGIN_GUID))
        {
            x--;
        }
        return x;
    }

    private int FindEventCategoryIndexInternal()
    {
        foreach (string category in ConfigManager.GetEventTemplatesBefore())
        {
            if (Entities.ContainsKey(category))
            {
                return Entities.Keys.ToList().IndexOf(category);
            }
        }
        foreach (string category in ConfigManager.GetEventTemplatesAfter())
        {
            if (Entities.ContainsKey(category))
            {
                return Entities.Keys.ToList().IndexOf(category) + 1;
            }
        }
        return ConfigManager.EventTemplatesIndex.Value;
    }

    public void HandleMenuOption(MixtapeEditorScript __instance, int index)
    {
        HandleMenuOption(__instance, index,
            ConfigManager.DisplayCopyOptions.Value,
            ConfigManager.DisplayReloadOptions.Value);
    }

    public void HandleMenuOption(MixtapeEditorScript __instance, int index,
        Display showCopyOptions,
        Display showReloadOptions)
    {
        if (!DisplayActive(showCopyOptions, Manager.HasCustomAssets))
        {
            index += 2;
        }
        switch (index)
        {
            case 0:
                Manager.FileOpenCustomsArchive(__instance);
                break;
            case 1:
                Manager.FileOpenCustomsDirectory(__instance);
                break;
            case 2:
                if (DisplayActive(showReloadOptions, Manager.HasCustomAssets))
                {
                    Manager.ResetAndReload(true);
                }
                break;
        }
    }

    public void UpdateEditorPropertiesEvent(MixtapeEditorScript __instance)
    {
        bool copyActive = DisplayActive(ConfigManager.DisplayCopyOptions.Value, Manager.HasCustomAssets);
        bool reloadActive = DisplayActive(ConfigManager.DisplayReloadOptions.Value, Manager.HasCustomAssets);

        if (_editorPropertiesEvent != null)
        {
            if (LastCopyActive != copyActive || LastReloadActive != reloadActive)
            {
                Object.Destroy(_editorPropertiesEvent.gameObject);
                EditorPropertiesEvent = null;
            }
        }

        if (_editorPropertiesEvent == null)
        {
            LastCopyActive = copyActive;
            LastReloadActive = reloadActive;

            EditorPropertiesTemplate.properties = new(BopCustomTexturesEventTemplates.EditorPropertiesTemplatePropertiesBase);
            if (copyActive)
            {
                foreach (string str in BopCustomTexturesEventTemplates.PropertyCopyOptions)
                {
                    EditorPropertiesTemplate.properties[str] = new MixtapeEventTemplates.ButtonField();
                }
            }
            if (reloadActive)
            {
                foreach (string str in BopCustomTexturesEventTemplates.PropertyReloadOptions)
                {
                    EditorPropertiesTemplate.properties[str] = new MixtapeEventTemplates.ButtonField();
                }
            }
            Entity entity = Entity.FromTemplate(EditorPropertiesTemplate);
            entity.beat = -100f;
            entity.SetString("last path", Manager.LastPath);
            EditorPropertiesEvent = MixtapeEventScript.Spawn(__instance.mixtapeEventPrefab, entity);
            var singletonEvents = __instance.GetSingletonEvents();
            if (singletonEvents != null)
            {
                singletonEvents[entity.dataModel] = _editorPropertiesEvent;
            }
        }
    }

    public void UpdateMixtapePropertiesEvent(MixtapeEditorScript __instance)
    {
        if (_mixtapePropertiesEvent == null)
        {
            Entity entity = Entity.FromTemplate(MixtapePropertiesTemplate);
            entity.beat = -100f;
            entity.SetString("version", Manager.Version);
            entity.SetInt("release", (int)Manager.Release);
            entity.SetBool("unsafe", Manager.Unsafe);
            MixtapePropertiesEvent = MixtapeEventScript.Spawn(__instance.mixtapeEventPrefab, entity);
            var singletonEvents = __instance.GetSingletonEvents();
            if (singletonEvents != null)
            {
                singletonEvents[entity.dataModel] = _mixtapePropertiesEvent;
            }
        }
    }

    public void UpdateSingletonEvents(MixtapeEditorScript __instance)
    {
        UpdateEventCategoryPosition();
        UpdateEditorPropertiesEvent(__instance);
        UpdateMixtapePropertiesEvent(__instance);
        UpdateMixtapeCategoryButton(__instance);
    }

    public bool CycleModdedCategory(MixtapeEditorScript __instance, ref string category)
    {
        return CycleModdedCategory(__instance, ref category, ConfigManager.GetHijackEventCategory());
    }
    public bool CycleModdedCategory(MixtapeEditorScript __instance, ref string category, IEnumerable<string> hijackedCategories)
    {
        var oldCategory = LastCategorySelected;
        LastCategorySelected = category;

        if (!hijackedCategories.Contains(category))
        {
            return false;
        }
        var moddedCategories = DefaultEventCategories.ModdedCategories;

        if (category == oldCategory)
        {
            ModdedCategoryIndex++;
            var index = moddedCategories.IndexOf(category);
            if (index >= 0 && ModdedCategoryIndex >= moddedCategories.IndexOf(category))
            {
                ModdedCategoryIndex++;
            }
            ModdedCategoryIndex = (ModdedCategoryIndex + 1) % (moddedCategories.Count + 1) - 1;
        }

        category = (ModdedCategoryIndex < 0 || ModdedCategoryIndex >= moddedCategories.Count) ? category : moddedCategories[ModdedCategoryIndex];
        return true;
    }

    public void CycleProperty(MixtapeEditorScript __instance, int option)
    {
        if (__instance.GetSelectedEventsCount() == 1 &&
            __instance.GetSelectedEventsIndex(0).Datamodel == EditorPropertiesTemplate.dataModel &&
            option >= BopCustomTexturesEventTemplates.EditorPropertiesTemplatePropertiesBase.Count)
        {
            HandleMenuOption(__instance, option - BopCustomTexturesEventTemplates.EditorPropertiesTemplatePropertiesBase.Count);
        }
    }

    // For versions without MixtapeEditorScript.singletonEvents
    public bool SelectedEventIsSingleton(MixtapeEditorScript __instance)
    {
        return __instance.GetSelectedEventsCount() == 1 &&
            (__instance.GetSelectedEventsIndex(0) == _editorPropertiesEvent ||
            __instance.GetSelectedEventsIndex(0) == _mixtapePropertiesEvent);
    }

    // For versions without MixtapeEditorScript.singletonEvents
    public bool CheckSingletonEventSelected(MixtapeEditorScript __instance)
    {
        if (__instance.GetLevelIndex() == Entities.Keys.ToList().IndexOf(MyPluginInfo.PLUGIN_GUID))
        {
            if (__instance.GetEventIndex() == 0)
            {
                __instance.SetSelectedEvent(_editorPropertiesEvent);
                return true;
            }
            else if (__instance.GetEventIndex() == 1)
            {
                __instance.SetSelectedEvent(_mixtapePropertiesEvent);
                return true;
            }
        }
        return false;
    }

    // For versions without MixtapeEditorScript.singletonEvents
    public bool CheckSingletonEventSpawning(MixtapeEditorScript __instance, MixtapeEventTemplate templateEvent)
    {
        if (templateEvent == EditorPropertiesTemplate)
        {
            __instance.SetSelectedEvent(_editorPropertiesEvent);
            return true;
        }
        else if (templateEvent == MixtapePropertiesTemplate)
        {
            __instance.SetSelectedEvent(_mixtapePropertiesEvent);
            return true;
        }
        return false;
    }

    // For versions without MixtapeEditorScript.singletonEvents
    public void FormatIfSingletonSelected(MixtapeEditorScript __instance)
    {
        if (CheckSingletonEventSelected(__instance))
        {
            __instance.ZSortEvents();
            __instance.FormatLevels();
            __instance.FormatEvents();
            __instance.FormatProperties();
            __instance.FormatValues();
        }
    }

    public void HandleKeybind(MixtapeEditorScript __instance)
    {
        if (Input.GetKeyDown(ConfigManager.CopyCustomsFromFileKeybind.Value))
        {
            Logger.LogInfo("Keybind pressed: Copy Customs from File");
            Manager.FileOpenCustomsArchive(__instance);
        }
        else if (Input.GetKeyDown(ConfigManager.CopyCustomsFromFolderKeybind.Value))
        {
            Logger.LogInfo("Keybind pressed: Copy Customs from Folder");
            Manager.FileOpenCustomsDirectory(__instance);
        }
        else if (Input.GetKeyDown(ConfigManager.ReloadCustomAssetsKeybind.Value))
        {
            Logger.LogInfo("Keybind pressed: Reload Custom Assets");
            Manager.ResetAndReload(true);
        }
        else if (Input.GetKeyDown(ConfigManager.SelectEventCatagoryKeybind.Value))
        {
            Logger.LogInfo("Keybind pressed: Select Event Catagory");
            __instance.OnSelectCategory_safe(MyPluginInfo.PLUGIN_GUID);
        }
    }

    public void HandleOldMenu(MixtapeEditorScript __instance)
    {
        if (!MixtapeEditorScriptExtensions.MenuField.Exists())
        {
            return;
        }
        SpriteRenderer menu = __instance.GetMenu();
        Vector3 mousePosition = Input.mousePosition;
        Vector3 vector = __instance.mainCamera.ScreenToWorldPoint(mousePosition);
        if (Input.GetKeyDown(KeyCode.Mouse0) && MixtapeEditorScript.HitTest(menu, vector))
        {
            int panel = (int)(Mathf.InverseLerp(-7.5f, 7.5f, vector.x) * 5f);
            int option = (int)(Mathf.InverseLerp(menu.bounds.center.y + menu.bounds.extents.y, menu.bounds.center.y - menu.bounds.extents.y, vector.y) * 16f);
            if (panel == 0 && option >= 9)
            {
                Logger.LogInfo($"Clicked modded option: {option - 9}");
                HandleMenuOption(__instance, option - 9);
            }
        }
    }

    public void FormatOldMenu(MixtapeEditorScript __instance)
    {
        FormatOldMenu(__instance, ConfigManager.DisplayCopyOptions.Value, ConfigManager.DisplayReloadOptions.Value);
    }
    public void FormatOldMenu(MixtapeEditorScript __instance, Display showCopyOptions, Display showReloadOptions)
    {
        if (!MixtapeEditorScriptExtensions.MenuTextField.Exists())
        {
            return;
        }

        TMP_Text menuText = __instance.GetMenuText();
        string text = menuText.text;
        bool changed = false;
        if (DisplayActive(showCopyOptions, Manager.HasCustomAssets))
        {
            text += "\n" + string.Join("\n", MenuCopyOptions);
            changed = true;
        }
        if (DisplayActive(showReloadOptions, Manager.HasCustomAssets))
        {
            text += "\n" + string.Join("\n", MenuReloadOptions);
            changed = true;
        }
        if (changed)
        {
            menuText.text = text;
            menuText.ForceMeshUpdate();
        }
    }

    public bool UpdateMixtapeCategoryButton(MixtapeEditorScript __instance)
    {
        var showButton = DisplayActive(ConfigManager.DisplayEventTemplates.Value, Manager.HasCustomAssets);
        if (MixtapeCategoryButton != null)
        {
            MixtapeCategoryButton.UpdateDisplay(showButton, FindEventCategoryIndex());
            return false;
        }
        if (!showButton)
        {
            return false;
        }
        MixtapeCategoryButton = BopCustomTexturesButton.Create(__instance);
        if (MixtapeCategoryButton == null)
        {
            return false;
        }
        MixtapeCategoryButton.UpdateDisplay(showButton, FindEventCategoryIndex());
        return true;
    }

    public static bool DisplayActive(Display display, bool active)
    {
        return display == Display.Always || display == Display.WhenActive && active;
    }
}
