using Interfaces;
using System.Collections.Generic;
using System;


namespace Data
{

    public class Sheeter : IAddSheetable
    {
        public Sheeter(Sheet root = null)
        {
            root_ = root;
            names_ = new List<string>() {
                "name", "visible", "type",
                "subtype", "size", "offset",
                "children"
            };
        }

        public Int64 AddSheet(Int64 idParent, Dictionary<string, string> map)
        {
            if (root_ == null)
                return -1;
            return root_.Add(idParent, map);
        }

        public IList<string> Names()
        {
            return names_;
        }
        
        public void setRoot(Sheet root)
        {
            root_ = root;
        }

        private Sheet root_;
        private List<string> names_;
    };
}