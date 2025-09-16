using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using sizoscopeX.Core.ViewModels;

namespace sizoscopeX.Core
{
    public partial class RootView : UserControl
    {
        private readonly RootViewModel _viewModel;

        [Obsolete("Should not be called except by XAML designer.")]
        public RootView()
        {
            InitializeComponent();
            _viewModel = default!;
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            Utils.SetTitle("Root View - sizoscopeX");
            base.OnLoaded(e);
        }

        public RootView(MstatData.Node node)
        {
            InitializeComponent();
            _viewModel = new RootViewModel(node);
            DataContext = _viewModel;
        }
    }
}
