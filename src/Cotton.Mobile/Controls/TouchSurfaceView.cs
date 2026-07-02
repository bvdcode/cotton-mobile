// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

using System.Windows.Input;
using Cotton.Mobile.Behaviors;

namespace Cotton.Mobile.Controls
{
    public class TouchSurfaceView : Border
    {
        private const string DefaultSurfaceStyleResourceKey = "M3ListItemTouchSurface";

        public static readonly BindableProperty CommandProperty = BindableProperty.Create(
            nameof(Command),
            typeof(ICommand),
            typeof(TouchSurfaceView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
            nameof(CommandParameter),
            typeof(object),
            typeof(TouchSurfaceView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TapCommandProperty = BindableProperty.Create(
            nameof(TapCommand),
            typeof(ICommand),
            typeof(TouchSurfaceView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty TapCommandParameterProperty = BindableProperty.Create(
            nameof(TapCommandParameter),
            typeof(object),
            typeof(TouchSurfaceView),
            propertyChanged: OnVisualPropertyChanged);

        public static readonly BindableProperty SurfaceStyleResourceKeyProperty = BindableProperty.Create(
            nameof(SurfaceStyleResourceKey),
            typeof(string),
            typeof(TouchSurfaceView),
            DefaultSurfaceStyleResourceKey,
            propertyChanged: OnVisualPropertyChanged);

        private readonly LongPressBehavior _longPressBehavior;

        public TouchSurfaceView()
        {
            _longPressBehavior = new LongPressBehavior();
            Behaviors.Add(_longPressBehavior);
            UpdateVisualState();
        }

        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public ICommand? TapCommand
        {
            get => (ICommand?)GetValue(TapCommandProperty);
            set => SetValue(TapCommandProperty, value);
        }

        public object? TapCommandParameter
        {
            get => GetValue(TapCommandParameterProperty);
            set => SetValue(TapCommandParameterProperty, value);
        }

        public string SurfaceStyleResourceKey
        {
            get => (string)GetValue(SurfaceStyleResourceKeyProperty);
            set => SetValue(SurfaceStyleResourceKeyProperty, value);
        }

        private static void OnVisualPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            TouchSurfaceView view = (TouchSurfaceView)bindable;
            view.UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            string surfaceStyleResourceKey = MaterialResources.ResolveStyleResourceKey(
                SurfaceStyleResourceKey,
                DefaultSurfaceStyleResourceKey);

            SetDynamicResource(StyleProperty, surfaceStyleResourceKey);
            _longPressBehavior.Command = Command;
            _longPressBehavior.CommandParameter = CommandParameter;
            _longPressBehavior.TapCommand = TapCommand;
            _longPressBehavior.TapCommandParameter = TapCommandParameter;
        }
    }
}
