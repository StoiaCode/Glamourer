﻿using System.Collections.Generic;
using Dalamud.Data;
using Dalamud.Plugin;
using Penumbra.GameData.Enums;

namespace Glamourer.Customization;

public class CustomizationManager : ICustomizationManager
{
    private static CustomizationOptions? _options;

    private CustomizationManager()
    { }

    public static ICustomizationManager Create(DalamudPluginInterface pi, DataManager gameData)
    {
        _options ??= new CustomizationOptions(pi, gameData);
        return new CustomizationManager();
    }

    public IReadOnlyList<Race> Races
        => CustomizationOptions.Races;

    public IReadOnlyList<SubRace> Clans
        => CustomizationOptions.Clans;

    public IReadOnlyList<Gender> Genders
        => CustomizationOptions.Genders;

    public CustomizationSet GetList(SubRace clan, Gender gender)
        => _options!.GetList(clan, gender);

    public ImGuiScene.TextureWrap GetIcon(uint iconId)
        => _options!.GetIcon(iconId);

    public void RemoveIcon(uint iconId)
        => _options!.RemoveIcon(iconId);

    public string GetName(CustomName name)
        => _options!.GetName(name);
}
