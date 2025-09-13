using Avalonia.Controls;
using sizoscopeX.Core.ViewModels;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Controls;
using Avalonia.Input;
using static MstatData;

namespace sizoscopeX.Core;

public partial class MainView : UserControl
{
    private readonly MainViewModel viewModel = new();
    private static readonly string[] filterPatterns = new[] { "*.mstat", "*.scan.dgml.xml" };
    private const string repoUrl = "https://github.com/hez2010/sizoscopeX";

    public MainView()
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
        try
        {
            e.DragEffects &= DragDropEffects.Copy;
            if (e.Data.Contains(DataFormats.Files))
            {
                var files = e.Data.GetFiles()?.OfType<IStorageFile>().ToArray();
                if (files is not null)
                {
                    viewModel.Loading = true;
                    await LoadForBaseAsync(files);
                }
            }
        }
        catch (Exception ex)
        {
            await PromptErrorAsync(ex.Message);
        }
        finally
        {
            viewModel.Loading = false;
        }
    }

    public async void Open_Clicked(object? sender, RoutedEventArgs args)
    {
        try
        {
            var result = await StorageProvider.OpenFilePickerAsync(new()
            {
                AllowMultiple = true,
                FileTypeFilter = new[] { new FilePickerFileType("Mstat and dgml files") { Patterns = filterPatterns } },
                Title = "Open to analyze"
            });
            if (result.Any())
            {
                viewModel.Loading = true;
                await LoadForBaseAsync(result);
            }
        }
        catch (Exception ex)
        {
            await PromptErrorAsync(ex.Message);
        }
        finally
        {
            viewModel.Loading = false;
        }
    }

    public async Task OpenFromPathAsync(string mstatFile, string? mstatFileForDiff = null)
    {
        try
        {
            viewModel.Loading = true;
            var result = await StorageProvider.TryGetFileFromPathAsync(mstatFile);
            if (result is not null)
            {
                await LoadForBaseAsync(new[] { result });
            }

            if (mstatFileForDiff is not null)
            {
                result = await StorageProvider.TryGetFileFromPathAsync(mstatFileForDiff);
                if (result is not null)
                {
                    await LoadForDiffAsync(viewModel.CurrentData!, new[] { result });
                }
            }
        }
        catch (Exception ex)
        {
            await PromptErrorAsync(ex.Message);
        }
        finally
        {
            viewModel.Loading = false;
        }
    }

    private IStorageProvider StorageProvider => TopLevel.GetTopLevel(this)?.StorageProvider ?? throw new InvalidOperationException("Invalid owner.");

    private async Task PromptErrorAsync(string message)
    {
        await new ContentDialog
        {
            CloseButtonText = "OK",
            Title = "Error",
            Content = message
        }.ShowAsync(TopLevel.GetTopLevel(this));
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
                AllowMultiple = true,
                FileTypeFilter = new[] { new FilePickerFileType("Mstat and dgml files") { Patterns = filterPatterns } },
                Title = "Select to compare"
            });
            if (result.Any())
            {
                viewModel.Loading = true;
                await LoadForDiffAsync(currentData, result);
            }
        }
        catch (Exception ex)
        {
            await PromptErrorAsync(ex.Message);
        }
        finally
        {
            viewModel.Loading = false;
        }
    }

    private async Task LoadForBaseAsync(IReadOnlyList<IStorageFile> result)
    {
        var (mstatStream, dmglStream) = await ReadStatFilesAsync(result);
        await viewModel.LoadDataAsync(mstatStream, dmglStream);
    }

    private async Task LoadForDiffAsync(MstatData currentData, IReadOnlyList<IStorageFile> files)
    {
        var (mstatStream, dmglStream) = await ReadStatFilesAsync(files);
        var mstaDataToCompare = await Utils.TaskRunIfPossible(() => Read(mstatStream, dmglStream));
        var view = new DiffView(currentData, mstaDataToCompare);
        Utils.ShowWindow(view);
    }

    public void Refresh_Clicked(object? sender, RoutedEventArgs args)
    {
        viewModel.Refresh();
    }

    public void Exit_Clicked(object? sender, RoutedEventArgs args)
    {
        Environment.Exit(0);
    }

    private async void Item_DoubleTapped(object? sender, TappedEventArgs args)
    {
        object? tag = null;
        if (sender is TreeView treeView && treeView.SelectedItem is TreeNode tn) tag = tn.Tag;
        if (sender is ListBox listBox && listBox.SelectedItem is SearchResultItem sri) tag = sri.Tag;

        var currentData = viewModel.CurrentData;
        if (currentData is null) return;

        int? id = tag switch
        {
            MstatTypeDefinition typedef => typedef.NodeId,
            MstatTypeSpecification typespec => typespec.NodeId,
            MstatMemberDefinition memberdef => memberdef.NodeId,
            MstatMethodSpecification methodspec => methodspec.NodeId,
            MstatFrozenObject frozenObject => frozenObject.NodeId,
            int nodeId => nodeId,
            _ => null
        };

        if (id.HasValue)
        {
            try
            {
                if (id.Value < 0)
                {
                    await PromptErrorAsync("This node was not used directly and is included for display purposes only. Try analyzing sub nodes.");
                    return;
                }

                if (!currentData.DgmlSupported)
                {
                    await PromptErrorAsync("Dependency graph information is only available in .NET 8 or later.");
                    return;
                }

                if (!currentData.DgmlAvailable || currentData.GetNodeForId(id.Value, out _) is not Node node)
                {
                    await PromptErrorAsync("Unable to load dependency graph. Was IlcGenerateDgmlFile=true specified?");
                    return;
                }

                var view = new RootView(node);
                Utils.ShowWindow(view);
            }
            catch (Exception ex)
            {
                await PromptErrorAsync(ex.Message);
                return;
            }
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
                       
                       License notice for TurboXml

                                                  Copyright (c) 2024, Alexandre Mutel
                       All rights reserved.

                       Redistribution and use in source and binary forms, with or without modification
                       , are permitted provided that the following conditions are met:

                       1. Redistributions of source code must retain the above copyright notice, this 
                          list of conditions and the following disclaimer.

                       2. Redistributions in binary form must reproduce the above copyright notice, 
                          this list of conditions and the following disclaimer in the documentation 
                          and/or other materials provided with the distribution.

                       THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND 
                       ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED 
                       WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
                       DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
                       FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL 
                       DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR 
                       SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER 
                       CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
                       OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
                       OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

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
        await dialog.ShowAsync(TopLevel.GetTopLevel(this));
    }

    public async void About_Clicked(object? sender, RoutedEventArgs args)
    {
        var dialog = new ContentDialog
        {
            CloseButtonText = "OK",
            Title = "About",
            Content = $"""
                       sizoscopeX - {typeof(MainView).Assembly?.GetName().Version}

                       Copyright (c) 2024 Michal Strehovsky
                       Copyright (c) 2024 hez2010

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
        await dialog.ShowAsync(TopLevel.GetTopLevel(this));
    }

    public async void Source_Clicked(object? sender, RoutedEventArgs args)
    {
        if (TopLevel.GetTopLevel(this) is TopLevel topLevel)
        {
            if (!await topLevel.Launcher.LaunchUriAsync(new Uri(repoUrl)))
            {
                await PromptErrorAsync($"Failed to open the source repository '{repoUrl}'.");
            }
        }
    }

    private async Task<(MemoryStream mstatStream, MemoryStream? dmglStream)> ReadStatFilesAsync(IReadOnlyList<IStorageItem> result)
    {
        var dmgl = result.FirstOrDefault(i => i is IStorageFile
        {
            Name:
            [
                .., '.', 's' or 'S', 'c' or 'C', 'a' or 'A', 'n' or 'N', '.', 'd' or 'D', 'g' or 'G', 'm' or 'M',
                'l' or 'L', '.', 'x' or 'X', 'm' or 'M', 'l' or 'L'
            ]
        }) as IStorageFile;
        if (result.FirstOrDefault(i => i is IStorageFile
        {
            Name: [.., '.', 'm' or 'M', 's' or 'S', 't' or 'T', 'a' or 'A', 't' or 'T']
        }) is not IStorageFile mstat)
        {
            throw new InvalidOperationException("An invalid file has been dropped.");
        }

        if (dmgl is null)
        {
            if (mstat.TryGetLocalPath() is string path)
            {
                dmgl = await StorageProvider.TryGetFileFromPathAsync(Path.ChangeExtension(path, "scan.dgml.xml"));
            }
        }

        MemoryStream? mstatStream = null, dmglStream = null;
        await using var mstatFileStream = await mstat.OpenReadAsync();
        await mstatFileStream.CopyToAsync(mstatStream = new MemoryStream());

        if (dmgl is not null)
        {
            await using var dmglFileStream = await dmgl.OpenReadAsync();
            await dmglFileStream.CopyToAsync(dmglStream = new MemoryStream());
        }

        return (mstatStream, dmglStream);
    }
}