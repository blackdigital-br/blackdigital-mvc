using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackDigital.Mvc.Test.Mock
{
    public class ComplexClass
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public ComplexClass Parent { get; set; }

        //public List<SimpleClass> ListChild { get; set; }
        public SimpleClass[] ListChild { get; set; }
    }
}
