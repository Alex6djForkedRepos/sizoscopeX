using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace sizoscopeX.Core.Controls;

[PseudoClasses(pcAssembly, pcNamespace, pcClass, pcMethod, pcInstantiation, pcBlob, pcFrozenData, pcResource, pcField)]
public class NodeItemControl : TemplatedControl
{
    private const string pcAssembly = ":assembly";
    private const string pcNamespace = ":namespace";
    private const string pcClass = ":class";
    private const string pcMethod = ":method";
    private const string pcInstantiation = ":instantiation";
    private const string pcBlob = ":blob";
    private const string pcFrozenData = ":frozenData";
    private const string pcResource = ":resource";
    private const string pcField = ":field";

    public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<NodeItemControl, string?>(nameof(Text));
    public static readonly StyledProperty<NodeType?> TypeProperty = AvaloniaProperty.Register<NodeItemControl, NodeType?>(nameof(Type));
    public static readonly StyledProperty<IImage?> ImageProperty = AvaloniaProperty.Register<NodeItemControl, IImage?>(nameof(Image));

    private IImage? Image
    {
        get => GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public NodeType? Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    private void SetPseudoClasses(NodeType? type)
    {
        PseudoClasses.Set(pcAssembly, type is NodeType.Assembly);
        PseudoClasses.Set(pcNamespace, type is NodeType.Namespace);
        PseudoClasses.Set(pcClass, type is NodeType.Class);
        PseudoClasses.Set(pcMethod, type is NodeType.Method);
        PseudoClasses.Set(pcInstantiation, type is NodeType.Instantiation);
        PseudoClasses.Set(pcBlob, type is NodeType.Blob);
        PseudoClasses.Set(pcFrozenData, type is NodeType.FrozenData);
        PseudoClasses.Set(pcResource, type is NodeType.Resource);
        PseudoClasses.Set(pcField, type is NodeType.Field);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        SetPseudoClasses(Type);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TypeProperty)
        {
            SetPseudoClasses(change.GetNewValue<NodeType?>());
            InvalidateVisual();
        }
    }
}
