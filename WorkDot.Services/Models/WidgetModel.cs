namespace WorkDot.Services.Models
{
    public class WidgetModel
    {
        public WidgetType Widget { get; set; }
        public object Payload { get; set; }
    }

    public enum WidgetType
    {
        Plan,
        ToDo,
        Teams,
        Notifications,
    }
}
