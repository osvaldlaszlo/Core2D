﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Core2D.UI.Wpf.Controls
{
    /// <summary>
    /// 
    /// </summary>
    public class RecordsListView : ListView
    {
        private bool _isLoaded = false;
        private Point dragStartPoint;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="P"></typeparam>
        /// <param name="child"></param>
        /// <returns></returns>
        private P FindVisualParent<P>(DependencyObject child)
            where P : DependencyObject
        {
            var parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null)
                return null;

            P parent = parentObject as P;
            if (parent != null)
                return parent;

            return FindVisualParent<P>(parentObject);
        }

        /// <summary>
        /// 
        /// </summary>
        public RecordsListView()
        {
            Loaded +=
                (sender, e) =>
                {
                    if (_isLoaded)
                        return;
                    else
                        _isLoaded = true;

                    InitializeColumnsView();
                };

            DataContextChanged +=
                (sender, e) =>
                {
                    var old = e.OldValue as Database;
                    if (old != null)
                    {
                        StopObservingColumns(old);
                    }

                    InitializeColumnsView();
                };

            this.PreviewMouseLeftButtonDown += ListView_PreviewMouseLeftButtonDown;
            this.PreviewMouseMove += ListView_PreviewMouseMove;
        }

        /// <summary>
        /// 
        /// </summary>
        public void InitializeColumnsView()
        {
            var database = DataContext as Database;
            if (database != null)
            {
                this.View = CreateColumnsView(database.Columns);
                StopObservingColumns(database);
                StartObservingColumns(database);
            }
            else
            {
                this.View = null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        private GridView CreateColumnsView(ImmutableArray<Column> columns)
        {
            var gv = new GridView();

            gv.SetValue(VirtualizingStackPanel.IsVirtualizingProperty, true);
            gv.SetValue(VirtualizingStackPanel.VirtualizationModeProperty, VirtualizationMode.Recycling);
            gv.SetValue(ScrollViewer.IsDeferredScrollingEnabledProperty, true);
            gv.SetValue(ScrollViewer.CanContentScrollProperty, true);

            int i = 0;
            foreach (var column in columns)
            {
                if (column.IsVisible)
                {
                    var gvc = new GridViewColumn
                    {
                        Header = column.Name,
                        Width = column.Width,
                        DisplayMemberBinding = new System.Windows.Data.Binding("Values[" + i + "].Content")
                    };
                    gv.Columns.Add(gvc);
                }
                i++;
            }

            return gv;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColumnObserver(object sender, PropertyChangedEventArgs e)
        {
            InitializeColumnsView();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DatabaseObserver(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Database.Columns))
            {
                InitializeColumnsView();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="database"></param>
        private void StartObservingColumns(Database database)
        {
            if (database == null || database.Columns == null)
                return;

            foreach (var column in database.Columns)
            {
                column.PropertyChanged += ColumnObserver;
            }

            database.PropertyChanged += DatabaseObserver;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="database"></param>
        private void StopObservingColumns(Database database)
        {
            if (database == null || database.Columns == null)
                return;

            foreach (var column in database.Columns)
            {
                column.PropertyChanged -= ColumnObserver;
            }

            database.PropertyChanged -= DatabaseObserver;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragStartPoint = e.GetPosition(null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Point point = e.GetPosition(null);
            Vector diff = dragStartPoint - point;
            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                var listView = sender as ListView;
                var listViewItem = FindVisualParent<ListViewItem>((DependencyObject)e.OriginalSource);
                if (listViewItem != null)
                {
                    var record = (Record)listView
                        .ItemContainerGenerator
                        .ItemFromContainer(listViewItem);
                    DataObject dragData = new DataObject(typeof(Record), record);
                    DragDrop.DoDragDrop(
                        listViewItem,
                        dragData,
                        DragDropEffects.Move);
                }
            }
        }
    }
}
