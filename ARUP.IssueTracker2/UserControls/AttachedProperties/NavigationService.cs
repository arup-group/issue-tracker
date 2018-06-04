using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ARUP.IssueTracker.UserControls.AttachedProperties
{
  //Copied from http://stackoverflow.com/questions/861409/wpf-making-hyperlinks-clickable
  //and from:https://github.com/teocomi/BCFier/blob/e6413d567595a121fa583de5b727bb906881ed7c/Bcfier/Data/AttachedProperties/NavigationService.cs
  /// <summary>
  /// Renders the text of the comments as clickable links if detects URLs or local PATHs
  /// </summary>
  public static class NavigationService
  {
    //URL
    // (?#Protocol)(?:(?:ht|f)tp(?:s?)\:\/\/|~/|/)?(?#Username:Password)(?:\w+:\w+@)?(?#Subdomains)(?:(?:[-\w]+\.)+(?#TopLevel Domains)(?:com|org|net|gov|mil|biz|info|mobi|name|aero|jobs|museum|travel|[a-z]{2}))(?#Port)(?::[\d]{1,5})?(?#Directories)(?:(?:(?:/(?:[-\w~!$+|.,=]|%[a-f\d]{2})+)+|/)+|\?|#)?(?#Query)(?:(?:\?(?:[-\w~!$+|.,*:]|%[a-f\d{2}])+=(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)(?:&(?:[-\w~!$+|.,*:]|%[a-f\d{2}])+=(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)*)*(?#Anchor)(?:#(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)?
    //PATH
    //((\\\\[a-zA-Z0-9-]+\\[a-zA-Z0-9`~!@#$%^&(){}'._-]+([ ]+[a-zA-Z0-9`~!@#$%^&(){}'._-]+)*)|([a-zA-Z]:))(\\[^ \\/:*?""<>|]+([ ]+[^ \\/:*?""<>|]+)*)*\\?

    //original URL regex
    // private static string regexUrl = @"(?<url>(?:(?:ht|f)tp(?:s?)\:\/\/|~/|/)(?#Subdomains)(?:(?:[-\w]+\.)+(?#TopLevel Domains)(?:com|org|net|gov|mil|biz|info|mobi|name|aero|jobs|museum|travel|[a-z]{2}))(?#Port)(?::[\d]{1,5})?(?#Directories)(?:(?:(?:/(?:[-\w~!$+|.,=]|%[a-f\d]{2})+)+|/)+|\?|#)?(?#Query)(?:(?:\?(?:[-\w~!$+|.,*:]|%[a-f\d{2}])+=(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)(?:&(?:[-\w~!$+|.,*:]|%[a-f\d{2}])+=(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)*)*(?#Anchor)(?:#(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)?)|(?<path>\[(((\\\\[a-zA-Z0-9-]+\\[a-zA-Z0-9`~!@#$%^&(){}'._-]+([ ]+[a-zA-Z0-9`~!@#$%^&(){}'._-]+)*)|([a-zA-Z]:))(\\[^ \\/:*?""<>|]+([ ]+[^ \\/:*?""<>|]+)*)*\\?)\])";
    //added pw support, only for urls without spaces
    private static string regexUrl = @"(?<url>(?:(?:http|ftp|pw)(?:s?)\:\/\/|~/|/)(?#Subdomains)(?:(?:[-\w]+\.)+(?#TopLevel Domains)(?:com|org|net|gov|mil|biz|info|mobi|name|aero|jobs|museum|travel|[a-z]{2}))(?#Port)(?::[a-zA-Z0-9_.-]+)?(?#Directories)(?:(?:(?:/(?:[-\w~!$+|.,&;=]|%[a-f\d]{2})+)+|/)+|\?|#)?(?#Query)(?:(?:\?(?:[-\w~!$+|.,*:]|%[a-f\d{2}])+=(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)(?:&(?:[-\w~!$+|.,*:]|%[a-f\d{2}])+=(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)*)*(?#Anchor)(?:#(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)?)|(?<path>\[(((\\\\[a-zA-Z0-9-]+\\[a-zA-Z0-9`~!@#$%^&(){}'._-]+([ ]+[a-zA-Z0-9`~!@#$%^&(){}'._-]+)*)|([a-zA-Z]:))(\\[^ \\/:*?""<>|]+([ ]+[^ \\/:*?""<>|]+)*)*\\?)\])";
    private static string regexPath = @"(?:[a-zA-Z]\:(\\|\/)|file\:\/\/|\\\\|\.(\/|\\))([^\\\/\:\*\?\<\>\""\|]+(\\|\/){0,1})+";

    private static readonly Regex regexUrlPath = new Regex(regexUrl + "|" + regexPath);


    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text",
        typeof(string),
        typeof(NavigationService),
        new PropertyMetadata(null, OnTextChanged)
    );

    public static string GetText(DependencyObject d)
    { return d.GetValue(TextProperty) as string; }

    public static void SetText(DependencyObject d, string value)
    { d.SetValue(TextProperty, value); }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      try
      {
        var tb = d as TextBlock;
        if (tb != null)
          TextBlockChanged(tb, (string)e.NewValue);

      }
      catch (System.Exception ex1)
      {
        MessageBox.Show("exception: " + ex1);
      }
    }

    private static void TextBlockChanged(TextBlock text_block, string new_text)
    {
      try
      {
        text_block.Inlines.Clear();

        if (string.IsNullOrEmpty(new_text))
          return;

        // Find all URLs using a regular expression
        int last_pos = 0;
        foreach (Match match in regexUrlPath.Matches(new_text))
        {
          var matchString = match.Value.Replace("[", "").Replace("]", "");
          Uri url = null;
          Uri.TryCreate(matchString, UriKind.RelativeOrAbsolute, out url);
          
          // Copy raw string from the last position up to the match
          if (match.Index != last_pos)
          {
            var raw_text = new_text.Substring(last_pos, match.Index - last_pos);
            text_block.Inlines.Add(new Run(raw_text));
          }

          // Create a hyperlink for the match
          var link = new Hyperlink(new Run(matchString))
          {
            NavigateUri = url
          };
          link.Click += OnUrlClick;

          text_block.Inlines.Add(link);

          // Update the last matched position
          last_pos = match.Index + match.Length;
        }

        // Finally, copy the remainder of the string
        if (last_pos < new_text.Length)
          text_block.Inlines.Add(new Run(new_text.Substring(last_pos)));
      }
      catch (System.Exception ex1)
      {
        MessageBox.Show("exception: " + ex1);
      }
    }


    private static void OnUrlClick(object sender, RoutedEventArgs e)
    {
      var link = (Hyperlink)sender;
      // Do something with link.NavigateUri like:
      try
      {
        string url = link.NavigateUri != null ? link.NavigateUri.ToString() : string.Join("", link.Inlines.Select(inline => ((Run)inline).Text));
        Process.Start(url);
      }
      catch (System.Exception ex1)
      {
        Console.WriteLine(ex1.Message);
      }
    }
  }
}

