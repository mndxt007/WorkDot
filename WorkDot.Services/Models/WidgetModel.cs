namespace WorkDot.Services.Models
{
    public class WidgetModel
    {
        public WidgetType Widget { get; set; }
        public object Data { get; set; }
    }

    public enum WidgetType
    {
        None,
        Chat,
        Plan,
        Todo,
        Teams,
        Notifications,
    }
}
