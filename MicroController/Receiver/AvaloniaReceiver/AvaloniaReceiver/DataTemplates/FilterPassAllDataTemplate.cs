using Avalonia.Markup.Xaml.Templates;
using AvaloniaReceiver.Filters;
using JetBrains.Annotations;

namespace AvaloniaReceiver.DataTemplates;

[method: UsedImplicitly]
public class FilterPassAllDataTemplate(Router.Router router) : FilterDataTemplate("Pass All", _ => { }, router)
{
    private PassAllFilter<(double, double)> Filter => _filter ??= new PassAllFilter<(double, double)>();
    private PassAllFilter<(double, double)>? _filter;
    
    public override IFilter GetFilter()
    {
        return Filter;
    }
}