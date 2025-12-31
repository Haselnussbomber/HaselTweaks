using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Windows;

[RegisterSingleton, AutoConstruct]
public unsafe partial class MJICraftScheduleSettingSearchBar : SimpleWindow
{
    private const int LanguageSelectorWidth = 90;

    private readonly PluginConfig _pluginConfig;
    private readonly ExcelService _excelService;
    private readonly TextService _textService;
    private readonly LanguageProvider _languageProvider;
    private readonly EnhancedIsleworksAgendaConfiguration _config;

    private string _query = string.Empty;
    private bool _inputFocused;

    [AutoPostConstruct]
    private void Initialize()
    {
        Flags |= ImGuiWindowFlags.NoSavedSettings;
        Flags |= ImGuiWindowFlags.NoDecoration;
        Flags |= ImGuiWindowFlags.NoMove;

        DisableWindowSounds = true;
        RespectCloseHotkey = false;
    }

    public override bool DrawConditions()
        => IsAddonOpen("MJICraftScheduleSetting"u8);

    public override void OnOpen()
    {
        _inputFocused = false;
        _query = string.Empty;
    }

    public override void PreDraw()
    {
        base.PreDraw();

        if (!TryGetAddon<AtkUnitBase>("MJICraftScheduleSetting"u8, out var addon))
            return;

        var scale = ImGuiHelpers.GlobalScale;
        var inverseScale = 1 / scale;
        var addonWidth = addon->GetScaledWidth(true);
        var width = (addonWidth - 8) * inverseScale;
        var height = (ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2 + ImGui.GetStyle().WindowPadding.Y * 2) * inverseScale;
        var offset = new Vector2(4, 3 - height * scale);

        Position = ImGui.GetMainViewport().Pos + addon->Position + offset;
        Size = new(width, height);
    }

    public override void Draw()
    {
        if (!_inputFocused)
        {
            ImGui.SetKeyboardFocusHere(0);
            _inputFocused = true;
        }

        if (!TryGetAddon<AddonMJICraftScheduleSetting>("MJICraftScheduleSetting"u8, out var addon))
            return;

        var lastQuery = _query;
        var contentRegionAvail = ImGui.GetContentRegionAvail();

        ImGui.SetNextItemWidth(contentRegionAvail.X - LanguageSelectorWidth * ImGuiHelpers.GlobalScale - ImGui.GetStyle().ItemSpacing.X);
        if (ImGui.InputTextWithHint("##Query", _textService.Translate("EnhancedIsleworksAgenda.MJICraftScheduleSettingSearchBar.QueryHint"), ref _query, 255, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            var evt = new AtkEvent();
            addon->ReceiveEvent(AtkEventType.ButtonClick, 6, &evt);
        }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(LanguageSelectorWidth * ImGuiHelpers.GlobalScale);
        using (var dropdown = ImRaii.Combo("##Language", Enum.GetName(_config.SearchLanguage) ?? "Language..."))
        {
            if (dropdown)
            {
                var values = Enum.GetValues<ClientLanguage>().OrderBy((ClientLanguage lang) => lang.ToString());
                foreach (var value in values)
                {
                    if (ImGui.Selectable(Enum.GetName(value), value == _config.SearchLanguage))
                    {
                        _config.SearchLanguage = value;
                        _pluginConfig.Save();
                    }
                }
            }
        }

        if (lastQuery != _query)
        {
            var entries = new List<(int Index, string ItemName)>();
            for (var i = 0; i < addon->TreeList->Items.LongCount; i++)
            {
                var item = addon->TreeList->Items[i].Value;
                if (item != null && item->UIntValues.LongCount >= 3 && item->UIntValues[0] != (uint)AtkComponentTreeListItemType.CollapsibleGroupHeader)
                {
                    var rowId = item->UIntValues[2];

                    if (!_excelService.TryGetRow<MJICraftworksObject>(rowId, out var mjiCraftworksObject))
                        continue;

                    if (!_excelService.TryGetRow<Item>(mjiCraftworksObject.Item.RowId, _config.SearchLanguage, out var itemRow))
                        continue;

                    var itemName = itemRow.Name.ToString();
                    if (string.IsNullOrEmpty(itemName))
                        continue;

                    entries.Add((i, itemName.ToLower()));
                }
            }

            if (entries.FuzzyMatch(_query.ToLower().Trim(), value => value.ItemName).TryGetFirst(out var result))
            {
                var index = result.Value.Index;
                var item = addon->TreeList->GetItem(index);
                if (item != null)
                {
                    // find parent group and expand it
                    for (var i = index; i >= 0; i--)
                    {
                        var headerItem = addon->TreeList->Items[i].Value;
                        if (headerItem != null && headerItem->UIntValues.LongCount >= 1 && headerItem->UIntValues[0] == (uint)AtkComponentTreeListItemType.CollapsibleGroupHeader)
                        {
                            addon->TreeList->ExpandGroupExclusively(headerItem, false);
                            addon->TreeList->LayoutRefreshPending = true;
                            break;
                        }
                    }

                    addon->TreeList->SelectItem(index, true); // if it would only scroll the selected item into view... oh well
                }
            }
        }
    }
}
