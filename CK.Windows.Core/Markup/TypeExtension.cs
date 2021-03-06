#region LGPL License
/*----------------------------------------------------------------------------
* This file (CK.Windows.Core\Markup\TypeExtension.cs) is part of CiviKey. 
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
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Security;
using System.Windows.Markup;
using System.Runtime;
using CK.Core;

namespace CK.Windows
{
    /// <summary>
    /// Replacement for the standard <see cref="System.Windows.Markup.TypeExtension"/> to support generic types
    /// definition in markup (currently up to 5 type arguments are supported).
    /// </summary>
    [MarkupExtensionReturnType( typeof( Type ) ), TypeConverter( typeof( TypeExtensionConverter ) )]
    public class TypeExtension : MarkupExtension
    {
        Type _type;
        string _typeName;

        [TargetedPatchingOptOut( "Performance critical to inline this type of method across NGen image boundaries" )]
        public TypeExtension()
        {
        }

        public TypeExtension( string typeName )
        {
            if( typeName == null ) throw new ArgumentNullException( "typeName" );
            _typeName = typeName;
        }

        public TypeExtension( Type type )
        {
            if( type == null ) throw new ArgumentNullException( "type" );
            _type = type;
        }

        public override object ProvideValue( IServiceProvider serviceProvider )
        {
            if( _type == null )
            {
                if( _typeName == null ) throw new InvalidOperationException();
                if( serviceProvider == null ) throw new ArgumentNullException( "serviceProvider" );
                IXamlTypeResolver service = serviceProvider.GetService<IXamlTypeResolver>( true );
                _type = service.Resolve( _typeName );
                if( _type == null ) throw new InvalidOperationException( String.Format( "Markup Type: {0} not found.", _typeName ) );
            }
            return _type;
        }

        [DefaultValue( (string)null ), ConstructorArgument( "type" )]
        public Type Type
        {
            [TargetedPatchingOptOut( "Performance critical to inline this type of method across NGen image boundaries" )]
            get { return _type; }
            set
            {
                if( value == null ) throw new ArgumentNullException( "value" );
                _type = value;
                _typeName = null;
            }
        }

        [DesignerSerializationVisibility( DesignerSerializationVisibility.Hidden )]
        public string TypeName
        {
            [TargetedPatchingOptOut( "Performance critical to inline this type of method across NGen image boundaries" )]
            get { return _typeName; }
            set 
            {
                if( value == null ) throw new ArgumentNullException( "value" );
                _typeName = value;
                _type = null;
            }
        }
    }

    internal class TypeExtensionConverter : TypeConverter
    {
        public override bool CanConvertTo( ITypeDescriptorContext context, Type destinationType )
        {
            return ((destinationType == typeof( InstanceDescriptor )) || base.CanConvertTo( context, destinationType ));
        }

        [SecurityCritical]
        public override object ConvertTo( ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType )
        {
            if( !(destinationType == typeof( InstanceDescriptor )) )
            {
                return base.ConvertTo( context, culture, value, destinationType );
            }
            TypeExtension extension = value as TypeExtension;
            if( extension == null )
            {
                throw new ArgumentException( String.Format( "Type must be '{0}'.", typeof( TypeExtension ).FullName ) );
            }
            return new InstanceDescriptor( typeof( TypeExtension ).GetConstructor( new Type[] { typeof( Type ) } ), new object[] { extension.Type } );
        }
    }

}
