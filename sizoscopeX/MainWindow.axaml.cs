﻿using Avalonia.Controls;
using sizoscopeX.ViewModels;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Controls;
using Avalonia.Input;
using static MstatData;

namespace sizoscopeX;

public partial class MainWindow : FluentAppWindow
{
    private readonly MainWindowViewModel viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = viewModel;

        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
    }

    void DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects &= DragDropEffects.Copy;
        if (!e.Data.Contains(DataFormats.Files) || e.Data.GetFiles()?.FirstOrDefault() is not IStorageFile)
            e.DragEffects = DragDropEffects.None;
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        e.DragEffects &= DragDropEffects.Copy;
        if (e.Data.Contains(DataFormats.Files))
        {
            var files = e.Data.GetFiles();
            if (e.Data.GetFiles()?.FirstOrDefault() is IStorageFile file)
            {
                viewModel.FileName = file.TryGetLocalPath() ?? throw new InvalidOperationException();
            }
        }
    }

    public async void Open_Clicked(object? sender, RoutedEventArgs args)
    {
        var result = await StorageProvider.OpenFilePickerAsync(new()
        {
            AllowMultiple = false,
            FileTypeFilter = new[] {
                new FilePickerFileType("Mstat files") { Patterns = new[] { "*.mstat" } },
                new FilePickerFileType("All files") { Patterns = new[] { "*.*" } }
            },
            Title = "Open a file for analysis"
        });
        if (result.Any())
        {
            viewModel.FileName = result.First().TryGetLocalPath() ?? throw new InvalidOperationException();
        }
    }

    public async void Diff_Clicked(object? sender, RoutedEventArgs args)
    {
        var currentData = viewModel.CurrentData;
        if (currentData is null)
        {
            var dialog = new ContentDialog
            {
                CloseButtonText = "OK",
                Title = "Error",
                Content = "You haven't open any file as the baseline."
            };
            await dialog.ShowAsync();
            return;
        }

        var result = await StorageProvider.OpenFilePickerAsync(new()
        {
            AllowMultiple = false,
            FileTypeFilter = new[] {
                new FilePickerFileType("Mstat files") { Patterns = new[] { "*.mstat" } },
                new FilePickerFileType("All files") { Patterns = new[] { "*.*" } }
            },
            Title = "Open a file for compare"
        });
        if (result.Any())
        {
            using var mstaDataToCompare = Read(result.First().TryGetLocalPath() ?? throw new InvalidOperationException());
            await new DiffWindow(currentData, mstaDataToCompare).ShowDialog(this);
        }
    }

    public void Refresh_Clicked(object? sender, RoutedEventArgs args)
    {
        viewModel.Refresh();
    }

    public void Exit_Clicked(object? sender, RoutedEventArgs args)
    {
        Environment.Exit(0);
    }

    private async void Tree_DoubleTapped(object? sender, TappedEventArgs args)
    {
        if (sender is not TreeView treeView || treeView.SelectedItem is not TreeNode tn) return;
        var currentData = viewModel.CurrentData;
        if (currentData is null) return;

        int? id = tn.Tag switch
        {
            MstatTypeDefinition typedef => typedef.NodeId,
            MstatTypeSpecification typespec => typespec.NodeId,
            MstatMemberDefinition memberdef => memberdef.NodeId,
            MstatMethodSpecification methodspec => methodspec.NodeId,
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
                    Content = "Dependency graph information is only available in .NET 8 Preview 4 or later."
                };
                await dialog.ShowAsync();
                return;
            }

            var node = currentData.GetNodeForId(id.Value);
            if (node == null)
            {
                var dialog = new ContentDialog
                {
                    CloseButtonText = "OK",
                    Title = "Error",
                    Content = "Unable to load dependency graph. Was IlcGenerateDgmlLog=true specified?"
                };
                await dialog.ShowAsync();
                return;
            }

            await new RootWindow(node).ShowDialog(this);
        }
    }

    public async void ThirdParty_Clicked(object? sender, RoutedEventArgs args)
    {
        var dialog = new ContentDialog
        {
            CloseButtonText = "OK",
            Title = "Third party notices",
            Content = """
                       License notice for SharpDevelop

                                                  The MIT License (MIT)

                       Copyright (c) 2002-2016 AlphaSierraPapa

                       Permission is hereby granted, free of charge, to any person obtaining a copy
                       of this software and associated documentation files (the "Software"), to deal
                       in the Software without restriction, including without limitation the rights
                       to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
                       copies of the Software, and to permit persons to whom the Software is
                       furnished to do so, subject to the following conditions:

                       The above copyright notice and this permission notice shall be included in
                       all copies or substantial portions of the Software.

                       THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
                       IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
                       FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
                       AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
                       LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
                       OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
                       THE SOFTWARE.

                       License notice for Avalonia

                                              The MIT License (MIT)

                       Copyright (c) .NET Foundation and Contributors All Rights Reserved

                       Permission is hereby granted, free of charge, to any person obtaining a copy 
                       of this software and associated documentation files (the "Software"), to deal 
                       in the Software without restriction, including without limitation the rights 
                       to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
                       copies of the Software, and to permit persons to whom the Software is 
                       furnished to do so, subject to the following conditions:

                       The above copyright notice and this permission notice shall be included in 
                       all copies or substantial portions of the Software.

                       THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
                       IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
                       FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
                       AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
                       LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
                       OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
                       THE SOFTWARE.

                       License notice for FluentAvalonia

                                              MIT License

                       Copyright (c) 2020 amwx

                       Permission is hereby granted, free of charge, to any person obtaining a copy
                       of this software and associated documentation files (the "Software"), to deal
                       in the Software without restriction, including without limitation the rights
                       to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
                       copies of the Software, and to permit persons to whom the Software is
                       furnished to do so, subject to the following conditions:

                       The above copyright notice and this permission notice shall be included in all
                       copies or substantial portions of the Software.

                       THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
                       IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
                       FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
                       AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
                       LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
                       OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
                       SOFTWARE.
                       """
        };
        await dialog.ShowAsync();
    }

    public async void About_Clicked(object? sender, RoutedEventArgs args)
    {
        var dialog = new ContentDialog
        {
            CloseButtonText = "OK",
            Title = "About",
            Content = """
                       Copyright (c) 2023 Michal Strehovsky
                       Copyright (c) 2023 hez2010

                       https://github.com/MichalStrehovsky
                       https://github.com/hez2010

                       .NET Native AOT binary size analysis tool.

                       This program is free software: you can redistribute it and/or modify
                       it under the terms of the GNU Affero General Public License as published
                       by the Free Software Foundation, either version 3 of the License, or
                       (at your option) any later version.
                       
                       This program is distributed in the hope that it will be useful,
                       but WITHOUT ANY WARRANTY; without even the implied warranty of
                       MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
                       GNU Affero General Public License for more details.
                       
                       You should have received a copy of the GNU Affero General Public License
                       along with this program.  If not, see <https://www.gnu.org/licenses/>.
                       """
        };
        await dialog.ShowAsync();
    }
}