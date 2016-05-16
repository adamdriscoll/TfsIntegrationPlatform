// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.TeamFoundation.Migration.Toolkit;

namespace Microsoft.TeamFoundation.Migration.Shell.ConflictManagement
{
   public static class ConflictHelpProvider
   {
       public static readonly DependencyProperty HelpKeywordProperty =
           DependencyProperty.RegisterAttached("HelpKeyword", typeof (string), typeof (ConflictHelpProvider));

       public static readonly DependencyProperty HelpPathProperty =
           DependencyProperty.RegisterAttached("HelpPath", typeof (string), typeof (ConflictHelpProvider));

       static ConflictHelpProvider()
       {
           CommandManager.RegisterClassCommandBinding(
               typeof (FrameworkElement),
               new CommandBinding(ApplicationCommands.Help, OnHelpExecuted, OnHelpCanExecute));
           HelpPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName), 
                                   @"Documentation\TfsIntegration.chm");
       }

       [SuppressMessage("Microsoft.Naming","CA1721:PropertyNamesShouldNotMatchGetMethods")]
       public static string HelpPath
       {
           get;
           set;
       }

       public static string HelpKey
       {
           get;
           set;
       }

       public static string GetHelpKeyword(DependencyObject obj)
       {
           return (string) obj.GetValue(HelpKeywordProperty);
       }

       public static void SetHelpKeyword(DependencyObject obj, string value)
       {
           obj.SetValue(HelpKeywordProperty, value);
       }

       public static void SetHelpKeyword(DependencyObject obj, ConflictType type)
       {
            obj.SetValue(HelpKeywordProperty, type.HelpKeyword);
       }

       public static string GetHelpPath(DependencyObject obj)
       {
           return (string) obj.GetValue(HelpPathProperty);
       }

       public static void SetHelpPath(DependencyObject obj, string value)
       {
           obj.SetValue(HelpPathProperty, value);
       }

       private static void OnHelpCanExecute(object sender, CanExecuteRoutedEventArgs e)
       {
           e.CanExecute = CanExecuteHelp((DependencyObject) sender);
       }

       private static bool CanExecuteHelp(DependencyObject sender)
       {
           if (sender != null)
           {
               if (GetKeywordElement(sender) != null)
               {
                   return true;
               }
           }

           return false;
       }

       private static DependencyObject GetKeywordElement(DependencyObject sender)
       {
           if (sender != null)
           {
               if (!string.IsNullOrEmpty(GetHelpKeyword(sender)))
               {
                   return sender;
               }

               return GetKeywordElement(VisualTreeHelper.GetParent(sender));
           }

           return null;
       }

       private static string GetEffectiveHelpPath(DependencyObject ctl)
       {
           DependencyObject item = ctl;
           string helpPath = null;

           do
           {
               helpPath = GetHelpPath(item);
           }
           while (String.IsNullOrEmpty(helpPath) && (item = VisualTreeHelper.GetParent(item)) != null);

           // Return the first configured help path in the hierarchy we could find, if none is defined,
           // return the potentially configured global help path...
           return (!String.IsNullOrEmpty(helpPath))? helpPath : HelpPath;
       }

       private static void OnHelpExecuted(object sender, ExecutedRoutedEventArgs e)
       {
           if (!e.Handled)
           {
               try
               {
                   DependencyObject ctl = GetKeywordElement(e.Source as DependencyObject);

                   if (ctl != null)
                   {
                       // Keyword is not null or empty... 
                       string keyword = GetHelpKeyword(ctl);
                       string helpPath = GetEffectiveHelpPath(ctl);

                       if (!String.IsNullOrEmpty(helpPath))
                       {
                            TraceHelp(keyword, helpPath);
                            ShowHelp(helpPath, keyword);
                            e.Handled = true;
                       }
                       else
                       {
                           // Will help catch scenarios where for some reason we have not defined the help path...
                           Debug.Fail(String.Format(CultureInfo.CurrentCulture, "Could not find help path associated with keyword '{0}'", keyword));
                       }
                   }
               }
               catch (Exception ex)
               {
                   Utilities.HandleException(ex, false, "Error", String.Format("Error in opening help file : {0}",
                       ex.Message));
               }
           }
       }

       private static void TraceHelp(string keyword, string helpFileUri)
       {
           Trace.WriteLine("???????????????????????????????????????????????????????????????????");
           Trace.WriteLine("?????? Help Keyword : " + keyword);
           Trace.WriteLine("?????? Help File Path : " + helpFileUri);
           Trace.WriteLine("???????????????????????????????????????????????????????????????????");
       }

       [StructLayout(LayoutKind.Sequential)]
       private struct HH_AKLINK
       {
           public int cbStruct;     // sizeof this structure
           [MarshalAs(UnmanagedType.Bool)]
           public bool fReserved;    // must be FALSE (really!)
           [MarshalAs(UnmanagedType.LPStr)]
           public String pszKeywords;  // semi-colon separated keywords
           [MarshalAs(UnmanagedType.LPStr)]
           public String pszUrl;       // URL to jump to if no keywords found (may be NULL)
           [MarshalAs(UnmanagedType.LPStr)]
           public String pszMsgText;   // Message text to display in MessageBox if pszUrl is NULL and no keyword match
           [MarshalAs(UnmanagedType.LPStr)]
           public String pszMsgTitle;  // Message text to display in MessageBox if pszUrl is NULL and no keyword match
           [MarshalAs(UnmanagedType.LPStr)]
           public String pszWindow;    // Window to display URL in
           [MarshalAs(UnmanagedType.Bool)]
           public bool fIndexOnFail; // Displays index if keyword lookup fails.
       };

       const int HH_ALINK_LOOKUP = 0x0013;  // ALink version of HH_KEYWORD_LOOKUP [Use HtmlHelp_AKLookup()]

       // This overload is for passing an HH_
       [DllImport("hhctrl.ocx", CharSet = CharSet.Unicode, EntryPoint = "HtmlHelpA", BestFitMapping=false, ThrowOnUnmappableChar=true)]
       private static  extern int HtmlHelp_AKLookup_Helper(
           int caller,
           [MarshalAs(UnmanagedType.LPStr)]
			String file,
           uint command,
           ref HH_AKLINK akl
           );

       private static int HtmlHelp_AlinkLookup(
           int caller,
           String file,
           ref HH_AKLINK akl
           )
       {
           akl.cbStruct = Marshal.SizeOf(akl);

           return HtmlHelp_AKLookup_Helper(
               caller,
               file,
               HH_ALINK_LOOKUP,
               ref akl);
       }

       // location of default pages in  chm files.  The page is displayed 
       // if we try to show help for a keyword that doesn't exist.
       private static readonly String DefaultPage = @"html\1f3b7d4c-bbe5-4555-a9d2-a35ea6367e48.htm";

       private static void ShowHelp(string helpPath, string keyword)
       {
           // this is a default page to display when the keyword can't be found
           // the GUIDs map to specific pages in the CHM files, which were 
           // selected by UE to be the fallback pages.
           String defaultPage = DefaultPage;

           HH_AKLINK akLink = new HH_AKLINK();
           akLink.pszKeywords = keyword;
           akLink.fReserved = false;
           akLink.pszUrl = defaultPage;
           akLink.pszMsgText = null;
           akLink.pszMsgTitle = string.Empty;
           akLink.pszWindow = string.Empty;
           akLink.fIndexOnFail = false;
   
           try
           {
               HtmlHelp_AlinkLookup(0, helpPath, ref akLink);
           }
           catch (Exception ex)
           {
               Utilities.HandleException(ex, false, "Error", String.Format("Error in opening help file : {0}",
                   ex.Message));
           }
           
       }
   }
}
