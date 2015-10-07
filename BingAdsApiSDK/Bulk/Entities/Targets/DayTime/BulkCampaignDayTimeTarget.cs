﻿using Microsoft.BingAds.CampaignManagement;
using Microsoft.BingAds.Internal.Bulk.Entities;

// ReSharper disable once CheckNamespace
namespace Microsoft.BingAds.Bulk.Entities
{
    /// <summary>    
    /// Represents a day and time target that is associated with a campaign. 
    /// This class exposes the <see cref="BulkDayTimeTarget{TBid}.DayTimeTarget"/> property that can be read and written as fields of the Campaign DayTime Target record in a bulk file.         
    /// </summary>
    /// <remarks>
    /// <para>
    /// One <see cref="BulkCampaignDayTimeTarget"/> exposes a read only list of <see cref="BulkCampaignDayTimeTargetBid"/>. Each <see cref="BulkCampaignDayTimeTargetBid"/> instance 
    /// corresponds to one Campaign DayTime Target record in the bulk file. If you upload a <see cref="BulkCampaignDayTimeTarget"/>, 
    /// then you are effectively replacing any existing bids for the corresponding day and time target. 
    /// </para>
    /// <para>For more information, see <see href="http://go.microsoft.com/fwlink/?LinkID=512016">Campaign DayTime Target</see>. </para>
    /// </remarks>
    /// <seealso cref="BulkServiceManager"/>
    /// <seealso cref="BulkOperation{TStatus}"/>
    /// <seealso cref="BulkFileReader"/>
    /// <seealso cref="BulkFileWriter"/>
    public class BulkCampaignDayTimeTarget : BulkDayTimeTarget<BulkCampaignDayTimeTargetBid>
    {
        /// <summary>
        /// The identifier of the campaign that the target is associated.
        /// Corresponds to the 'Parent Id' field in the bulk file. 
        /// </summary>
        public long? CampaignId
        {
            get { return EntityId; }
            set { EntityId = value; }
        }

        /// <summary>
        /// The name of the campaign that the target is associated.
        /// Corresponds to the 'Campaign' field in the bulk file. 
        /// </summary>
        public string CampaignName
        {
            get { return EntityName; }
            set { EntityName = value; }
        }

        /// <summary>
        /// Reserved for internal use.
        /// </summary>
        protected internal override BulkCampaignDayTimeTargetBid CreateBid()
        {
            return new BulkCampaignDayTimeTargetBid();
        }
    }
}
