using EmpireAtWar.Commands.Settings;
using EmpireAtWar.Models.Settings;
using EmpireAtWar.Views.ViewImpl;

namespace EmpireAtWar.Views.Settings
{
    public class SettingsView: View<ISettingsModelObserver, ISettingsCommand>
    {
        protected override void OnInitialize()
        {
            
        }

        protected override void OnDispose()
        {
        }
    }
}