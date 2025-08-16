using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using AvaloniaReceiver.DataTemplates;

namespace AvaloniaReceiver.CustomControls;

public class FilterListBoxItem : StackPanel
{
    public static readonly StyledProperty<FilterDataTemplate> FilterDataProperty = 
        AvaloniaProperty.Register<FilterListBoxItem, FilterDataTemplate>(nameof(FilterData));

    public FilterDataTemplate FilterData
    {
        get => GetValue(FilterDataProperty);
        set
        {
            SetValue(FilterDataProperty, value);
            UpdateFilterParams();
        }
    }

    protected sealed override void OnInitialized()
    {
        base.OnInitialized();
        UpdateFilterParams();
    }

    private void UpdateFilterParams()
    {
        Console.WriteLine(" Adding filter params");
        var paramControls = FilterData.GetFilterParamsControls();
        Children.AddRange(paramControls);
    }

    protected override Type StyleKeyOverride => typeof(ListBoxItem);
    
    public FilterListBoxItem()
    {
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
    }

    private const string DraggedFilterInstanceIdKey = "DraggedFilterInstaceId";
    protected override async void OnPointerPressed(PointerPressedEventArgs e)
    {
        Console.WriteLine("aaa");
        base.OnPointerPressed(e);
        
        var dragData = new DataObject();
        dragData.Set(DraggedFilterInstanceIdKey, FilterData.FilterInstanceId);
        var result = await DragDrop.DoDragDrop(e, dragData, DragDropEffects.Move);
        Console.WriteLine($"Dnd result: {result}");
    }

    private void Drop(object? sender, DragEventArgs e)
    {
        var data = e.Data.Get(DraggedFilterInstanceIdKey);
        Console.WriteLine($"{data} Dropped on {FilterData.FilterInstanceId} | {Parent}");
        
        if (Parent is not ListBoxItem listBoxItem) return;
        if (listBoxItem.Parent is not ListBox listBox) return;
        
        if (listBox.ItemsSource is not ObservableCollection<FilterDataTemplate> items) return;

        var droppedAtMeItem = items.First(x => x.FilterInstanceId == (int)data!);
        
        var myIndex = items.IndexOf(FilterData);
        var droppedAtMeItemIndex = items.IndexOf(droppedAtMeItem);
        
        Console.WriteLine($"MyIndex {myIndex} | {droppedAtMeItemIndex}");

        var myData = FilterData;
        
        items.RemoveAt(myIndex);
        items.Insert(myIndex, droppedAtMeItem);
        items.RemoveAt(droppedAtMeItemIndex);
        items.Insert(droppedAtMeItemIndex, myData);

        OnListChanged();
        
        //OnPropertyChanged(nameof());
        
            //items[myIndex] = droppedAtMeItem;
            //items[droppedAtMeItemIndex] = this;
        
        Console.WriteLine(items);
        
        //ListChanged?.Invoke();
        
       // var data = e.Data.Get()
    }

    private void DragOver(object? sender, DragEventArgs e)
    {
        //Console.WriteLine("DragOver ");
    }

    public delegate void ListDirty();
    public static event ListDirty? ListChanged;

    private static void OnListChanged()
    {
        ListChanged?.Invoke();
    }
}