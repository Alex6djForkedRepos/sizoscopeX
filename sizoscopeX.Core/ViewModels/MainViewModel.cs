using Avalonia.Threading;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection.Metadata;
using static MstatData;
using static sizoscopeX.Core.TreeLogic;

namespace sizoscopeX.Core.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    public MainViewModel()
    {
        _searchDebouncer = new(TimeSpan.FromMilliseconds(500), DispatcherPriority.Normal, ExecuteSearch);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private MstatData? _data;
    private int _sortMode;
    private int _searchMode;
    private string _searchPattern = "";
    private readonly DispatcherTimer _searchDebouncer;
    private bool _loading;

    public ObservableCollection<TreeNode> Items { get; } = new();
    public ObservableCollection<SearchResultItem> SearchResult { get; } = new();
    public Sorter Sorter => SortMode is 0 ? Sorter.BySize() : Sorter.ByName();
    public bool CanExit => !OperatingSystem.IsBrowser();

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

    public string SearchPattern
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

    public void ClearData()
    {
        _data?.Dispose();
        _data = null;
        Items.Clear();
        SearchResult.Clear();
    }

    public async Task LoadDataAsync(MemoryStream Mstat, MemoryStream? Dgml)
    {
        var newData = await Utils.TaskRunIfPossible(() => Read(Mstat, Dgml));

        _data?.Dispose();
        _data = newData;
        Items.Clear();
        SearchResult.Clear();

        PropertyChanged?.Invoke(this, new(nameof(DataFileSize)));
        PropertyChanged?.Invoke(this, new(nameof(CurrentData)));
        RefreshTree(Items, _data, Sorter);
        RefreshSearch();
    }

    private void ExecuteSearch(object? sender, EventArgs args)
    {
        _searchDebouncer.Stop();
        RefreshSearch();
    }

    private void RefreshSearch()
    {
        if (_data is null)
        {
            return;
        }

        SearchResult.Clear();
        var result = new List<SearchResultItem>();
        foreach (var asm in _data.GetScopes())
        {
            if (asm.Name == "System.Private.CompilerGenerated")
                continue;

            AddTypes(asm.GetTypes());
        }

        foreach (var item in result.OrderBy(x => x.Name))
        {
            SearchResult.Add(item);
        }

        void AddTypes(Enumerator<TypeReferenceHandle, MstatTypeDefinition, MoveToNextInScope> types)
        {
            foreach (var t in types)
            {
                if (t.Name.Contains(_searchPattern) || t.Namespace.Contains(_searchPattern))
                {
                    var newItem = new SearchResultItem(t.ToString(), _data.DgmlSupported && _data.DgmlAvailable && t.NodeId >= 0 ? _data.GetNameForId(t.NodeId) : null, t.Size, t.AggregateSize);

                    newItem.Tag = t;

                    result.Add(newItem);
                }

                AddTypes(t.GetNestedTypes());

                if (_searchMode is 0 or 2)
                {
                    foreach (var s in t.GetTypeSpecifications())
                        AddMembers(s.GetMembers());
                    AddMembers(t.GetMembers());
                }
            }
        }

        void AddMembers(Enumerator<MemberReferenceHandle, MstatMemberDefinition, MoveToNextMemberOfType> members)
        {
            foreach (var m in members)
            {
                if (m.Name.Contains(_searchPattern))
                {
                    var newItem = new SearchResultItem(m.ToString(), _data.DgmlSupported && _data.DgmlAvailable && m.NodeId >= 0 ? _data.GetNameForId(m.NodeId) : null, m.Size, m.AggregateSize);

                    newItem.Tag = m;

                    result.Add(newItem);
                }
            }
        }
    }
}
