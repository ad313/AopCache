using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AopCache.Test1
{
    public class TestClass
    {
        [AopCache(Key = "aaaaa",Group = "b",Length = 10000)]
        public virtual Guid Get()
        {
            return Guid.NewGuid();
        }
    }
}
