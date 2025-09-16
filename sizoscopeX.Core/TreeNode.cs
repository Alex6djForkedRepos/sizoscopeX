using System.Collections.ObjectModel;
using System.ComponentModel;
using static sizoscopeX.Core.TreeLogic;

namespace sizoscopeX.Core
{
    public sealed class TreeNode : INotifyPropertyChanged
    {
        private string? name;
        private NodeType? type;
        private bool childrenInitialized;
        private bool isExpanded;

        public event PropertyChangedEventHandler? PropertyChanged;

        public TreeNode(string? name, NodeType? type, Sorter sorter)
        {
            Name = name;
            Type = type;
            Sorter = sorter;
        }

        public string? Name
        {
            get
            {
                if (!childrenInitialized)
                {
                    childrenInitialized = true;
                    InsertChildren(this);
                }
                return name;
            }
            set
            {
                name = value;
                PropertyChanged?.Invoke(value, new(nameof(Name)));
            }
        }

        public void InitializeChildren()
        {
            if (!childrenInitialized)
            {
                childrenInitialized = true;
                InsertChildren(this);
            }
        }

        public NodeType? Type
        {
            get => type;
            set
            {
                type = value;
                PropertyChanged?.Invoke(this, new(nameof(Type)));
            }
        }

        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (isExpanded != value)
                {
                    isExpanded = value;
                    PropertyChanged?.Invoke(this, new(nameof(IsExpanded)));
                }
            }
        }

        public object? Tag { get; set; }

        public MstatData? MstatData { get; set; }

        public TreeNode? FirstNode => Nodes.FirstOrDefault();

        public ObservableCollection<TreeNode> Nodes { get; } = new();

        public Sorter Sorter { get; }
    }
}