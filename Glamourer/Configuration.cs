﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Internal.Notifications;
using Glamourer.Designs;
using Glamourer.Gui;
using Glamourer.Services;
using Newtonsoft.Json;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Filesystem;
using OtterGui.Widgets;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace Glamourer;

public class Configuration : IPluginConfiguration, ISavable
{
    public bool Enabled                          { get; set; } = true;
    public bool UseRestrictedGearProtection      { get; set; } = false;
    public bool OpenFoldersByDefault             { get; set; } = false;
    public bool AutoRedrawEquipOnChanges         { get; set; } = false;
    public bool EnableAutoDesigns                { get; set; } = true;
    public bool IncognitoMode                    { get; set; } = false;
    public bool UnlockDetailMode                 { get; set; } = true;
    public bool HideApplyCheckmarks              { get; set; } = false;
    public bool SmallEquip                       { get; set; } = false;
    public bool UnlockedItemMode                 { get; set; } = false;
    public byte DisableFestivals                 { get; set; } = 1;
    public bool EnableGameContextMenu            { get; set; } = true;
    public bool HideWindowInCutscene             { get; set; } = false;
    public bool ShowAutomationSetEditing         { get; set; } = true;
    public bool ShowAllAutomatedApplicationRules { get; set; } = true;
    public bool ShowUnlockedItemWarnings         { get; set; } = true;
    public bool RevertManualChangesOnZoneChange  { get; set; } = false;
    public bool ShowDesignQuickBar               { get; set; } = false;
    public bool LockDesignQuickBar               { get; set; } = false;
    public bool ShowQuickBarInTabs               { get; set; } = true;
    public bool LockMainWindow                   { get; set; } = false;

    public ModifiableHotkey   ToggleQuickDesignBar { get; set; } = new(VirtualKey.NO_KEY);
    public MainWindow.TabType SelectedTab          { get; set; } = MainWindow.TabType.Settings;
    public DoubleModifier     DeleteDesignModifier { get; set; } = new(ModifierHotkey.Control, ModifierHotkey.Shift);

    public int                  LastSeenVersion      { get; set; } = GlamourerChangelog.LastChangelogVersion;
    public ChangeLogDisplayType ChangeLogDisplayType { get; set; } = ChangeLogDisplayType.New;

    [JsonConverter(typeof(SortModeConverter))]
    [JsonProperty(Order = int.MaxValue)]
    public ISortMode<Design> SortMode { get; set; } = ISortMode<Design>.FoldersFirst;

    public List<(string Code, bool Enabled)> Codes { get; set; } = new();

#if DEBUG
    public bool DebugMode { get; set; } = true;
#else
    public bool DebugMode { get; set; } = false;
#endif

    public int Version { get; set; } = Constants.CurrentVersion;

    public Dictionary<ColorId, uint> Colors { get; private set; }
        = Enum.GetValues<ColorId>().ToDictionary(c => c, c => c.Data().DefaultColor);

    [JsonIgnore]
    private readonly SaveService _saveService;

    public Configuration(SaveService saveService, ConfigMigrationService migrator)
    {
        _saveService = saveService;
        Load(migrator);
    }

    public void Save()
        => _saveService.DelaySave(this);

    public void Load(ConfigMigrationService migrator)
    {
        static void HandleDeserializationError(object? sender, ErrorEventArgs errorArgs)
        {
            Glamourer.Log.Error(
                $"Error parsing Configuration at {errorArgs.ErrorContext.Path}, using default or migrating:\n{errorArgs.ErrorContext.Error}");
            errorArgs.ErrorContext.Handled = true;
        }

        if (!File.Exists(_saveService.FileNames.ConfigFile))
            return;

        if (File.Exists(_saveService.FileNames.ConfigFile))
            try
            {
                var text = File.ReadAllText(_saveService.FileNames.ConfigFile);
                JsonConvert.PopulateObject(text, this, new JsonSerializerSettings
                {
                    Error = HandleDeserializationError,
                });
            }
            catch (Exception ex)
            {
                Glamourer.Messager.NotificationMessage(ex,
                    "Error reading Configuration, reverting to default.\nYou may be able to restore your configuration using the rolling backups in the XIVLauncher/backups/Glamourer directory.",
                    "Error reading Configuration", NotificationType.Error);
            }

        migrator.Migrate(this);
    }

    public string ToFilename(FilenameService fileNames)
        => fileNames.ConfigFile;

    public void Save(StreamWriter writer)
    {
        using var jWriter    = new JsonTextWriter(writer) { Formatting = Formatting.Indented };
        var       serializer = new JsonSerializer { Formatting         = Formatting.Indented };
        serializer.Serialize(jWriter, this);
    }

    public static class Constants
    {
        public const int CurrentVersion = 4;

        public static readonly ISortMode<Design>[] ValidSortModes =
        {
            ISortMode<Design>.FoldersFirst,
            ISortMode<Design>.Lexicographical,
            new DesignFileSystem.CreationDate(),
            new DesignFileSystem.InverseCreationDate(),
            new DesignFileSystem.UpdateDate(),
            new DesignFileSystem.InverseUpdateDate(),
            ISortMode<Design>.InverseFoldersFirst,
            ISortMode<Design>.InverseLexicographical,
            ISortMode<Design>.FoldersLast,
            ISortMode<Design>.InverseFoldersLast,
            ISortMode<Design>.InternalOrder,
            ISortMode<Design>.InverseInternalOrder,
        };
    }

    /// <summary> Convert SortMode Types to their name. </summary>
    private class SortModeConverter : JsonConverter<ISortMode<Design>>
    {
        public override void WriteJson(JsonWriter writer, ISortMode<Design>? value, JsonSerializer serializer)
        {
            value ??= ISortMode<Design>.FoldersFirst;
            serializer.Serialize(writer, value.GetType().Name);
        }

        public override ISortMode<Design> ReadJson(JsonReader reader, Type objectType, ISortMode<Design>? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            var name = serializer.Deserialize<string>(reader);
            if (name == null || !Constants.ValidSortModes.FindFirst(s => s.GetType().Name == name, out var mode))
                return existingValue ?? ISortMode<Design>.FoldersFirst;

            return mode;
        }
    }
}
