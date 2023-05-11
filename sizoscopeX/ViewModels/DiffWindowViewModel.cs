﻿using System.Collections.ObjectModel;
using static sizoscopeX.TreeLogic;
using System.ComponentModel;

namespace sizoscopeX.ViewModels;

public class DiffWindowViewModel : INotifyPropertyChanged
{
    private readonly MstatData _baseline, _compare;
    private readonly int _diffSize;

    public DiffWindowViewModel(MstatData baseline, MstatData compare)
    {
        var baselineTree = new ObservableCollection<TreeNode>();
        var compareTree = new ObservableCollection<TreeNode>();
        (_baseline, _compare) = MstatData.Diff(baseline, compare);
        _diffSize = compare.Size - baseline.Size;
        BaselineData = _baseline;
        CompareData = _compare;
        RefreshTree(baselineTree, _baseline, Sorter.BySize());
        RefreshTree(compareTree, _compare, Sorter.BySize());
        BaselineItems = baselineTree;
        CompareItems = compareTree;
    }

    private int _baselineSortMode, _compareSortMode;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<TreeNode> BaselineItems { get; }
    public ObservableCollection<TreeNode> CompareItems { get; }
    public MstatData BaselineData { get; }
    public MstatData CompareData { get; }
    public string BaselineDataFileSize => AsFileSize(BaselineData.Size);
    public string CompareDataFileSize => AsFileSize(CompareData.Size);
    public string TitleString => $"Diff View - Total accounted difference: {AsFileSize(_diffSize)}";

    public Sorter BaselineSorter => BaselineSortMode is 0 ? Sorter.BySize() : Sorter.ByName();
    public Sorter CompareSorter => CompareSortMode is 0 ? Sorter.BySize() : Sorter.ByName();

    public int BaselineSortMode
    {
        get => _baselineSortMode;
        set
        {
            if (value != _baselineSortMode)
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
            if (value != _compareSortMode)
            {
                _compareSortMode = value;
                PropertyChanged?.Invoke(this, new(nameof(CompareSortMode)));
                PropertyChanged?.Invoke(this, new(nameof(CompareSorter)));
                RefreshTree(CompareItems, _compare, CompareSorter);
            }
        }
    }
}
