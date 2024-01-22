using System;
using System.Collections.Generic;


namespace Interfaces
{

    public interface IAddSheetable
    {
        Int64 AddSheet(Int64 idParent, Dictionary<string, string> map);
        IList<string> Names();
    }

}