﻿//=====================================================================================================================================================
// Bing Ads .NET SDK ver. 9.3
// 
// Copyright (c) Microsoft Corporation
// 
// All rights reserved. 
// 
// MS-PL License
// 
// This license governs use of the accompanying software. If you use the software, you accept this license. 
//  If you do not accept the license, do not use the software.
// 
// 1. Definitions
// 
// The terms reproduce, reproduction, derivative works, and distribution have the same meaning here as under U.S. copyright law. 
//  A contribution is the original software, or any additions or changes to the software. 
//  A contributor is any person that distributes its contribution under this license. 
//  Licensed patents  are a contributor's patent claims that read directly on its contribution.
// 
// 2. Grant of Rights
// 
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
//  each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, 
//  prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// 
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
//  each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, 
//  sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
// 
// 3. Conditions and Limitations
// 
// (A) No Trademark License - This license does not grant you rights to use any contributors' name, logo, or trademarks.
// 
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
//  your patent license from such contributor to the software ends automatically.
// 
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, 
//  and attribution notices that are present in the software.
// 
// (D) If you distribute any portion of the software in source code form, 
//  you may do so only under this license by including a complete copy of this license with your distribution. 
//  If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
// 
// (E) The software is licensed *as-is.* You bear the risk of using it. The contributors give no express warranties, guarantees or conditions.
//  You may have additional consumer rights under your local laws which this license cannot change. 
//  To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, 
//  fitness for a particular purpose and non-infringement.
//=====================================================================================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.BingAds.Bulk.Entities;
using Microsoft.BingAds.CampaignManagement;

// ReSharper disable once CheckNamespace
namespace Microsoft.BingAds.Internal.Bulk.Entities
{
    /// <summary>
    /// This abstract base class provides properties that are shared by all bulk radius target classes.
    /// </summary>
    /// <typeparam name="TBid"><see cref="BulkRadiusTargetBid"/></typeparam>
    public abstract class BulkRadiusTarget<TBid> : BulkSubTarget<TBid>
        where TBid : BulkRadiusTargetBid
    {                
        /// <summary>
        /// Defines a list of geographical radius targets with bid adjustments.  
        /// </summary>
        public RadiusTarget2 RadiusTarget
        {
            get { return GetLocationProperty(x => x.RadiusTarget); }
            set { SetLocationProperty(x => x.RadiusTarget = value); }
        }

        /// <summary>
        /// Defines the possible intent options for location targeting.
        /// </summary>
        public IntentOption? IntentOption
        {
            get { return GetLocationProperty(x => x.IntentOption); }
            set { SetLocationProperty(x => x.IntentOption = value); }
        }

        internal LocationTarget2 Location { get; set; }

        /// <summary>
        /// Reserved for internal use.
        /// </summary>
        protected override void ReconstructSubTargets()
        {
            ReconstructApiBids(x => x.RadiusTargetBid, () => Location.RadiusTarget, _ => Location.RadiusTarget = _, () => Location.RadiusTarget.Bids, _ => Location.RadiusTarget.Bids = _);
        }

        /// <summary>
        /// Reserved for internal use.
        /// </summary>
        protected override IReadOnlyList<TBid> ConvertApiToBulkBids()
        {
            if (Location.RadiusTarget == null || Location.RadiusTarget.Bids == null)
            {
                return new List<TBid>();
            }

            return Location.RadiusTarget.Bids.Select(b => CreateAndPopulateBid(x => x.RadiusTargetBid = b)).ToList();
        }

        /// <summary>
        /// Reserved for internal use.
        /// </summary>
        protected override void ValidatePropertiesNotNull()
        {
            ValidatePropertyNotNull(RadiusTarget, "RadiusTarget");
        }

        /// <summary>
        /// Reserved for internal use.
        /// </summary>
        protected override void ValidateBidsNotNullOrEmpty()
        {
            if (RadiusTarget != null)
            {
                ValidateListNotNullOrEmpty(RadiusTarget.Bids, "RadiusTarget.Bids");
            }
        }

        private void ReconstructApiBids<TApiBid, TTarget>(Func<TBid, TApiBid> createBid, Func<TTarget> getTarget, Action<TTarget> setTarget, Func<IList<TApiBid>> getBids, Action<IList<TApiBid>> setBids)
            where TApiBid : new()
            where TTarget : class, new()
        {
            var bidsFromFile = Bids.Select(createBid).ToList();

            if (bidsFromFile.Count > 0)
            {
                if (getTarget() == null)
                {
                    setTarget(new TTarget());

                    setBids(new List<TApiBid>());
                }

                getBids().AddRange(bidsFromFile);
            }
        }

        private T GetLocationProperty<T>(Func<LocationTarget2, T> getFunc)
        {
            if (Location == null)
            {
                return default(T);
            }

            return getFunc(Location);
        }

        private void SetLocationProperty(Action<LocationTarget2> setAction)
        {
            if (Location == null)
            {
                Location = new LocationTarget2();
            }

            setAction(Location);
        }
    }
}
