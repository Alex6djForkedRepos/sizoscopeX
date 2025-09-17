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
            ExpandTree();
        }

        public RootView(MstatData.Node node)
        {
            InitializeComponent();
            _viewModel = new RootViewModel(node);
            DataContext = _viewModel;
        }

        private void ExpandTree()
        {
            var queue = new Queue<TreeNode>();
            foreach (var item in _viewModel.Items)
            {
                queue.Enqueue(item);
            }

            var limit = 512;
            var currentNode = _viewModel.Items.FirstOrDefault();
            while (--limit >= 0 && queue.TryDequeue(out var item))
            {
                item.IsExpanded = true;
                bool isFirst = true;
                foreach (var child in item.Nodes)
                {
                    queue.Enqueue(child);
                    if (isFirst && item == currentNode)
                    {
                        currentNode = child;
                        isFirst = false;
                    }
                }
            }

            while (currentNode != null)
            {
                currentNode.IsExpanded = true;
                currentNode = currentNode.FirstNode;
            }
        }
    }
}
