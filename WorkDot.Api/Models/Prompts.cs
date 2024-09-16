namespace WorkDot.Api.Models
{
    public static class Prompts
    {
        public const  string EmailParamDescription = """
            A Microsoft Graph API query string for retrieving emails based on the user input, following the format $top=[number]&$orderby=receivedDateTime desc&$filter=[filter conditions], with rules for $top, $orderby, and $filter as specified. 
            """;
        public const  string ToDoParamDescription = """
            A Microsoft Graph API query string for retrieving To-Do tasks based on the user input, following the format $top=[number]&$orderby=dueDateTime/dateTime desc&$filter=[filter conditions], with rules for $top, $orderby, and $filter as specified. 
            """;
        public const string ToDoListParamDescription = """
            A Microsoft Graph API To-Do list name for retrieving To-Do tasks based on the user input.
            Default is "Tasks"
            """;
    }
}
