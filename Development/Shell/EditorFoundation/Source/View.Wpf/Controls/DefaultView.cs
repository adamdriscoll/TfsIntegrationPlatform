// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Provides a default view container.
    /// </summary>
    // This class is implemented entirely in code (no xaml) because there is currently a
    // compiler and/or runtime constraint that disallows inheritance of classes that use xaml.
    [ContentProperty ("Content")]
    public partial class DefaultView : DockPanel
    {
        #region Fields
        private readonly DefaultMenu defaultMenu;
        private readonly DefaultToolBar defaultToolBar;
        private readonly DefaultStatusBar defaultStatusBar;
        private readonly ContentControl viewContent;
        #endregion

        #region Properties
        /// <summary>
        /// Identifies the Content dependency property.
        /// </summary>
        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register ("Content", typeof (object), typeof (DefaultView));
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultView"/> class.
        /// </summary>
        public DefaultView ()
        {
            this.defaultMenu = new DefaultMenu ();
            this.defaultToolBar = new DefaultToolBar ();
            this.defaultStatusBar = new DefaultStatusBar ();
            this.viewContent = new ContentControl ();

            // Bind the content property to the content control's content property
            Binding contentBinding = new Binding (DefaultView.ContentProperty.Name);
            contentBinding.Source = this;
            BindingOperations.SetBinding (this.viewContent, ContentControl.ContentProperty, contentBinding);

            //this.Children.Add (this.defaultToolBar);
            //this.Children.Add (this.defaultStatusBar);
            this.Children.Add (this.viewContent);
        }
        #endregion

        /// <summary>
        /// Gets the default menu.
        /// </summary>
        public DefaultMenu DefaultMenu
        {
            get
            {
                return this.defaultMenu;
            }
        }

        /// <summary>
        /// Gets the default tool bar.
        /// </summary>
        public DefaultToolBar DefaultToolBar
        {
            get
            {
                return this.defaultToolBar;
            }
        }

        /// <summary>
        /// Gets the default status bar.
        /// </summary>
        public DefaultStatusBar DefaultStatusBar
        {
            get
            {
                return this.defaultStatusBar;
            }
        }

        /// <summary>
        /// Gets or sets the content of the view.
        /// </summary>
        public object Content
        {
            get
            {
                return (object)this.GetValue (DefaultView.ContentProperty);
            }
            set
            {
                this.SetValue (DefaultView.ContentProperty, value);
            }
        }
    }
}
