namespace WorkDot.Api.Models
{
    public static class Prompts
    {
        public const  string EmailParamDescription = """
            A Microsoft Graph API query string for retrieving emails based on the user input, following the format $top=[number]&$orderby=receivedDateTime desc&$filter=[filter conditions], with rules for $top, $orderby, and $filter as specified.
            top should not exceed 5, orderby is mandatory, and filter is optional.
            """;
        public const  string ToDoParamDescription = """
            A Microsoft Graph API query string for retrieving To-Do tasks based on the user input, following the format $top=[number]&$orderby=createdDateTime desc&$filter=[filter conditions], with rules for $top, $orderby, and $filter as specified. 
            Example - $top=5&$orderby=dueDateTime/dateTime desc&$filter=dueDateTime/dateTime ge 'YYYY-MM-DDTHH:MM:SSZ' and dueDateTime/dateTime le 'YYYY-MM-DDTHH:MM:SSZ'&$orderby=createdDateTime desc           
            """;
        public const string ToDoListParamDescription = """
            A Microsoft Graph API To-Do list name for retrieving To-Do tasks based on the user input.
            Default is "Tasks"
            """;
    }
}
