﻿using Microsoft.BingAds.V10.Internal.Bulk.Entities;

// ReSharper disable once CheckNamespace
namespace Microsoft.BingAds.V10.Bulk.Entities
{
    /// <summary>
    /// <para>
    /// Represents a campaign level image ad extension. 
    /// This class exposes properties that can be read and written 
    /// as fields of the Campaign Image Ad Extension record in a bulk file. 
    /// </para>
    /// <para>For more information, see <see href="http://go.microsoft.com/fwlink/?LinkID=620276">Campaign Image Ad Extension</see>. </para>
    /// </summary>
    /// <seealso cref="BulkServiceManager"/>
    /// <seealso cref="BulkOperation{TStatus}"/>
    /// <seealso cref="BulkFileReader"/>
    /// <seealso cref="BulkFileWriter"/>
    public class BulkCampaignImageAdExtension : BulkCampaignAdExtensionAssociation
    {
    }
}
