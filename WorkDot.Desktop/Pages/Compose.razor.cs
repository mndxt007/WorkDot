using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using MudBlazor;
using System.Text.Json;
using WorkDot.Models;
using WorkDot.Services.Common;

namespace WorkDot.Pages
{
    public partial class Compose
    {
        [Inject]
        public required IServiceProvider ServiceProvider { get; set; }

        [Inject]
        public required IConfiguration Configuration { get; set; }

        [Inject]
        public required ChatCompletionService OpenAIService { get; set; }

        [Inject]
        public required OutlookService OutlookService { get; set; }

        private bool _loadingCustomResponse = false;
        private bool _generatingPlan = false;
        private bool _isProcessing = false;
        private string? _composePrompt;
        private string? _customComposePrompt;
        private string? _status { get; set; } = string.Empty;
        private string? _customInstruction = string.Empty;
        private MudForm form;
        private EmailPluginModel _generatedPlan = new();
        private EmailContext? _selectedEmail = new();

        protected override async Task OnInitializedAsync()
        {
            _composePrompt = Configuration.GetValue<string>("Prompts:Compose")!;
            _customComposePrompt = Configuration.GetValue<string>("Prompts:CustomCompose")!;
            _selectedEmail = GetEmailContext();

            try
            {
                await GenerateEmailPlan();
            }
            catch (Exception ex)
            {
                _status = ex.Message;
                Console.WriteLine($"Error: {ex}");
            }

            OutlookService._outlookApp.ActiveExplorer().SelectionChange += OnSelectionChanged;
        }

        private async Task GenerateEmailPlan(bool preserveHistory = true)
        {
            try
            {
                _status = "Reading Email";
                _generatingPlan = true;
                StateHasChanged();

                if (_selectedEmail is null)
                {
                    return;
                }

                _selectedEmail.EmailBody = GetProcessedEmailBody(_selectedEmail.EmailBody!);

                if (!string.IsNullOrEmpty(_selectedEmail.EmailBody))
                {
                    _status = "Generating Plan";
                    StateHasChanged();

                    var prompt = ConstructPlanPrompt(_selectedEmail);
                    if (!string.IsNullOrEmpty(prompt))
                    {
                        var response = await OpenAIService.GetResponseAsync(prompt, preserveHistory);
                        _generatedPlan = JsonSerializer.Deserialize<EmailPluginModel>(response)!;
                    }
                }
            }
            catch (Exception ex)
            {
                _status = ex.Message;
                Console.WriteLine($"Error: {ex}");
                _generatingPlan = false;
            }

            _generatingPlan = false;
            StateHasChanged();
        }

        private string GetProcessedEmailBody(string emailBody)
        {
            return emailBody?.Length > 8000 ? emailBody[..8000] : emailBody!;
        }

        private string ConstructPlanPrompt(EmailContext selectedEmail)
        {
            if (selectedEmail != null && !string.IsNullOrEmpty(_composePrompt))
            {
                if (!string.IsNullOrEmpty(_customInstruction))
                {
                    return string.Format(_composePrompt, selectedEmail.EmailBody, string.Join(_customComposePrompt, _customInstruction, Environment.NewLine));
                }

                return string.Format(_composePrompt, selectedEmail.EmailBody, string.Empty);
            }

            return string.Empty;
        }

        private EmailContext GetEmailContext()
        {
            var response = OutlookService.GetEmailDataAsync();
            return response;
        }

        private void ReplyAll(string? entryID, string response)
        {
            OutlookService.ReplyAll(entryID!, response);
        }

        private async Task GenerateCustomPlan()
        {
            await form.Validate();

            if(form.IsValid)
            {
                await GenerateEmailPlan();
            }
        }

        private async void OnSelectionChanged()
        {
            if (_isProcessing) return;

            _isProcessing = true;

            try
            {
                _selectedEmail = GetEmailContext();

                try
                {
                    await InvokeAsync(() => GenerateEmailPlan(false));
                }
                catch (Exception ex)
                {
                    _status = ex.Message;
                    Console.WriteLine($"Error: {ex.ToString()}");
                }
                await InvokeAsync(StateHasChanged);

            }
            catch (Exception ex)
            {
                _status = ex.Message;
                Console.WriteLine($"Error: {ex.ToString()}");
            }
            finally
            {
                _isProcessing = false;
            }
        }
    }
}