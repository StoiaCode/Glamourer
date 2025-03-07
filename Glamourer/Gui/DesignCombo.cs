﻿using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Utility;
using Glamourer.Automation;
using Glamourer.Customization;
using Glamourer.Designs;
using Glamourer.Events;
using Glamourer.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Log;
using OtterGui.Widgets;

namespace Glamourer.Gui;

public abstract class DesignComboBase : FilterComboCache<Tuple<Design, string>>, IDisposable
{
    private readonly   Configuration _config;
    private readonly   DesignChanged _designChanged;
    protected readonly TabSelected   TabSelected;
    protected          float         InnerWidth;

    protected DesignComboBase(Func<IReadOnlyList<Tuple<Design, string>>> generator, Logger log, DesignChanged designChanged,
        TabSelected tabSelected, Configuration config)
        : base(generator, log)
    {
        _designChanged = designChanged;
        TabSelected    = tabSelected;
        _config        = config;
        _designChanged.Subscribe(OnDesignChange, DesignChanged.Priority.DesignCombo);
    }

    public bool Incognito
        => _config.IncognitoMode;

    void IDisposable.Dispose()
        => _designChanged.Unsubscribe(OnDesignChange);

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var ret = base.DrawSelectable(globalIdx, selected);
        var (design, path) = Items[globalIdx];
        if (path.Length > 0 && design.Name != path)
        {
            var start          = ImGui.GetItemRectMin();
            var pos            = start.X + ImGui.CalcTextSize(design.Name).X;
            var maxSize        = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
            var remainingSpace = maxSize - pos;
            var requiredSize   = ImGui.CalcTextSize(path).X + ImGui.GetStyle().ItemInnerSpacing.X;
            var offset         = remainingSpace - requiredSize;
            if (ImGui.GetScrollMaxY() == 0)
                offset -= ImGui.GetStyle().ItemInnerSpacing.X;

            if (offset < ImGui.GetStyle().ItemSpacing.X)
                ImGuiUtil.HoverTooltip(path);
            else
                ImGui.GetWindowDrawList().AddText(start with { X = pos + offset },
                    ImGui.GetColorU32(ImGuiCol.TextDisabled), path);
        }

        return ret;
    }

    protected bool Draw(Design? currentDesign, string? label, float width)
    {
        InnerWidth          = 400 * ImGuiHelpers.GlobalScale;
        CurrentSelectionIdx = Math.Max(Items.IndexOf(p => currentDesign == p.Item1), 0);
        CurrentSelection    = Items[CurrentSelectionIdx];
        var name = label ?? "Select Design Here...";
        var ret = Draw("##design", name, string.Empty, width, ImGui.GetTextLineHeightWithSpacing())
         && CurrentSelection != null;

        if (currentDesign != null)
        {
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && ImGui.GetIO().KeyCtrl)
                TabSelected.Invoke(MainWindow.TabType.Designs, currentDesign);
            ImGuiUtil.HoverTooltip("Control + Right-Click to move to design.");
        }

        return ret;
    }

    protected override string ToString(Tuple<Design, string> obj)
        => obj.Item1.Name.Text;

    protected override float GetFilterWidth()
        => InnerWidth - 2 * ImGui.GetStyle().FramePadding.X;

    protected override bool IsVisible(int globalIndex, LowerString filter)
    {
        var (design, path) = Items[globalIndex];
        return filter.IsContained(path) || design.Name.Lower.Contains(filter.Lower);
    }

    private void OnDesignChange(DesignChanged.Type type, Design design, object? data = null)
    {
        switch (type)
        {
            case DesignChanged.Type.Created:
            case DesignChanged.Type.Renamed:
                Cleanup();
                break;
            case DesignChanged.Type.Deleted:
                Cleanup();
                if (CurrentSelection?.Item1 == design)
                {
                    CurrentSelectionIdx = -1;
                    CurrentSelection    = null;
                }

                break;
        }
    }
}

public sealed class DesignCombo : DesignComboBase
{
    public DesignCombo(DesignManager designs, DesignFileSystem fileSystem, Logger log, DesignChanged designChanged, TabSelected tabSelected,
        Configuration config)
        : base(
            () => designs.Designs
                .Select(d => new Tuple<Design, string>(d, fileSystem.FindLeaf(d, out var l) ? l.FullName() : string.Empty))
                .OrderBy(d => d.Item2)
                .ToList(), log, designChanged, tabSelected, config)
    { }

    public Design? Design
        => CurrentSelection?.Item1;

    public void Draw(float width)
        => Draw(Design, (Incognito ? Design?.Incognito : Design?.Name.Text) ?? string.Empty, width);
}

public sealed class RevertDesignCombo : DesignComboBase, IDisposable
{
    public const     int               RevertDesignIndex = -1228;
    public readonly  Design            RevertDesign;
    private readonly AutoDesignManager _autoDesignManager;

    public RevertDesignCombo(DesignManager designs, DesignFileSystem fileSystem, TabSelected tabSelected,
        ItemManager items, CustomizationService customize, Logger log, DesignChanged designChanged, AutoDesignManager autoDesignManager,
        Configuration config)
        : this(designs, fileSystem, tabSelected, CreateRevertDesign(customize, items), log, designChanged, autoDesignManager, config)
    { }

    private RevertDesignCombo(DesignManager designs, DesignFileSystem fileSystem, TabSelected tabSelected,
        Design revertDesign, Logger log, DesignChanged designChanged, AutoDesignManager autoDesignManager, Configuration config)
        : base(() => designs.Designs
            .Select(d => new Tuple<Design, string>(d, fileSystem.FindLeaf(d, out var l) ? l.FullName() : string.Empty))
            .OrderBy(d => d.Item2)
            .Prepend(new Tuple<Design, string>(revertDesign, string.Empty))
            .ToList(), log, designChanged, tabSelected, config)
    {
        RevertDesign       = revertDesign;
        _autoDesignManager = autoDesignManager;
    }


    public void Draw(AutoDesignSet set, AutoDesign? design, int autoDesignIndex)
    {
        if (!Draw(design?.Design, design?.Name(Incognito), ImGui.GetContentRegionAvail().X))
            return;

        if (autoDesignIndex >= 0)
            _autoDesignManager.ChangeDesign(set, autoDesignIndex, CurrentSelection!.Item1 == RevertDesign ? null : CurrentSelection!.Item1);
        else
            _autoDesignManager.AddDesign(set, CurrentSelection!.Item1 == RevertDesign ? null : CurrentSelection!.Item1);
    }

    private static Design CreateRevertDesign(CustomizationService customize, ItemManager items)
        => new(customize, items)
        {
            Index          = RevertDesignIndex,
            Name           = AutoDesign.RevertName,
            ApplyCustomize = CustomizeFlagExtensions.AllRelevant,
        };
}
