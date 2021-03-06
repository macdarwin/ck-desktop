﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace CK.Windows.Helpers
{
    /// <summary>
    /// Helper methods for UI-related tasks.
    /// </summary>
    public static class TreeHelper
    {

        #region find children

        /// <summary>
        /// Analyzes both visual and logical tree in order to find all elements of a given
        /// type that are descendants of the <paramref name="source"/> item.
        /// </summary>
        /// <typeparam name="T">The type of the queried items.</typeparam>
        /// <param name="source">The root element that marks the source of the search. If the
        /// source is already of the requested type, it will not be included in the result.</param>
        /// <returns>All descendants of <paramref name="source"/> that match the requested type.</returns>
        public static IEnumerable<T> FindChildren<T>( this DependencyObject source ) where T : DependencyObject
        {
            if( source != null )
            {
                var childs = GetChildObjects( source );
                foreach( DependencyObject child in childs )
                {
                    //analyze if children match the requested type
                    if( child != null && child is T )
                    {
                        yield return (T)child;
                    }

                    //recurse tree
                    foreach( T descendant in FindChildren<T>( child ) )
                    {
                        yield return descendant;
                    }
                }
            }
        }


        /// <summary>
        /// This method is an alternative to WPF's
        /// <see cref="VisualTreeHelper.GetChild"/> method, which also
        /// supports content elements. Keep in mind that it only navigates through the visual tree.
        /// </summary>
        /// <param name="parent">The item to be processed.</param>
        /// <returns>The submitted item's child elements, if available.</returns>
        public static IEnumerable<DependencyObject> GetChildObjects( this DependencyObject parent )
        {
            if( parent == null ) yield break;

            int count = VisualTreeHelper.GetChildrenCount( parent );
            for( int i = 0; i < count; i++ )
            {
                yield return VisualTreeHelper.GetChild( parent, i );
            }
        }

        #endregion
    }
}
