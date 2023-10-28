using System.Collections.ObjectModel;
using System.Reflection.Metadata;
using static MstatData;
using static sizoscopeX.Core.TreeLogic;

namespace sizoscopeX.Core
{
    public class TreeLogic
    {
        public static void RefreshTree(ObservableCollection<TreeNode> items, MstatData data, Sorter sorter)
        {
            items.Clear();

            if (data.UnownedFrozenObjectSize > 0)
            {
                items.Add(new TreeNode($"Frozen objects ({AsFileSize(data.UnownedFrozenObjectSize)})", NodeType.FrozenData, sorter)
                {
                    Tag = data
                });
            }

            var asms = data.GetScopes();
            foreach (var asm in sorter.Sort(asms))
            {
                // Do not show for now. This is currently not possible to diff and just causes problems.
                if (asm.Name == "System.Private.CompilerGenerated")
                    continue;

                // Aggregate size can be zero if this is a diff
                if (asm.AggregateSize == 0)
                    continue;

                string name = $"{asm.Name} ({AsFileSize(asm.AggregateSize)})";
                TreeNode node = new TreeNode(name, 0, sorter)
                {
                    Tag = asm
                };
                items.Add(node);
            }
        }

        public static void Expand(TreeNode node)
        {
            if (node is { Tag: MstatData mstat, Type: NodeType.FrozenData })
            {
                foreach (var frozenObj in node.Sorter.Sort(mstat.GetFrozenObjects()))
                {
                    node.Nodes.Add(new TreeNode($"Instance of {frozenObj} ({AsFileSize(frozenObj.Size)})", NodeType.FrozenData, node.Sorter)
                    {
                        Tag = frozenObj.NodeId,
                    });
                }
            }
            else if (node is { Tag: MstatAssembly resourceAssembly, Type: NodeType.Resource })
            {
                foreach (var res in node.Sorter.Sort(resourceAssembly.GetManifestResources().Select(r => (r.Name, r.Size))))
                {
                    string name = $"{res.Name} ({AsFileSize(res.AggregateSize)})";
                    var newNode = new TreeNode(name, NodeType.Resource, node.Sorter);
                    node.Nodes.Add(newNode);
                }
            }
            else if (node is { Tag: MstatAssembly asm })
            {
                if (asm.GetManifestResources().MoveNext())
                {
                    string name = "Resources";
                    var newNode = new TreeNode(name, NodeType.Resource, node.Sorter);
                    newNode.Tag = asm;
                    node.Nodes.Add(newNode);
                }

                var namespacesAndSizes = asm.GetTypes()
                    .Where(t => t.Namespace.Length > 0)
                    .GroupBy(t => t.Namespace)
                    .Select(g => (Name: g.Key, AggregateSize: g.Sum(t => t.AggregateSize)));
                foreach (var ns in node.Sorter.Sort(namespacesAndSizes))
                {
                    string name = $"{ns.Name} ({AsFileSize(ns.AggregateSize)})";
                    var newNode = new TreeNode(name, NodeType.Namespace, node.Sorter);
                    newNode.Tag = (asm, ns.Name);
                    node.Nodes.Add(newNode);
                }

                AppendTypes(node, asm.GetTypes(), d => d.Namespace.Length == 0);
            }
            else if (node is { Tag: (MstatAssembly a, string ns) })
            {
                AppendTypes(node, a.GetTypes(), d => d.Namespace == ns);
            }
            else if (node is { Tag: MstatTypeDefinition frozenDef, Type: NodeType.FrozenData })
            {
                foreach (var frozenObj in node.Sorter.Sort(frozenDef.GetFrozenObjects()))
                {
                    node.Nodes.Add(new TreeNode($"Instance of {frozenObj} ({AsFileSize(frozenObj.Size)})", NodeType.FrozenData, node.Sorter)
                    {
                        Tag = frozenObj.NodeId,
                    });
                }
            }
            else if (node is { Tag: MstatTypeSpecification frozenSpec, Type: NodeType.FrozenData })
            {
                foreach (var frozenObj in node.Sorter.Sort(frozenSpec.GetFrozenObjects()))
                {
                    node.Nodes.Add(new TreeNode($"Instance of {frozenObj} ({AsFileSize(frozenObj.Size)})", NodeType.FrozenData, node.Sorter)
                    {
                        Tag = frozenObj.NodeId,
                    });
                }
            }
            else if (node is { Tag: MstatTypeDefinition genericDef, Type: NodeType.Instantiation })
            {
                foreach (var inst in node.Sorter.Sort(genericDef.GetTypeSpecifications()))
                {
                    string name = $"{inst} ({AsFileSize(inst.AggregateSize)})";
                    var newNode = new TreeNode(name, NodeType.Class, node.Sorter);
                    newNode.Tag = inst;

                    node.Nodes.Add(newNode);
                }
            }
            else if (node is { Tag: MstatTypeSpecification spec })
            {
                if (spec.GetFrozenObjects().MoveNext())
                {
                    TreeNode newNode = new TreeNode("Frozen objects", NodeType.FrozenData, node.Sorter);
                    newNode.Tag = spec;
                    node.Nodes.Add(newNode);
                }
                AppendMembers(node, spec.GetMembers());
            }
            else if (node is { Tag: MstatTypeDefinition def })
            {
                if (def.GetFrozenObjects().MoveNext())
                {
                    TreeNode newNode = new TreeNode("Frozen objects", NodeType.FrozenData, node.Sorter);
                    newNode.Tag = def;
                    node.Nodes.Add(newNode);
                }
                if (def.GetTypeSpecifications().MoveNext())
                {
                    TreeNode newNode = new TreeNode("Instantiations", NodeType.Instantiation, node.Sorter);
                    newNode.Tag = def;
                    node.Nodes.Add(newNode);
                }
                AppendTypes(node, def.GetNestedTypes(), x => true);
                AppendMembers(node, def.GetMembers());
            }
            else if (node is { Tag: MstatMemberDefinition memberDef })
            {
                foreach (var inst in memberDef.GetInstantiations())
                {
                    string name = $"{inst} ({AsFileSize(inst.Size)})";
                    var newNode = new TreeNode(name, NodeType.Method, node.Sorter);
                    newNode.Tag = inst;
                    node.Nodes.Add(newNode);
                }
            }

            static void AppendTypes(TreeNode node, Enumerator<TypeReferenceHandle, MstatTypeDefinition, MoveToNextInScope> list, Func<MstatTypeDefinition, bool> filter)
            {
                foreach (var t in node.Sorter.Sort(list.Where(filter)))
                {
                    string name = $"{t.Name} ({AsFileSize(t.AggregateSize)})";
                    var n = new TreeNode(name, NodeType.Class, node.Sorter);
                    n.Tag = t;
                    node.Nodes.Add(n);
                }
            }

            static void AppendMembers(TreeNode node, Enumerator<MemberReferenceHandle, MstatMemberDefinition, MoveToNextMemberOfType> list)
            {
                foreach (var t in node.Sorter.Sort(list))
                {
                    string name = $"{t} ({AsFileSize(t.AggregateSize)})";
                    var n = new TreeNode(name, t.IsField ? NodeType.Field : NodeType.Method, node.Sorter);
                    n.Tag = t;
                    node.Nodes.Add(n);
                }
            }
        }

        public static string AsFileSize(int size)
            => Math.Abs(size) switch
            {
                < 1024 => $"{size:F0} B",
                < 1024 * 1024 => $"{size / 1024f:F1} kB",
                _ => $"{size / (1024f * 1024f):F1} MB",
            };

        public class Sorter
        {
            public string Key { get; private set; }

            private Sorter(string key) => Key = key;

            public IEnumerable<MstatAssembly> Sort(IEnumerable<MstatAssembly> asms)
                => Key switch
                {
                    "Name" => asms.OrderBy(a => a.Name),
                    "Size" => asms.OrderByDescending(a => a.AggregateSize),
                    _ => asms
                };
            public IEnumerable<(string Name, int AggregateSize)> Sort(IEnumerable<(string Name, int AggregateSize)> ns)
                => Key switch
                {
                    "Name" => ns.OrderBy(n => n.Name),
                    "Size" => ns.OrderByDescending(n => n.AggregateSize),
                    _ => ns
                };

            public IEnumerable<MstatTypeSpecification> Sort(IEnumerable<MstatTypeSpecification> specs)
                => Key switch
                {
                    "Name" => specs.OrderBy(s => s.ToString()),
                    "Size" => specs.OrderByDescending(s => s.AggregateSize),
                    _ => specs
                };

            public IEnumerable<MstatTypeDefinition> Sort(IEnumerable<MstatTypeDefinition> types)
                => Key switch
                {
                    "Name" => types.OrderBy(t => t.Name),
                    "Size" => types.OrderByDescending(t => t.AggregateSize),
                    _ => types
                };

            public IEnumerable<MstatMemberDefinition> Sort(IEnumerable<MstatMemberDefinition> members)
                => Key switch
                {
                    "Name" => members.OrderBy(t => t.Name),
                    "Size" => members.OrderByDescending(t => t.AggregateSize),
                    _ => members
                };
            public IEnumerable<MstatFrozenObject> Sort(IEnumerable<MstatFrozenObject> frozenObjects)
                => Key switch
                {
                    "Name" => frozenObjects.OrderBy(t => t.ToString()),
                    "Size" => frozenObjects.OrderByDescending(t => t.Size),
                    _ => frozenObjects
                };

            public IEnumerable<(string Name, int Size, int Id)> Sort(IEnumerable<(string Name, int Size, int Id)> members)
                => Key switch
                {
                    "Name" => members.OrderBy(t => t.Name),
                    "Size" =>members.OrderByDescending(t => t.Size),
                    _ => members
                };

            public static Sorter ByName() => new Sorter("Name");
            public static Sorter BySize() => new Sorter("Size");
            public static Sorter ByDefault() => new Sorter("Default");

            public override string ToString() => $"Sort by {Key}";
        }
    }
}