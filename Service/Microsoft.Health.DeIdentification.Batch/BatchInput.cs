﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Health.DeIdentification.Batch
{
    public class BatchInput<TSource>
    {
        public TSource[] Sources { get; set; }

        public int StartIndex { get; set; }

        public string[] Select()
        {
            throw new NotImplementedException();
        }
    }
}