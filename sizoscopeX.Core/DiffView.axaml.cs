using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using sizoscopeX.Core.ViewModels;
using static MstatData;

namespace sizoscopeX.Core
{
    public partial class DiffView : UserControl
    {
        private readonly DiffViewModel _viewModel;

        [Obsolete("Should not be called except by XAML designer.")]
        public DiffView()
        {
            InitializeComponent();
            _viewModel = default!;
        }

        public DiffView(MstatData baseline, MstatData compare)
        {
            InitializeComponent();
            _viewModel = new(baseline, compare);
            DataContext = _viewModel;
            _viewModel.TitleChangedEvent += (obj, e) =>
            {
                if (obj is DiffViewModel vm)
                {
                    Utils.SetTitle(vm.TitleString);
                }
            };
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            Utils.SetTitle(_viewModel.TitleString);
            base.OnLoaded(e);
        }

        private async void Tree_DoubleTapped(object? sender, TappedEventArgs args)
        {
            if (sender is not TreeView treeView || 
                treeView.SelectedItem is not TreeNode tn ||
                treeView.Tag is not MstatData currentData) return;

            int? id = tn.Tag switch
            {
                MstatTypeDefinition typedef => typedef.NodeId,
                MstatTypeSpecification typespec => typespec.NodeId,
                MstatMemberDefinition memberdef => memberdef.NodeId,
                MstatMethodSpecification methodspec => methodspec.NodeId,
                MstatFrozenObject frozenObject => frozenObject.NodeId,
                int nodeId => nodeId,
                _ => null
            };

            if (id.HasValue)
            {
                if (id.Value < 0)
                {
                    var dialog = new ContentDialog
                    {
                        CloseButtonText = "OK",
                        Title = "Error",
                        Content = "Dependency graph information is only available in .NET 8 or later."
                    };
                    await dialog.ShowAsync(TopLevel.GetTopLevel(this));
                    return;
                }

                var node = currentData.GetNodeForId(id.Value, out _);
                if (node == null)
                {
                    var dialog = new ContentDialog
                    {
                        CloseButtonText = "OK",
                        Title = "Error",
                        Content = "Unable to load dependency graph. Was IlcGenerateDgmlFile=true specified?"
                    };
                    await dialog.ShowAsync(TopLevel.GetTopLevel(this));
                    return;
                }

                var view = new RootView(node);
                Utils.ShowWindow(view);
            }
        }
    }
}
