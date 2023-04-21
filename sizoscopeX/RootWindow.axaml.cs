using sizoscopeX.ViewModels;

namespace sizoscopeX
{
    public partial class RootWindow : FluentAppWindow
    {
        private readonly RootWindowViewModel _viewModel;

        [Obsolete("Should not be called except by XAML designer.")]
        public RootWindow()
        {
            InitializeComponent();
            _viewModel = default!;
        }

        public RootWindow(MstatData.Node node)
        {
            InitializeComponent();
            _viewModel = new RootWindowViewModel(node);
            DataContext = _viewModel;
        }
    }
}
