using MvvmCross.Platforms.Wpf.Presenters.Attributes;
using MvvmCross.Platforms.Wpf.Views;

namespace GoldbergGUI.WPF.Views
{
    [MvxWindowPresentation(Identifier = nameof(SearchResultView), Modal = false)]
    public partial class SearchResultView
    {
        public SearchResultView()
        {
            InitializeComponent();
        }
    }
}