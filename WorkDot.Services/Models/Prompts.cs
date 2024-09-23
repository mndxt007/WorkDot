namespace WorkDot.Services.Models
{
    public static class Prompts
    {
        public const string EmailParamDescription = """
            A Microsoft Graph API query string for retrieving emails based on the user input, following the format $top=[number]&$orderby=receivedDateTime desc&$filter=[filter conditions], with rules for $top, $orderby, and $filter as specified.
            top should not exceed 5, orderby is mandatory, and filter is optional.
            """;
        public const string ToDoParamDescription = """
            A Microsoft Graph API query string for retrieving To-Do tasks based on the user input, following the format $top=[number]&$orderby=createdDateTime desc&$filter=[filter conditions], with rules for $top, $orderby, and $filter as specified. 
            Example - $top=5&$orderby=dueDateTime/dateTime desc&$filter=dueDateTime/dateTime ge 'YYYY-MM-DDTHH:MM:SSZ' and dueDateTime/dateTime le 'YYYY-MM-DDTHH:MM:SSZ'&$orderby=createdDateTime desc           
            """;
        public const string ToDoListParamDescription = """
            A Microsoft Graph API To-Do list name for retrieving To-Do tasks based on the user input.
            Default is "Tasks"
            """;

        public const string ComposePrompt = """
            Analyze the provided email content and return a structured JSON response with the following four attributes:
            
            1. Action (String): Determine the appropriate action based on the email content. Possible actions include:
                'Follow-up'
                'Attention-Needed'
                'Acknowledgement'
                'Case-Closure' If the email is from the 'Sent Items' folder, default to 'Follow-up'.
            
            2. Response (String): A generated response to the email that is professional, contextually appropriate and aligns with the identified action. Exclude any signature.
            
            3. Sentiment (String): Evaluate the sender's tone and classify the sentiment as one of the following:
                'Positive'
                'Neutral'
                'Negative'
            
            4. Priority (Integer): Assign a priority level on a scale of 1 to 10 (1 being the lowest, 10 the highest). The priority should reflect the urgency of the action, factoring in both the sentiment and the email's age.
            
            Instructions:
            Empty Response Handling: If the email content is missing or irrelevant, return an empty JSON object.

            Output Format:

            Return a JSON object in this structure:

            {{ "Action": "Action type based on email content", "Response": "Drafted email response", "Sentiment": "Identified sentiment", "Priority": priority_number }}
            
            Following is the Email Content:
            _________________________________________

            {0}
            """;

        public const string CustomComposePrompt = """
            Analyze the provided email content and return a structured JSON response with the following four attributes:
            
            1. Action (String): Determine the appropriate action based on the email content. Possible actions include:
                'Follow-up'
                'Attention-Needed'
                'Acknowledgement'
                'Case-Closure' If the email is from the 'Sent Items' folder, default to 'Follow-up'.
            
            2. Response (String): A generated response to the email that is professional, contextually appropriate and aligns with the identified action. Exclude any signature. Additionally, strictly adhere to the following special instructions: {1}
            
            3. Sentiment (String): Evaluate the sender's tone and classify the sentiment as one of the following:
                'Positive'
                'Neutral'
                'Negative'
            
            4. Priority (Integer): Assign a priority level on a scale of 1 to 10 (1 being the lowest, 10 the highest). The priority should reflect the urgency of the action, factoring in both the sentiment and the email's age.
            
            Instructions:
            Empty Response Handling: If the email content is missing or irrelevant, return an empty JSON object.
            
            Output Format:
            
            Return a JSON object in this structure:
            
            {{ "Action": "Action type based on email content", "Response": "Drafted email response", "Sentiment": "Identified sentiment", "Priority": priority_number }}
            
            Following is the Email Content:
            _________________________________________

            {0}
            """;
    }
}
