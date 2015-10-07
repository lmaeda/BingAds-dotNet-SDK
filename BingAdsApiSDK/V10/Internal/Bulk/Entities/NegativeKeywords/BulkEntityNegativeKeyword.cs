﻿using Microsoft.BingAds.V10.Bulk.Entities;
using Microsoft.BingAds.V10.Internal;
using Microsoft.BingAds.V10.Internal.Bulk;
using Microsoft.BingAds.V10.Internal.Bulk.Mappings;

// ReSharper disable once CheckNamespace
namespace Microsoft.BingAds.V10.Internal.Bulk.Entities
{
    /// <summary>
    /// This abstract base class for all bulk negative keywords that are assigned individually to a campaign or ad group entity.
    /// </summary>
    /// <seealso cref="BulkAdGroupNegativeKeyword"/>
    /// <seealso cref="BulkCampaignNegativeKeyword"/>
    public abstract class BulkEntityNegativeKeyword : BulkNegativeKeyword
    {
        /// <summary>
        /// Reserved for internal use.
        /// </summary>
        protected string EntityName { get; set; }

        /// <summary>
        /// Reserved for internal use.
        /// </summary>
        protected abstract string EntityColumnName { get; }

        private static readonly IBulkMapping<BulkEntityNegativeKeyword>[] Mappings =
        {
            new DynamicColumnNameMapping<BulkEntityNegativeKeyword>(c => c.EntityColumnName,
                c => c.EntityName,
                (v, c) => c.EntityName = v
            ), 
        };

        internal override void ProcessMappingsFromRowValues(RowValues values)
        {
            base.ProcessMappingsFromRowValues(values);

            values.ConvertToEntity(this, Mappings);
        }

        internal override void ProcessMappingsToRowValues(RowValues values, bool excludeReadonlyData)
        {
            base.ProcessMappingsToRowValues(values, excludeReadonlyData);

            this.ConvertToValues(values, Mappings);
        }
    }
}
