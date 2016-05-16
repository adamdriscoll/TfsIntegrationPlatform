// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Provides a means by which arbitrary dependency properties can be bound to the CanExecute status of an arbitrary command.
    /// </summary>
    /// <example>
    /// <![CDATA[
    /// <Label Content="Test Label 2" Visibility="{Binding Source={StaticResource CopyCommandStatus},Path=CanExecute},Converter={StaticResource BooleanToVisibilityConverter}">
    ///   <Label.Resources>
    ///     <local:CommandStatus x:Key="CopyCommandStatus" Command="ApplicationCommands.Copy" />
    ///     <BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter" />
    ///   </Label.Resources>
    /// </Label>
    /// ]]>
    /// </example>
    public class CommandStatus : DependencyObject, ICommandSource, INotifyPropertyChanged
    {
        #region Fields
        // Note: Because of the CommandManager's use of weak event handlers,
        // we need to keep explicit references to the event handler.
        private EventHandler canExecuteChangedEventHandler;
        #endregion

        #region Events
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Dependency Properties
        /// <summary>
        /// Identifies the <see cref="CommandStatus.Command" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register ("Command", typeof (ICommand), typeof (CommandStatus), new PropertyMetadata (CommandStatus.OnCommandChanged));

        /// <summary>
        /// Identifies the <see cref="CommandStatus.CommandParameter" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register ("CommandParameter", typeof (object), typeof (CommandStatus), new PropertyMetadata (CommandStatus.OnCommandParameterChanged));

        /// <summary>
        /// Identifies the <see cref="CommandStatus.CommandTarget" /> dependency property. 
        /// </summary>
        public static readonly DependencyProperty CommandTargetProperty =
            DependencyProperty.Register ("CommandTarget", typeof (IInputElement), typeof (CommandStatus), new PropertyMetadata (CommandStatus.OnCommandTargetChanged));
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the <see cref="System.Windows.Input.ICommand" /> that will be executed when the command source is invoked.
        /// </summary>
        public ICommand Command
        {
            get
            {
                return (ICommand)this.GetValue (CommandProperty);
            }
            set
            {
                this.SetValue (CommandProperty, value);
            }
        }
        
        /// <summary>
        /// Represents a user defined data value that can be passed to the command when it is executed.
        /// </summary>
        /// <returns>The command specific data.</returns>
        public object CommandParameter
        {
            get
            {
                return (object)this.GetValue (CommandParameterProperty);
            }
            set
            {
                this.SetValue (CommandParameterProperty, value);
            }
        }

        /// <summary>
        /// The <see cref="System.Windows.IInputElement" /> on which the command is being executed.
        /// </summary>
        public IInputElement CommandTarget
        {
            get
            {
                return (IInputElement)this.GetValue (CommandTargetProperty);
            }
            set
            {
                this.SetValue (CommandTargetProperty, value);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="CommandStatus.Command" /> can be executed.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the command can execute; otherwise, <c>false</c>.
        /// </value>
        public bool CanExecute
        {
            get
            {
                if (this.Command == null)
                {
                    return false;
                }
                else
                {
                    RoutedCommand routedCommand = this.Command as RoutedCommand;
                    if (routedCommand != null && this.CommandTarget != null)
                    {
                        return routedCommand.CanExecute (this.CommandParameter, this.CommandTarget);
                    }
                    else
                    {
                        return this.Command.CanExecute (this.CommandParameter);
                    }
                }
            }
        }
        #endregion

        #region Private Methods
        private static void OnCommandChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CommandStatus commandStatus = d as CommandStatus;
            if (commandStatus != null)
            {
                ICommand oldCommand = e.OldValue as ICommand;
                if (oldCommand != null)
                {
                    oldCommand.CanExecuteChanged -= commandStatus.canExecuteChangedEventHandler;
                    commandStatus.canExecuteChangedEventHandler = null;
                }

                ICommand newCommand = e.NewValue as ICommand;
                if (newCommand != null)
                {
                    commandStatus.canExecuteChangedEventHandler = delegate { commandStatus.RaiseCanExecutePropertyChangedEvent (); };
                    newCommand.CanExecuteChanged += commandStatus.canExecuteChangedEventHandler;
                }

                // The command may have changed and therefore the CanExecute status may have changed
                commandStatus.RaiseCanExecutePropertyChangedEvent ();
            }
        }

        private static void OnCommandParameterChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CommandStatus commandStatus = d as CommandStatus;
            if (commandStatus != null && commandStatus.Command != null)
            {
                commandStatus.RaiseCanExecutePropertyChangedEvent ();
            }
        }

        private static void OnCommandTargetChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CommandStatus commandStatus = d as CommandStatus;
            if (commandStatus != null && commandStatus.Command is RoutedCommand)
            {
                commandStatus.RaiseCanExecutePropertyChangedEvent ();
            }
        }

        private void RaiseCanExecutePropertyChangedEvent ()
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged (this, new PropertyChangedEventArgs ("CanExecute"));
            }
        }
        #endregion
    }
}
