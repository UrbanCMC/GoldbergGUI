using MvvmCross.Platforms.Wpf.Presenters.Attributes;
using MvvmCross.Platforms.Wpf.Views;

namespace GoldbergGUI.WPF.Views
{
    /// <summary>
    ///     Interaction logic for MainView.xaml
    /// </summary>
    [MvxContentPresentation(WindowIdentifier = nameof(MainWindow))]
    public partial class MainView : MvxWpfView
    {
        public MainView()
        {
            InitializeComponent();
        }
    }
}