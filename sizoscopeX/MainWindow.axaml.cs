using Avalonia.Controls;
using sizoscopeX.ViewModels;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Controls;
using Avalonia.Input;
using static MstatData;
using Avalonia.Threading;

namespace sizoscopeX;

public partial class MainWindow : FluentAppWindow
{
    private readonly MainWindowViewModel viewModel = new();
    private static readonly string[] filterPatterns = new[] { "*.mstat" };

    public MainWindow()
    {
        InitializeComponent();
        DataContext = viewModel;

        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects &= DragDropEffects.Copy;
        if (!e.Data.Contains(DataFormats.Files) || e.Data.GetFiles()?.FirstOrDefault() is not IStorageFile)
            e.DragEffects = DragDropEffects.None;
    }

    private async void Drop(object? sender, DragEventArgs e)
    {
        var currentFile = viewModel.FileName;
        try
        {
            e.DragEffects &= DragDropEffects.Copy;
            if (e.Data.Contains(DataFormats.Files))
            {
                if (e.Data.GetFiles()?.FirstOrDefault() is IStorageFile file)
                {
                    viewModel.FileName = file.TryGetLocalPath() is [.., '.', 'm' or 'M', 's' or 'S', 't' or 'T', 'a' or 'A', 't' or 'T'] path ? path : throw new InvalidOperationException("An invalid file has been dropped.");
                }
            }
        }
        catch (Exception ex)
        {
            await PromptErrorAsync(ex.Message);
            await Dispatcher.UIThread.InvokeAsync(() => viewModel.FileName = currentFile);
        }
    }

    public async void Open_Clicked(object? sender, RoutedEventArgs args)
    {
        var currentFile = viewModel.FileName;
        try
        {
            var result = await StorageProvider.OpenFilePickerAsync(new()
            {
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("Mstat files") { Patterns = filterPatterns } },
                Title = "Open a file for analysis"
            });
            if (result.Any())
            {
                viewModel.FileName = result[0].TryGetLocalPath() is [.., '.', 'm' or 'M', 's' or 'S', 't' or 'T', 'a' or 'A', 't' or 'T'] path ? path : throw new InvalidOperationException("An invalid file has been selected.");
            }
        }
        catch (Exception ex)
        {
            await PromptErrorAsync(ex.Message);
            await Dispatcher.UIThread.InvokeAsync(() => viewModel.FileName = currentFile);
        }
    }

    private static async Task PromptErrorAsync(string message)
    {
        await new ContentDialog
        {
            CloseButtonText = "OK",
            Title = "Error",
            Content = message
        }.ShowAsync();
    }

    public async void Diff_Clicked(object? sender, RoutedEventArgs args)
    {
        var currentData = viewModel.CurrentData;
        if (currentData is null)
        {
            await PromptErrorAsync("You haven't open any file as the baseline.");
            return;
        }

        try
        {
            var result = await StorageProvider.OpenFilePickerAsync(new()
            {
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("Mstat files") { Patterns = filterPatterns } },
                Title = "Open a file for compare"
            });
            if (result.Any())
            {
                viewModel.Loading = true;
                using var mstaDataToCompare = await Task.Run(() => Task.FromResult(Read(result[0].TryGetLocalPath() ?? throw new InvalidOperationException("An invalid file has been selected."))))
                    .ContinueWith(t =>
                    {
                        viewModel.Loading = false;
                        return t.Result;
                    }, TaskScheduler.Default);
                await new DiffWindow(currentData, mstaDataToCompare).ShowDialog(this);
            }
        }
        catch (Exception ex)
        {
            await PromptErrorAsync(ex.Message);
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
                await PromptErrorAsync("Dependency graph information is only available in .NET 8 Preview 4 or later.");
                return;
            }

            var node = currentData.GetNodeForId(id.Value);
            if (node == null)
            {
                await PromptErrorAsync("Unable to load dependency graph. Was IlcGenerateDgmlFile=true specified?");
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