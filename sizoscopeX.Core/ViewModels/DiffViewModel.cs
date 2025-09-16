using System.Collections.ObjectModel;
using static sizoscopeX.Core.TreeLogic;
using System.ComponentModel;

namespace sizoscopeX.Core.ViewModels;

public sealed class DiffViewModel : INotifyPropertyChanged
{
    private MstatData? _baseline, _compare;
    private int _diffSize;
    private bool _loading;

    public DiffViewModel(MstatData baseline, MstatData compare)
    {
        var baselineTree = new ObservableCollection<TreeNode>();
        var compareTree = new ObservableCollection<TreeNode>();
        Loading = true;
        TitleString = "Diff View - sizoscopeX";
        _ = Utils.TaskRunIfPossible(() => MstatData.Diff(baseline, compare))
            .ContinueOnMainThread(t =>
            {
                (_baseline, _compare) = t;
                _diffSize = compare.Size - baseline.Size;
                compare.InvalidateOwnership();
                BaselineData = _baseline;
                CompareData = _compare;
                RefreshTree(baselineTree, _baseline, Sorter.BySize());
                RefreshTree(compareTree, _compare, Sorter.BySize());
                BaselineItems = baselineTree;
                CompareItems = compareTree;
                PropertyChanged?.Invoke(this, new(nameof(BaselineItems)));
                PropertyChanged?.Invoke(this, new(nameof(CompareItems)));
                PropertyChanged?.Invoke(this, new(nameof(BaselineData)));
                PropertyChanged?.Invoke(this, new(nameof(CompareData)));
                TitleString = $"Diff View - Total accounted difference: {AsFileSize(_diffSize)} - sizoscopeX";
                TitleChangedEvent?.Invoke(this, new());
                Loading = false;
            });
    }

    private int _baselineSortMode, _compareSortMode;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<TreeNode>? BaselineItems { get; private set; }
    public ObservableCollection<TreeNode>? CompareItems { get; private set; }
    public MstatData? BaselineData { get; private set; }
    public MstatData? CompareData { get; private set; }
    public event EventHandler? TitleChangedEvent;
    public string TitleString { get; private set; }

    public Sorter BaselineSorter => BaselineSortMode is 0 ? Sorter.BySize() : Sorter.ByName();
    public Sorter CompareSorter => CompareSortMode is 0 ? Sorter.BySize() : Sorter.ByName();

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

    public int BaselineSortMode
    {
        get => _baselineSortMode;
        set
        {
            if (value != _baselineSortMode && BaselineItems is not null && _baseline is not null)
            {
                _baselineSortMode = value;
                PropertyChanged?.Invoke(this, new(nameof(BaselineSortMode)));
                PropertyChanged?.Invoke(this, new(nameof(BaselineSorter)));
                RefreshTree(BaselineItems, _baseline, BaselineSorter);
            }
        }
    }

    public int CompareSortMode
    {
        get => _compareSortMode;
        set
        {
            if (value != _compareSortMode && CompareItems is not null && _compare is not null)
            {
                _compareSortMode = value;
                PropertyChanged?.Invoke(this, new(nameof(CompareSortMode)));
                PropertyChanged?.Invoke(this, new(nameof(CompareSorter)));
                RefreshTree(CompareItems, _compare, CompareSorter);
            }
        }
    }
}
