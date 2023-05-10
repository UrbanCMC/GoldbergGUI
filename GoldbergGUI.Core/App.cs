using GoldbergGUI.Core.Utils;
using GoldbergGUI.Core.ViewModels;
using MvvmCross.IoC;
using MvvmCross.ViewModels;

namespace GoldbergGUI.Core
{
    public class App : MvxApplication
    {
        public override void Initialize()
        {
            CreatableTypes()
                .EndingWith("Service")
                .AsInterfaces()
                .RegisterAsLazySingleton();
            //RegisterAppStart<MainViewModel>();
            RegisterCustomAppStart<CustomMvxAppStart<MainViewModel>>();

            LoadSettings();
        }

        private void LoadSettings()
        {
            // Call both read and write to ensure any new settings are added to the file
            Settings.Instance.Read();
            Settings.Instance.Write();
        }
    }
}