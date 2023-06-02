using System.Collections.ObjectModel;
using static sizoscopeX.TreeLogic;
using System.ComponentModel;

namespace sizoscopeX.ViewModels;

public class DiffWindowViewModel : INotifyPropertyChanged
{
    private MstatData? _baseline, _compare;
    private int _diffSize;
    private bool _loading;

    public DiffWindowViewModel(MstatData baseline, MstatData compare)
    {
        var baselineTree = new ObservableCollection<TreeNode>();
        var compareTree = new ObservableCollection<TreeNode>();
        Loading = true;
        Task.Run(() => Task.FromResult(MstatData.Diff(baseline, compare)))
            .ContinueWith(t =>
            {
                (_baseline, _compare) = t.Result;
                _diffSize = compare.Size - baseline.Size;
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
                PropertyChanged?.Invoke(this, new(nameof(TitleString)));
                Loading = false;
            }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private int _baselineSortMode, _compareSortMode;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<TreeNode>? BaselineItems { get; private set; }
    public ObservableCollection<TreeNode>? CompareItems { get; private set; }
    public MstatData? BaselineData { get; private set; }
    public MstatData? CompareData { get; private set; }
    public string TitleString => $"Diff View - Total accounted difference: {AsFileSize(_diffSize)}";

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
