#region LGPL License
/*----------------------------------------------------------------------------
* This file (CK.Plugin.Model\Requirements\Events\PluginRequirementCollectionChangedEventArgs.cs) is part of CiviKey. 
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
using CK.Plugin;
using CK.Core;
using System.ComponentModel;

namespace CK.Plugin
{
    /// <summary>
    /// Describes a change that occured in a <see cref="IPluginRequirementCollection"/>.
    /// </summary>
    public class PluginRequirementCollectionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The <see cref="ChangeStatus"/> that synthetizes the change.
        /// </summary>
        public ChangeStatus Action { get; private set; }

        /// <summary>
        /// The source <see cref="IPluginRequirementCollection"/> that changed.
        /// </summary>
        public IPluginRequirementCollection Collection { get; private set; }

        /// <summary>
        /// The plugin identifier for which a change occured. 
        /// It is <see cref="Guid.Empty"/> if the change is a global change (<see cref="IPluginRequirementCollection.Clear"/> has been called for instance).
        /// </summary>
        public Guid PluginId { get; private set; }

        /// <summary>
        /// The <see cref="RunningRequirement"/> that changed.
        /// It is <see cref="RunningRequirement.Optional"/> if the change is a global change (<see cref="IPluginRequirementCollection.Clear"/> has been called for instance).
        /// </summary>
        public RunningRequirement Requirement { get; private set; }

        /// <summary>
        /// Initializes a new <see cref="PluginRequirementCollectionChangedEventArgs"/>.
        /// </summary>
        /// <param name="c">The collection that is changing.</param>
        /// <param name="action">The <see cref="ChangeStatus"/>.</param>
        /// <param name="pluginId">The plugin identifier concerned.</param>
        /// <param name="requirement">The <see cref="RunningRequirement"/> of the changing <paramref name="pluginId"/>.</param>
        public PluginRequirementCollectionChangedEventArgs( IPluginRequirementCollection c, ChangeStatus action, Guid pluginId, RunningRequirement requirement )
        {
            Collection = c;
            Action = action;
            PluginId = pluginId;
            Requirement = requirement;
        }
    }

}
