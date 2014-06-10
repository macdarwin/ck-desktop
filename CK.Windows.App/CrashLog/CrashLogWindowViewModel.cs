#region LGPL License
/*----------------------------------------------------------------------------
* This file (CK.Windows.App\CrashLog\CrashLogWindowViewModel.cs) is part of CiviKey. 
*  
* CiviKey is free software: you can redistribute it and/or modify 
* it under the terms of the GNU Lesser General Public License as published 
* by the Free Software Foundation, either version 3 of the License, or 
* (at your option) any later version. 
*  
* CiviKey is distributed in the hope that it will be useful, 
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
* GNU Lesser General Public License for more details. 
* You should have received a copy of the GNU Lesser General Public License 
* along with CiviKey.  If not, see <http://www.gnu.org/licenses/>. 
*  
* Copyright © 2007-2012, 
*     Invenietis <http://www.invenietis.com>,
*     In’Tech INFO <http://www.intechinfo.fr>,
* All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using System.IO;
using System.Reflection;
using System.Net;
using System.Windows;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using CK.Reflection;
using System.Windows.Input;
using CK.Core;

namespace CK.Windows.App
{
    internal class CachedPropertyVM : PropertyChangedBase
    {
        CachedProperty _first;
        Delayed _delay;

		internal void Updated( string propertyName )
        {
            NotifyOfPropertyChange( propertyName );
        }

		
        class Delayed : IDisposable
        {
            public CachedPropertyVM Holder;
            public int RefCount;

            public void Dispose()
            {
                if( RefCount-- == 0 )
                {
                    CachedProperty p = Holder._first;
                    while( p != null )
                    {
                        if( p.Update() ) Holder.NotifyOfPropertyChange( p._p.Name );
                        p = p._next;
                    }
                    Holder._delay = null;
                }
            }
        }

        public IDisposable DelayedPropertyChanged()
        {
            if( _delay == null ) _delay = new Delayed() { Holder = this };
            else ++_delay.RefCount;
            return _delay;
        }

        protected void AddProperty<THolder, TProperty>( THolder holder, Expression<Func<THolder, TProperty>> property )
        {
            _first = new CachedProperty<TProperty>( holder, CK.Reflection.ReflectionHelper.GetPropertyInfo( holder, property ), _first );
        }


    }

    internal abstract class CachedProperty
    {
        internal object _o;
        internal PropertyInfo _p;
        internal CachedProperty _next;

        protected CachedProperty( object o, PropertyInfo p, CachedProperty next )
        {
            _o = o;
            _p = p;
            _next = next;
        }

        public abstract bool Update();
    }

    internal sealed class CachedProperty<TProperty> : CachedProperty
    {
        TProperty _cached;

        internal CachedProperty( object o, PropertyInfo p, CachedProperty next )
            : base( o, p, next )
        {
            _cached = default( TProperty );
        }

        public override bool Update()
        {
            TProperty newValue = (TProperty)_p.GetValue( _o, null );
            if( !EqualityComparer<TProperty>.Default.Equals( newValue, _cached ) )
            {
                _cached = newValue;
                return true;
            }
            return false;
        }

    }

    internal class CrashLogWindowViewModel : CachedPropertyVM
    {
        IActivityMonitor _log;

        WebClient _webClient;
        int _progressPercentage;
        Uri _uploadUri;
        string _errorMessage;
        bool _success;

        public CrashLogWindowViewModel( string crashPath, string crashUploadUrl )
        {
            _log = new ActivityMonitor( "CrashLogManager" );
            _log.Info().Send( "Starting CrashUploader" );

            MultiLogReader.ActivityMap activityMap;

            using( MultiLogReader r = new MultiLogReader() )
            {
                r.Add( _filesToLoad );

                activityMap = r.GetActivityMap();
            }

            DirectoryInfo c = new DirectoryInfo( crashPath );
            Files = new ObservableCollection<FileInfo>( c.GetFiles( "*.ckmon" ) );
            _uploadUri = new Uri( crashUploadUrl );
            _progressPercentage = -1;
			ViewFileCommand = new SimpleCommand<FileInfo>( ViewFile, f => IsNotUploading );
            DeleteFileCommand = new SimpleCommand<FileInfo>( DeleteFile, f => IsNotUploading );

			
            AddProperty( this, t => t.DisplayProgress );
            AddProperty( this, t => t.ProgressPercentage );
            AddProperty( this, t => t.CanSend );
            AddProperty( this, t => t.IsNotUploading );
            AddProperty( this, t => t.DisplaySendButton );
            AddProperty( this, t => t.DisplayThanks );
            AddProperty( this, t => t.DisplayError );
            AddProperty( this, t => t.ErrorMessage );
            AddProperty( this, t => t.OkButtonText );
        }

		public ICommand ViewFileCommand { get; private set; }

        public ICommand DeleteFileCommand { get; private set; }
		
        public ObservableCollection<FileInfo> Files { get; private set; }

        public Visibility DisplayProgress
        {
            get { return _progressPercentage >= 0 ? Visibility.Visible : Visibility.Collapsed; }
        }

        public int ProgressPercentage
        {
            get { return _progressPercentage; }
        }

        public bool CanSend
        {
            get { return _errorMessage == null && !_success && _webClient == null && Files.Count > 0; }
        }

        public bool IsNotUploading
        {
            get { return _webClient == null; }
        }

        public Visibility DisplaySendButton
        {
            get { return _success || Files.Count == 0 ? Visibility.Hidden : Visibility.Visible; }
        }

        public Visibility DisplayThanks
        {
            get { return _success ? Visibility.Visible : Visibility.Hidden; }
        }

        public Visibility DisplayError
        {
            get { return _errorMessage != null ? Visibility.Visible : Visibility.Hidden; }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
        }

        public string OkButtonText
        {
            get
            {
                if( _errorMessage != null || _success || Files.Count == 0 ) return "Fermer";
                return "Plus tard...";
            }
        }

        internal void StartUpload()
        {
            if( _webClient != null ) return;
            using( DelayedPropertyChanged() )
            {
                try
                {
                    _webClient = new WebClient();
                    _webClient.UploadProgressChanged += new UploadProgressChangedEventHandler( _webClient_UploadProgressChanged );
                    _webClient.UploadFileCompleted += new UploadFileCompletedEventHandler( _webClient_UploadFileCompleted );
                    _webClient.UploadFileAsync( _uploadUri, Files[0].FullName );
                    _progressPercentage = 0;
                }
                catch( Exception ex )
                {
                    _log.Error().Send( ex, "While uploading" );
                    OnUploadError( ex );
                }
            }
        }

        void _webClient_UploadProgressChanged( object sender, UploadProgressChangedEventArgs e )
        {
            using( DelayedPropertyChanged() )
            {
                _progressPercentage = e.ProgressPercentage;
            }
        }

        void _webClient_UploadFileCompleted( object sender, UploadFileCompletedEventArgs e )
        {
            using( DelayedPropertyChanged() )
            {
                if( e.Cancelled )
                {
                    OnUploadCompleted();
                }
                else
                {
                    if( e.Error != null ) OnUploadError( e.Error );
                    else
                    {
                        DoDeleteFile( Files[0] );
                        _progressPercentage = 0;
                        if( Files.Count > 0 )
                        {
                            _webClient.UploadFileAsync( _uploadUri, Files[0].FullName );
                        }
                        else
                        {
                            OnUploadCompleted();
                            _success = true;
                        }
                    }
                }
            }
        }

        private void OnUploadError( Exception exception )
        {
            using( DelayedPropertyChanged() )
            {
                OnUploadCompleted();
                string msg = String.Format( "Erreur lors de l'envoi : '{0}'", exception.Message );
                while( (exception = exception.InnerException) != null ) msg += " / " + exception.Message;
                _errorMessage = msg;
            }
        }

        public void ViewFile( FileInfo f )
        {
            if( IsNotUploading ) Process.Start( f.FullName );
        }

        public void DeleteFile( FileInfo f )
        {
            if( MessageBox.Show( "Voulez-vous vraiment supprimer ce fichier ?", "Suppression d'un fichier", MessageBoxButton.YesNo ) == MessageBoxResult.Yes )
            {
                DoDeleteFile( f );
            }
        }

        private void DoDeleteFile( FileInfo f )
        {
			using( DelayedPropertyChanged() )
			{
				try
				{
					f.Delete();
				}
				catch( Exception ex )
				{
					_log.Error().Send( ex, "While deleting crash log." );
				}
				Files.Remove( f );
			}
        }

        private void OnUploadCompleted()
        {
            if( _webClient != null )
            {
                using( DelayedPropertyChanged() )
                {
                    _webClient.Dispose();
                    _webClient = null;
                    _progressPercentage = -1;
                }
            }
        }

    }

}
