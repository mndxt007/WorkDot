using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using MudBlazor;
using System.Text.Json;
using WorkDot.Models;
using WorkDot.Services.Common;
using WorkDot.Services.Models;

namespace WorkDot.Pages
{
    public partial class Compose
    {
        [Inject]
        public required IServiceProvider ServiceProvider { get; set; }

        [Inject]
        public required IConfiguration Configuration { get; set; }

        [Inject]
        public required Kernel Kernel { get; set; }

        [Inject]
        public required OutlookService OutlookService { get; set; }

        private bool _loadingCustomResponse = false;
        private bool _generatingPlan = false;
        private bool _isProcessing = false;

        private string? _status { get; set; } = string.Empty;
        private string? _customInstruction = string.Empty;
        private MudForm form;
        private EmailPluginModel _generatedPlan = new();
        private EmailContext? _selectedEmail = new();

        protected override async Task OnInitializedAsync()
        {
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

        private async Task GenerateEmailPlan(bool preserveHistory = false)
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
                        var response = await Kernel.InvokePromptAsync(prompt);
                        _generatedPlan = JsonSerializer.Deserialize<EmailPluginModel>(response.ToString())!;
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
            if (selectedEmail != null)
            {
                if (!string.IsNullOrEmpty(_customInstruction))
                {
                    return string.Format(Prompts.CustomComposePrompt, selectedEmail.EmailBody, _customInstruction);
                }

                return string.Format(Prompts.ComposePrompt, selectedEmail.EmailBody);
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