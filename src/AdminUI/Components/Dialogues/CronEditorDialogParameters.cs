using Microsoft.FluentUI.AspNetCore.Components;

namespace AzUrlShortener.AdminUI.Components.Dialogues
{
    public class CronEditorDialogParameters
    {
        public string CronExpression { get; set; } = string.Empty;
        public Action<string> OnSave { get; set; } = _ => { };
    }
}
