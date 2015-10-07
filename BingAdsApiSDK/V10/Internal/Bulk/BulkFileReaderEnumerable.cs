﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.BingAds.V10.Bulk;
using Microsoft.BingAds.V10.Bulk.Entities;

namespace Microsoft.BingAds.V10.Internal.Bulk
{
    internal class BulkFileReaderEnumerable : IEnumerable<BulkEntity>
    {
        private readonly BulkFileReader _bulkFileReader;

        public BulkFileReaderEnumerable(BulkFileReader bulkFileReader)
        {
            _bulkFileReader = bulkFileReader;
        }

        public IEnumerator<BulkEntity> GetEnumerator()
        {
            return new BulkFileReaderIEnumerator(_bulkFileReader);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new BulkFileReaderIEnumerator(_bulkFileReader);
        }
    }
}
