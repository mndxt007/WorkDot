using Microsoft.AspNetCore.Components;
using MudBlazor;
using WorkDot.Services.Presentation;

namespace WorkDot.Shared
{
    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        [Inject] private LayoutService LayoutService { get; set; }

        private MudThemeProvider _mudThemeProvider;
        private bool _drawerOpen = false;
        protected override void OnInitialized()
        {
            LayoutService.SetBaseTheme(Theme.AdminPanelTheme());
            base.OnInitialized();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                await ApplyUserPreferences();
                await _mudThemeProvider.WatchSystemPreference(OnSystemPreferenceChanged);
                StateHasChanged();
            }
        }

        private async Task ApplyUserPreferences()
        {
            var defaultDarkMode = await _mudThemeProvider.GetSystemPreference();
            await LayoutService.ApplyUserPreferences(defaultDarkMode);
        }

        private async Task OnSystemPreferenceChanged(bool newValue)
        {
            await LayoutService.OnSystemPreferenceChanged(newValue);
        }

        public void Dispose()
        {
            LayoutService.MajorUpdateOccured -= LayoutServiceOnMajorUpdateOccured;
        }

        private void ToggleDrawer()
        {
            _drawerOpen = !_drawerOpen;
        }
        private void LayoutServiceOnMajorUpdateOccured(object sender, EventArgs e) => StateHasChanged();
    }
}
