// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Windows;
using System.Windows.Input;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Represents a binding between a <see cref="RoutedEvent"/> and an <see cref="ICommand"/>, such as a <see cref="RoutedCommand"/>.
    /// </summary>
    public class EventBinding : DependencyObject, ICommandSource
    {
        #region Dependency Properties
        private static readonly DependencyPropertyKey SourcePropertyKey = DependencyProperty.RegisterReadOnly ("Source", typeof (UIElement), typeof (EventBinding), null);

        /// <summary>
        /// Identifies the <see cref="Source"/> property.
        /// </summary>
        public static readonly DependencyProperty SourceProperty = EventBinding.SourcePropertyKey.DependencyProperty;

        /// <summary>
        /// Identifies the <see cref="RoutedEvent"/> property.
        /// </summary>
        public static readonly DependencyProperty RoutedEventProperty = DependencyProperty.Register ("RoutedEvent", typeof (RoutedEvent), typeof (EventBinding), new PropertyMetadata (EventBinding.OnRoutedEventChanged));

        /// <summary>
        /// Identifies the <see cref="Command"/> property.
        /// </summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register ("Command", typeof (ICommand), typeof (EventBinding));

        /// <summary>
        /// Identifies the <see cref="CommandParameter"/> property.
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register ("CommandParameter", typeof (object), typeof (EventBinding));

        /// <summary>
        /// Identifies the <see cref="CommandTarget"/> property.
        /// </summary>
        public static readonly DependencyProperty CommandTargetProperty = DependencyProperty.Register ("CommandTarget", typeof (IInputElement), typeof (EventBinding));
        #endregion

        #region Properties
        /// <summary>
        /// Gets the source <see cref="UIElement"/>.
        /// </summary>
        public UIElement Source
        {
            get
            {
                return (UIElement)this.GetValue (EventBinding.SourceProperty);
            }
            protected internal set
            {
                if (value != this.Source)
                {
                    // Remove the old event handler
                    if (this.Source != null && this.RoutedEvent != null)
                    {
                        this.Source.RemoveHandler (this.RoutedEvent, new RoutedEventHandler (this.OnEventRaised));
                    }

                    this.SetValue (EventBinding.SourcePropertyKey, value);

                    // Add the new event handler
                    if (this.Source != null && this.RoutedEvent != null)
                    {
                        this.Source.AddHandler (this.RoutedEvent, new RoutedEventHandler (this.OnEventRaised));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="RoutedEvent"/> that will trigger the <see cref="ICommand"/>.
        /// </summary>
        public RoutedEvent RoutedEvent
        {
            get
            {
                return (RoutedEvent)this.GetValue (EventBinding.RoutedEventProperty);
            }
            set
            {
                this.SetValue (EventBinding.RoutedEventProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="ICommand"/> that will be executed when the command source is invoked.
        /// </summary>
        public ICommand Command
        {
            get
            {
                return (ICommand)this.GetValue (EventBinding.CommandProperty);
            }
            set
            {
                this.SetValue (EventBinding.CommandProperty, value);
            }
        }

        /// <summary>
        /// Represents a user defined data value that can be passed to the <see cref="ICommand"/> when it is executed.
        /// </summary>
        /// <value></value>
        /// <returns>The command specific data.</returns>
        public object CommandParameter
        {
            get
            {
                return (object)this.GetValue (EventBinding.CommandParameterProperty);
            }
            set
            {
                this.SetValue (EventBinding.CommandParameterProperty, value);
            }
        }

        /// <summary>
        /// The object that the <see cref="ICommand"/> is being executed on.
        /// </summary>
        /// <remarks>
        /// This property is ignored unless the command is a <see cref="RoutedCommand"/>.
        /// </remarks>
        public IInputElement CommandTarget
        {
            get
            {
                return (IInputElement)this.GetValue (EventBinding.CommandTargetProperty);
            }
            set
            {
                this.SetValue (EventBinding.CommandTargetProperty, value);
            }
        }
        #endregion

        #region Private Methods
        private static void OnRoutedEventChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != e.NewValue)
            {
                EventBinding eventBinding = d as EventBinding;
                if (eventBinding != null && eventBinding.Source != null)
                {
                    RoutedEvent oldEvent = e.OldValue as RoutedEvent;
                    RoutedEvent newEvent = e.NewValue as RoutedEvent;

                    // Remove the old event handler
                    if (oldEvent != null)
                    {
                        eventBinding.Source.RemoveHandler (oldEvent, new RoutedEventHandler (eventBinding.OnEventRaised));
                    }

                    // Add the new event handler
                    if (newEvent != null)
                    {
                        eventBinding.Source.AddHandler (newEvent, new RoutedEventHandler (eventBinding.OnEventRaised));
                    }
                }
            }
        }

        private void OnEventRaised (object sender, RoutedEventArgs e)
        {
            if (this.Command != null)
            {
                // Get the command as a routed command
                RoutedCommand routedCommand = this.Command as RoutedCommand;

                // If the command is a routed command and we have a command target,
                // execute the routed command on the command target
                if (routedCommand != null && this.CommandTarget != null)
                {
                    routedCommand.Execute (this.CommandParameter, this.CommandTarget);
                }
                // Otherwise just execute the command normally.
                else
                {
                    this.Command.Execute (this.CommandParameter);
                }
            }
        }
        #endregion
    }
}
