﻿using System.Numerics;
using Dalamud.Interface.Utility;
using Glamourer.Customization;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;

namespace Glamourer.Gui.Customization;

public partial class CustomizationDrawer
{
    private const string IconSelectorPopup = "Style Picker";

    private void DrawIconSelector(CustomizeIndex index)
    {
        using var _        = SetId(index);
        using var bigGroup = ImRaii.Group();
        var       label    = _currentOption;

        var current = _set.DataByValue(index, _currentByte, out var custom, _customize.Face);
        var npc     = false;
        if (current < 0)
        {
            label   = $"{_currentOption} (NPC)";
            current = 0;
            custom  = _set.Data(index, 0);
            npc     = true;
        }

        var icon = _service.AwaitedService.GetIcon(custom!.Value.IconId);
        using (var disabled = ImRaii.Disabled(_locked || _currentIndex is CustomizeIndex.Face && _lockedRedraw))
        {
            if (ImGui.ImageButton(icon.ImGuiHandle, _iconSize))
                ImGui.OpenPopup(IconSelectorPopup);
        }

        ImGuiUtil.HoverIconTooltip(icon, _iconSize);

        ImGui.SameLine();
        using (var group = ImRaii.Group())
        {
            DataInputInt(current, npc);
            if (_lockedRedraw && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                ImGui.SetTooltip(
                    "The face can not be changed as this requires a redraw of the character, which is not supported for this actor.");

            if (_withApply)
            {
                ApplyCheckbox();
                ImGui.SameLine();
            }

            ImGui.TextUnformatted(label);
        }

        DrawIconPickerPopup(current);
    }

    private void DrawIconPickerPopup(int current)
    {
        using var popup = ImRaii.Popup(IconSelectorPopup, ImGuiWindowFlags.AlwaysAutoResize);
        if (!popup)
            return;

        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)
            .Push(ImGuiStyleVar.FrameRounding, 0);
        for (var i = 0; i < _currentCount; ++i)
        {
            var custom = _set.Data(_currentIndex, i, _customize.Face);
            var icon   = _service.AwaitedService.GetIcon(custom.IconId);
            using (var _ = ImRaii.Group())
            {
                using var frameColor = ImRaii.PushColor(ImGuiCol.Button, Colors.SelectedRed, current == i);

                if (ImGui.ImageButton(icon.ImGuiHandle, _iconSize))
                {
                    UpdateValue(custom.Value);
                    ImGui.CloseCurrentPopup();
                }

                ImGuiUtil.HoverIconTooltip(icon, _iconSize);

                var text      = custom.Value.ToString();
                var textWidth = ImGui.CalcTextSize(text).X;
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (_iconSize.X - textWidth + 2 * ImGui.GetStyle().FramePadding.X) / 2);
                ImGui.TextUnformatted(text);
            }

            if (i % 8 != 7)
                ImGui.SameLine();
        }
    }


    // Only used for facial features, so fixed ID.
    private void DrawMultiIconSelector()
    {
        using var bigGroup = ImRaii.Group();
        using var disabled = ImRaii.Disabled(_locked);
        DrawMultiIcons();
        ImGui.SameLine();
        using var group = ImRaii.Group();

        _currentCount = 256;
        if (_withApply)
        {
            ApplyCheckbox(CustomizeIndex.FacialFeature1);
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + _spacing.X);
            ApplyCheckbox(CustomizeIndex.FacialFeature2);
            ImGui.SameLine();
            ApplyCheckbox(CustomizeIndex.FacialFeature3);
            ImGui.SameLine();
            ApplyCheckbox(CustomizeIndex.FacialFeature4);
        }

        PercentageInputInt();
        if (_set.DataByValue(CustomizeIndex.Face, _customize.Face, out _, _customize.Face) < 0)
        {
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            using var _ = ImRaii.Enabled();
            ImGui.TextUnformatted("(Using Face 1)");
        }

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + _spacing.Y);
        ImGui.AlignTextToFramePadding();
        using (var _ = ImRaii.Enabled())
        {
            ImGui.TextUnformatted(_set.Option(CustomizeIndex.LegacyTattoo));
        }

        if (_withApply)
        {
            ApplyCheckbox(CustomizeIndex.FacialFeature5);
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + _spacing.X);
            ApplyCheckbox(CustomizeIndex.FacialFeature6);
            ImGui.SameLine();
            ApplyCheckbox(CustomizeIndex.FacialFeature7);
            ImGui.SameLine();
            ApplyCheckbox(CustomizeIndex.LegacyTattoo);
        }
    }

    private void DrawMultiIcons()
    {
        var options = _set.Order[CharaMakeParams.MenuType.IconCheckmark];
        using var group = ImRaii.Group();
        var face = _set.DataByValue(CustomizeIndex.Face, _customize.Face, out _, _customize.Face) < 0 ? _set.Faces[0].Value : _customize.Face;
        foreach (var (featureIdx, idx) in options.WithIndex())
        {
            using var id      = SetId(featureIdx);
            var       enabled = _customize.Get(featureIdx) != CustomizeValue.Zero;
            var       feature = _set.Data(featureIdx, 0, face);
            var icon = featureIdx == CustomizeIndex.LegacyTattoo
                ? _legacyTattoo ?? _service.AwaitedService.GetIcon(feature.IconId)
                : _service.AwaitedService.GetIcon(feature.IconId);
            if (ImGui.ImageButton(icon.ImGuiHandle, _iconSize, Vector2.Zero, Vector2.One, (int)ImGui.GetStyle().FramePadding.X,
                    Vector4.Zero, enabled ? Vector4.One : _redTint))
            {
                _customize.Set(featureIdx, enabled ? CustomizeValue.Zero : CustomizeValue.Max);
                Changed |= _currentFlag;
            }

            ImGuiUtil.HoverIconTooltip(icon, _iconSize);
            if (idx % 4 != 3)
                ImGui.SameLine();
        }
    }
}
