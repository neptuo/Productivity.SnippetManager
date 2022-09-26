﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;

namespace Neptuo.Productivity.SnippetManager.Views.Controls
{
    public static class WindowDrag
    {
        private readonly static Type[] activeControlTypes = new Type[]
        {
            typeof(ListBoxItem),
            typeof(ListViewItem),
            typeof(Button),
            typeof(ToggleButton),
            typeof(TextBox),
            typeof(ComboBox),
            typeof(CheckBox),
            typeof(ComboBoxItem)
        };

        public static bool TryMove(MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return false;

            FrameworkElement? element = e.OriginalSource as FrameworkElement;
            if (element != null)
            {
                for (int i = 0; i < 10; i++)
                {
                    FrameworkElement? parent = element.Parent as FrameworkElement;
                    if (parent == null)
                        parent = element.TemplatedParent as FrameworkElement;

                    if (parent == null)
                        parent = VisualTreeHelper.GetParent(element) as FrameworkElement;

                    if (parent == null)
                        break;

                    Type parentType = parent.GetType();
                    if (activeControlTypes.Contains(parentType))
                        return false;

                    element = parent;
                }
            }

            return true;
        }
    }
}
