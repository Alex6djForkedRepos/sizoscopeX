using Avalonia.Threading;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection.Metadata;
using static MstatData;
using static sizoscopeX.TreeLogic;

namespace sizoscopeX.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    public MainViewModel()
    {
        _searchDebouncer = new(TimeSpan.FromMilliseconds(500), DispatcherPriority.Normal, ExecuteSearch);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private MstatData? _data;
    private (MemoryStream Mstat, MemoryStream? Dgml)? _file;
    private int _sortMode;
    private int _searchMode;
    private string? _searchPattern;
    private readonly DispatcherTimer _searchDebouncer;
    private bool _loading;

    public ObservableCollection<TreeNode> Items { get; } = new();
    public ObservableCollection<SearchResultItem> SearchResult { get; } = new();
    public Sorter Sorter => SortMode is 0 ? Sorter.BySize() : Sorter.ByName();
    public bool Loading
    {
        get => _loading;
        set
        {
            if (value != _loading)
            {
                _loading = value;
                PropertyChanged?.Invoke(this, new(nameof(Loading)));
            }
        }
    }

    public void Refresh()
    {
        if (_data is not null)
        {
            RefreshTree(Items, _data, Sorter);
            RefreshSearch();
        }
    }

    public MstatData? CurrentData => _data;

    public (MemoryStream Mstat, MemoryStream? Dgml)? File
    {
        get => _file;
        set
        {
            if (value != _file)
            {
                _file?.Mstat?.Dispose();
                _file?.Dgml?.Dispose();
                _file = value;
                PropertyChanged?.Invoke(this, new(nameof(File)));
                _data?.Dispose();
                if (value is null)
                {
                    _data = null;
                    Items.Clear();
                    SearchResult.Clear();
                    return;
                }
                Loading = true;
                _ = Utils.TaskRunIfPossible(() => Read(value.Value.Mstat, value.Value.Dgml))
                    .ContinueOnMainThread(t =>
                    {
                        _data = t;
                        PropertyChanged?.Invoke(this, new(nameof(DataFileSize)));
                        RefreshTree(Items, _data, Sorter);
                        RefreshSearch();
                        Loading = false;
                    });
            }
        }
    }

    public int SortMode
    {
        get => _sortMode;
        set
        {
            if (value != _sortMode)
            {
                _sortMode = value;
                PropertyChanged?.Invoke(this, new(nameof(SortMode)));
                PropertyChanged?.Invoke(this, new(nameof(Sorter)));
                if (_data is not null)
                {
                    RefreshTree(Items, _data, Sorter);
                }
            }
        }
    }

    public int SearchMode
    {
        get => _searchMode;
        set
        {
            if (value != _searchMode)
            {
                _searchMode = value;
                PropertyChanged?.Invoke(this, new(nameof(SearchMode)));
                RefreshSearch();
            }
        }
    }

    public string? SearchPattern
    {
        get => _searchPattern;
        set
        {
            if (value != _searchPattern)
            {
                _searchPattern = value;
                PropertyChanged?.Invoke(this, new(nameof(SearchPattern)));
                _searchDebouncer.Stop();
                _searchDebouncer.Start();
            }
        }
    }

    public string? DataFileSize => AsFileSize(CurrentData?.Size ?? 0);

    private void ExecuteSearch(object? sender, EventArgs args)
    {
        _searchDebouncer.Stop();
        RefreshSearch();
    }

    private void RefreshSearch()
    {
        if (_searchPattern is null || _data is null)
        {
            return;
        }

        SearchResult.Clear();

        if (_searchPattern.Length > 0)
        {
            foreach (var asm in _data.GetScopes())
            {
                if (asm.Name == "System.Private.CompilerGenerated")
                    continue;

                AddTypes(asm.GetTypes());
            }
        }

        void AddTypes(Enumerator<TypeReferenceHandle, MstatTypeDefinition, MoveToNextInScope> types)
        {
            foreach (var t in types)
            {
                if (t.Name.Contains(_searchPattern) || t.Namespace.Contains(_searchPattern))
                {
                    var newItem = new SearchResultItem(t.ToString(), t.Size, t.AggregateSize);

                    newItem.Tag = t;

                    SearchResult.Add(newItem);
                }

                AddTypes(t.GetNestedTypes());

                if (_searchMode is 0 or 2)
                    AddMembers(t.GetMembers());
            }
        }

        void AddMembers(Enumerator<MemberReferenceHandle, MstatMemberDefinition, MoveToNextMemberOfType> members)
        {
            foreach (var m in members)
            {
                if (m.Name.Contains(_searchPattern))
                {
                    var newItem = new SearchResultItem(m.ToString(), m.Size, m.AggregateSize);

                    newItem.Tag = m;

                    SearchResult.Add(newItem);
                }
            }
        }
    }

    class SearchResultComparer : IComparer
    {
        public bool InvertSort { get; set; }

        public int SortColumn { get; set; }

        public int Compare(object? x, object? y)
        {
            var i1 = (SearchResultItem)x!;
            var i2 = (SearchResultItem)y!;

            int result;
            if (SortColumn == 0)
            {
                string? s1 = i1.Tag?.ToString();
                string? s2 = i2.Tag?.ToString();
                result = string.Compare(s1, s2);
            }
            else
            {
                int v1 = i1.Tag switch
                {
                    MstatTypeDefinition def => SortColumn == 1 ? def.Size : def.AggregateSize,
                    MstatTypeSpecification spec => SortColumn == 1 ? spec.Size : spec.AggregateSize,
                    MstatMemberDefinition mem => SortColumn == 1 ? mem.Size : mem.AggregateSize,
                    MstatMethodSpecification met => met.Size,
                    _ => throw new InvalidOperationException()
                };
                int v2 = i2.Tag switch
                {
                    MstatTypeDefinition def => SortColumn == 1 ? def.Size : def.AggregateSize,
                    MstatTypeSpecification spec => SortColumn == 1 ? spec.Size : spec.AggregateSize,
                    MstatMemberDefinition mem => SortColumn == 1 ? mem.Size : mem.AggregateSize,
                    MstatMethodSpecification met => met.Size,
                    _ => throw new InvalidOperationException()
                };
                result = v1.CompareTo(v2);
            }

            return InvertSort ? -result : result;
        }
    }
}
