using System.Collections.ObjectModel;

namespace sizoscopeX.Core.ViewModels;

public class RootViewModel
{
    public ObservableCollection<TreeNode> Items { get; } = new ObservableCollection<TreeNode>();
    public string Name { get; }
    public RootViewModel(MstatData.Node node)
    {
        var tree = CreateTree(node);
        Name = node.Name;
        Items = tree.Nodes;
    }

    private TreeNode CreateTree(MstatData.Node node, string? label = null)
    {
        TreeNode result = new TreeNode(label is null ? node.Name : $"({label}) {node.Name}", null, TreeLogic.Sorter.ByDefault());
        foreach (var edge in node.Edges)
            result.Nodes.Add(CreateTree(edge.Node, edge.Label));

        return result;
    }
}
