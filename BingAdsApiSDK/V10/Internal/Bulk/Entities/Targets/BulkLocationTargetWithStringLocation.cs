﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.BingAds.Internal;
using Microsoft.BingAds.V10.CampaignManagement;
using Microsoft.BingAds.V10.Bulk.Entities;

// ReSharper disable once CheckNamespace
namespace Microsoft.BingAds.V10.Internal.Bulk.Entities
{
    /// <summary>
    /// This abstract base class provides properties that are shared by all bulk location target classes.
    /// </summary>
    /// <typeparam name="TBid"><see cref="BulkLocationTargetBid"/></typeparam>
    public abstract class BulkLocationTargetWithStringLocation<TBid> : BulkTargetWithLocation<TBid>
        where TBid : BulkLocationTargetBidWithStringLocation
    {
        /// <summary>
        /// Defines a list of cities to target with bid adjustments.
        /// </summary>
        public CityTarget CityTarget
        {
            get { return GetLocationProperty(x => x.CityTarget); }
            set { SetLocationProperty(x => x.CityTarget = value); }
        }

        /// <summary>
        /// Defines a list of metro areas to target with bid adjustments.
        /// </summary>
        public MetroAreaTarget MetroAreaTarget
        {
            get { return GetLocationProperty(x => x.MetroAreaTarget); }
            set { SetLocationProperty(x => x.MetroAreaTarget = value); }
        }

        /// <summary>
        /// Defines a list of states to target with bid adjustments.
        /// </summary>
        public StateTarget StateTarget
        {
            get { return GetLocationProperty(x => x.StateTarget); }
            set { SetLocationProperty(x => x.StateTarget = value); }
        }

        /// <summary>
        /// Defines a list of countries to target with bid adjustments.
        /// </summary>
        public CountryTarget CountryTarget
        {
            get { return GetLocationProperty(x => x.CountryTarget); }
            set { SetLocationProperty(x => x.CountryTarget = value); }
        }

        /// <summary>
        /// Defines a list of postal codes to target with bid adjustments.
        /// </summary>
        public PostalCodeTarget PostalCodeTarget
        {
            get { return GetLocationProperty(x => x.PostalCodeTarget); }
            set { SetLocationProperty(x => x.PostalCodeTarget = value); }
        }

        // Copies additional properties from bulk bid to API bid: BidAdjustment for LocationTarget, IsExcluded for NegativeLocationTarget

        internal abstract CityTargetBid SetCityBidAdditionalProperties(CityTargetBid apiBid, TBid bulkBid);

        internal abstract MetroAreaTargetBid SetMetroAreaBidAdditionalProperties(MetroAreaTargetBid apiBid, TBid bulkBid);

        internal abstract StateTargetBid SetStateBidAdditionalProperties(StateTargetBid apiBid, TBid bulkBid);

        internal abstract CountryTargetBid SetCountryBidAdditionalProperties(CountryTargetBid apiBid, TBid bulkBid);

        internal abstract PostalCodeTargetBid SetPostalCodeBidAdditionalProperties(PostalCodeTargetBid apiBid, TBid bulkBid);

        // Copies additional properties from API bid to bulk bid: BidAdjustment for LocationTarget, IsExcluded for NegativeLocationTarget

        internal abstract void SetBulkCityBidAdditionalProperties(TBid bulkBid, CityTargetBid apiBid);

        internal abstract void SetBulkMetroAreaBidAdditionalProperties(TBid bulkBid, MetroAreaTargetBid apiBid);

        internal abstract void SetBulkStateBidAdditionalProperties(TBid bulkBid, StateTargetBid apiBid);

        internal abstract void SetBulkCountryBidAdditionalProperties(TBid bulkBid, CountryTargetBid apiBid);

        internal abstract void SetBulkPostalCodeBidAdditionalProperties(TBid bulkBid, PostalCodeTargetBid apiBid);

        // Should convert LocationTargetBid if IsExcluded is false, should convert NegativeLocationTargetBid if IsExcluded is true

        internal abstract bool ShouldConvertCityTargetBid(CityTargetBid bid);

        internal abstract bool ShouldConvertMetroAreaTargetBid(MetroAreaTargetBid bid);

        internal abstract bool ShouldConvertStateTargetBid(StateTargetBid bid);

        internal abstract bool ShouldConvertCountryTargetBid(CountryTargetBid bid);

        internal abstract bool ShouldConvertPostalCodeTargetBid(PostalCodeTargetBid bid);

        /// <summary>
        /// Reserved for internal use.
        /// </summary>
        protected override void ReconstructSubTargets()
        {
            ReconstructApiBids(
                LocationTargetType.City,
                t => SetCityBidAdditionalProperties(new CityTargetBid { City = t.Location }, t),
                () => Location.CityTarget,
                () => new CityTarget(),
                _ => Location.CityTarget = _,
                () => Location.CityTarget.Bids,
                _ => Location.CityTarget.Bids = _
            );

            ReconstructApiBids(
                LocationTargetType.MetroArea,
                t => SetMetroAreaBidAdditionalProperties(new MetroAreaTargetBid() { MetroArea = t.Location }, t),
                () => Location.MetroAreaTarget,
                () => new MetroAreaTarget(),
                _ => Location.MetroAreaTarget = _,
                () => Location.MetroAreaTarget.Bids,
                _ => Location.MetroAreaTarget.Bids = _
            );

            ReconstructApiBids(
                LocationTargetType.State,
                t => SetStateBidAdditionalProperties(new StateTargetBid() { State = t.Location }, t),
                () => Location.StateTarget,
                () => new StateTarget(),
                _ => Location.StateTarget = _,
                () => Location.StateTarget.Bids,
                _ => Location.StateTarget.Bids = _
            );

            ReconstructApiBids(
                LocationTargetType.Country,
                t => SetCountryBidAdditionalProperties(new CountryTargetBid() { CountryAndRegion = t.Location }, t),
                () => Location.CountryTarget,
                () => new CountryTarget(),
                _ => Location.CountryTarget = _,
                () => Location.CountryTarget.Bids,
                _ => Location.CountryTarget.Bids = _
            );

            ReconstructApiBids(
                LocationTargetType.PostalCode,
                t => SetPostalCodeBidAdditionalProperties(new PostalCodeTargetBid() { PostalCode = t.Location }, t),
                () => Location.PostalCodeTarget,
                () => new PostalCodeTarget(),
                _ => Location.PostalCodeTarget = _,
                () => Location.PostalCodeTarget.Bids,
                _ => Location.PostalCodeTarget.Bids = _
            );
        }

        /// <summary>
        /// Reserved for internal use.
        /// </summary>
        protected override IReadOnlyList<TBid> ConvertApiToBulkBids()
        {
            var bids = new List<TBid>();

            ConvertBidsFromApi(LocationTargetType.City, bids, () => Location.CityTarget, _ => _.Bids, _ => _.City, ShouldConvertCityTargetBid);
            ConvertBidsFromApi(LocationTargetType.MetroArea, bids, () => Location.MetroAreaTarget, _ => _.Bids, _ => _.MetroArea, ShouldConvertMetroAreaTargetBid);
            ConvertBidsFromApi(LocationTargetType.State, bids, () => Location.StateTarget, _ => _.Bids, _ => _.State, ShouldConvertStateTargetBid);
            ConvertBidsFromApi(LocationTargetType.Country, bids, () => Location.CountryTarget, _ => _.Bids, _ => _.CountryAndRegion, ShouldConvertCountryTargetBid);
            ConvertBidsFromApi(LocationTargetType.PostalCode, bids, () => Location.PostalCodeTarget, _ => _.Bids, _ => _.PostalCode, ShouldConvertPostalCodeTargetBid);

            return bids;
        }

        /// <summary>
        /// Reserved for internal use.
        /// </summary>
        protected override void ValidatePropertiesNotNull()
        {
            if (CityTarget == null && MetroAreaTarget == null && StateTarget == null && CountryTarget == null && PostalCodeTarget == null)
            {
                throw new InvalidOperationException(ErrorMessages.AtLeastOneLocationSubTargetMustNotBeNull);
            }
        }

        /// <summary>
        /// Reserved for internal use.
        /// </summary>
        protected override void ValidateBidsNotNullOrEmpty()
        {
            if (CityTarget != null)
            {
                ValidateListNotNullOrEmpty(CityTarget.Bids, "CityTarget.Bids");
            }

            if (MetroAreaTarget != null)
            {
                ValidateListNotNullOrEmpty(MetroAreaTarget.Bids, "MetroAreaTarget.Bids");
            }

            if (StateTarget != null)
            {
                ValidateListNotNullOrEmpty(StateTarget.Bids, "StateTarget.Bids");
            }

            if (CountryTarget != null)
            {
                ValidateListNotNullOrEmpty(CountryTarget.Bids, "CountryTarget.Bids");
            }

            if (PostalCodeTarget != null)
            {
                ValidateListNotNullOrEmpty(PostalCodeTarget.Bids, "PostalCodeTarget.Bids");
            }
        }

        /// <summary>
        /// Reserved for internal use.
        /// </summary>
        protected void ReconstructApiBids<TApiBid, TTarget>(
            LocationTargetType locationType,
            Func<TBid, TApiBid> createBid,
            Func<TTarget> getTarget,
            Func<TTarget> createNewTarget,
            Action<TTarget> setTarget,
            Func<IList<TApiBid>> getBids,
            Action<IList<TApiBid>> setBids
        )
            where TApiBid : new()
            where TTarget : class, new()
        {
            ReconstructApiBids(Bids.Where(t => t.LocationType == locationType).ToList(), createBid, getTarget, createNewTarget, setTarget, getBids, setBids);
        }

        /// <summary>
        /// Reserved for internal use.
        /// </summary>
        protected void ConvertBidsFromApi<TApiBid, TTarget>(
            LocationTargetType locationType, 
            IList<TBid> bids, 
            Func<TTarget> getTarget, 
            Func<TTarget, IList<TApiBid>> getBids, 
            Func<TApiBid, string> getLocation, 
            Func<TApiBid, bool> shouldConvertBid
        )
            where TTarget : class
        {
            ConvertBidsFromApi(bids, getTarget, getBids, (bulkBid, apiBid) =>
            {
                bulkBid.LocationType = locationType;
                bulkBid.Location = getLocation(apiBid);

                if (!(
                    TrySetBulkBidAdditionalProperties(bulkBid, apiBid as CityTargetBid, SetBulkCityBidAdditionalProperties) ||
                    TrySetBulkBidAdditionalProperties(bulkBid, apiBid as MetroAreaTargetBid, SetBulkMetroAreaBidAdditionalProperties) ||
                    TrySetBulkBidAdditionalProperties(bulkBid, apiBid as StateTargetBid, SetBulkStateBidAdditionalProperties) ||
                    TrySetBulkBidAdditionalProperties(bulkBid, apiBid as CountryTargetBid, SetBulkCountryBidAdditionalProperties) ||
                    TrySetBulkBidAdditionalProperties(bulkBid, apiBid as PostalCodeTargetBid, SetBulkPostalCodeBidAdditionalProperties)
                ))
                {
                    throw new NotImplementedException("Unknown target bid type.");
                }                               
            }, shouldConvertBid);
        }

        private static bool TrySetBulkBidAdditionalProperties<TBulkBid, TApiBid>(TBulkBid bulkBid, TApiBid apiBid, Action<TBulkBid, TApiBid> setBulkBidAdditionalPropertiesAction)
            where TApiBid : class
        {
            if (apiBid != null)
            {
                setBulkBidAdditionalPropertiesAction(bulkBid, apiBid);

                return true;
            }

            return false;
        }
    }
}
